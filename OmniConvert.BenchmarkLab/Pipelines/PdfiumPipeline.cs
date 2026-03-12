using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;
using PdfiumViewer;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class PdfiumPipeline : IConversionPipeline
{
    public string Name => "PdfiumPipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Pdf;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
    ConversionRequest request,
    CancellationToken cancellationToken = default)
    {
        var tempFiles = new List<string>();

        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();

            Console.WriteLine($"[PDFIUM] Start PrivateMemory MB : {process.PrivateMemorySize64 / 1024d / 1024d:F2}");
            Console.WriteLine($"[PDFIUM] Input                  : {request.InputPath}");
            Console.WriteLine($"[PDFIUM] Output                 : {request.OutputPath}");
            Console.WriteLine($"[PDFIUM] Profile                : {request.Profile.Name}");

            cancellationToken.ThrowIfCancellationRequested();

            string? outputDirectory = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using var document = PdfDocument.Load(request.InputPath);

            for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageSize = document.PageSizes[pageIndex];

                int width = Math.Max(1, (int)Math.Ceiling(pageSize.Width / 72.0 * request.Profile.Dpi));
                int height = Math.Max(1, (int)Math.Ceiling(pageSize.Height / 72.0 * request.Profile.Dpi));

                using var renderedImage = document.Render(
                    pageIndex,
                    width,
                    height,
                    request.Profile.Dpi,
                    request.Profile.Dpi,
                    PdfRenderFlags.Annotations);

                using var bitmap = new Bitmap(renderedImage);
                using var pageImage = ConvertBitmapToMagickImage(bitmap, request.Profile);

                string tempPagePath = Path.Combine(
                    Path.GetTempPath(),
                    $"omniconvert_pdfium_{Guid.NewGuid():N}_page_{pageIndex + 1}.tiff");

                pageImage.Write(tempPagePath);
                tempFiles.Add(tempPagePath);

                process.Refresh();
                Console.WriteLine($"[PDFIUM] Temp page written      : {pageIndex + 1}/{document.PageCount}");
                Console.WriteLine($"[PDFIUM] Page {pageIndex + 1}/{document.PageCount} PrivateMemory MB : {process.PrivateMemorySize64 / 1024d / 1024d:F2}");
            }

            using var mergedFrames = new MagickImageCollection();

            foreach (string tempFile in tempFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mergedImage = new MagickImage(tempFile);
                mergedImage.Format = MagickFormat.Tiff;
                mergedImage.Density = new Density(request.Profile.Dpi, request.Profile.Dpi);

                ApplyColorMode(mergedImage, request.Profile);
                ApplyCompression(mergedImage, request.Profile);

                mergedFrames.Add(mergedImage);
            }

            mergedFrames.Write(request.OutputPath);

            process.Refresh();
            Console.WriteLine($"[PDFIUM] End PrivateMemory MB   : {process.PrivateMemorySize64 / 1024d / 1024d:F2}");

            long outputBytes = File.Exists(request.OutputPath)
                ? new FileInfo(request.OutputPath).Length
                : 0;

            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = request.OutputPath,
                Success = true,
                ErrorMessage = null,
                ElapsedMilliseconds = 0,
                PeakPrivateBytes = 0,
                FinalPrivateBytes = 0,
                OutputFileBytes = outputBytes
            };
        }
        catch (Exception ex)
        {
            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = request.OutputPath,
                Success = false,
                ErrorMessage = ex.ToString(),
                ElapsedMilliseconds = 0,
                PeakPrivateBytes = 0,
                FinalPrivateBytes = 0,
                OutputFileBytes = 0
            };
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // Temp cleanup best-effort
                }
            }
        }
    }

    private static MagickImage ConvertBitmapToMagickImage(Bitmap bitmap, ConversionProfile profile)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;

        var image = new MagickImage(memoryStream);
        image.Density = new Density(profile.Dpi, profile.Dpi);
        image.Format = MagickFormat.Tiff;

        ApplyColorMode(image, profile);
        ApplyCompression(image, profile);

        return image;
    }

    private static void ApplyColorMode(MagickImage image, ConversionProfile profile)
    {
        switch (profile.ColorMode)
        {
            case TargetColorMode.Rgb24Bit:
                image.ColorType = ColorType.TrueColor;
                break;

            case TargetColorMode.Grayscale8Bit:
                image.Grayscale();
                image.Depth = 8;
                break;

            case TargetColorMode.Binary1Bit:
                image.Grayscale();
                image.Depth = 1;

                if (profile.Threshold.HasValue)
                {
                    double thresholdPercent = profile.Threshold.Value / 255.0 * 100.0;
                    image.Threshold(new Percentage(thresholdPercent));
                }
                else
                {
                    image.Threshold(new Percentage(50));
                }

                break;

            default:
                throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}");
        }
    }

    private static void ApplyCompression(MagickImage image, ConversionProfile profile)
    {
        switch (profile.Compression)
        {
            case TiffCompressionKind.None:
                image.Settings.Compression = CompressionMethod.NoCompression;
                break;

            case TiffCompressionKind.Lzw:
                image.Settings.Compression = CompressionMethod.LZW;
                break;

            case TiffCompressionKind.Jpeg:
                image.Settings.Compression = CompressionMethod.JPEG;
                image.Quality = (uint)(profile.JpegQuality ?? 85);
                break;

            case TiffCompressionKind.Ccitt4:
                image.Settings.Compression = CompressionMethod.Group4;
                break;

            default:
                throw new NotSupportedException($"Desteklenmeyen Compression: {profile.Compression}");
        }
    }
}