using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;
using OmniConvert.BenchmarkLab.Core;
using PdfiumViewer;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class PdfiumPngPipeline : IConversionPipeline
{
    public string Name => "PdfiumPngPipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Pdf;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        string finalOutputPath = BuildUniqueOutputPath(request.OutputPath, Name, request.Profile.Name);
        string? outputDirectory = Path.GetDirectoryName(finalOutputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var tempFiles = new List<string>();

        try
        {
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

                string tempPngPath = Path.Combine(
                    Path.GetTempPath(),
                    $"omniconvert_pdfium_png_{Guid.NewGuid():N}_page_{pageIndex + 1}.png");

                bitmap.Save(tempPngPath, ImageFormat.Png);
                tempFiles.Add(tempPngPath);
            }

            using var mergedFrames = new MagickImageCollection();

            foreach (string tempFile in tempFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var image = new MagickImage(tempFile);
                image.Format = MagickFormat.Tiff;
                image.Density = new Density(request.Profile.Dpi, request.Profile.Dpi);

                ApplyColorMode(image, request.Profile);
                ApplyCompression(image, request.Profile);

                mergedFrames.Add(image);
            }

            mergedFrames.Write(finalOutputPath);

            if (!File.Exists(finalOutputPath))
                throw new FileNotFoundException("Pdfium PNG pipeline çıktı dosyasını üretmedi.", finalOutputPath);

            long outputBytes = new FileInfo(finalOutputPath).Length;

            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = finalOutputPath,
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
                OutputPath = finalOutputPath,
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
                        File.Delete(tempFile);
                }
                catch
                {
                }
            }
        }
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

    private static string BuildUniqueOutputPath(string originalPath, string pipelineName, string profileName)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? AppContext.BaseDirectory;
        string extension = Path.GetExtension(originalPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        string finalFileName =
            $"{fileNameWithoutExtension}__{pipelineName}__{profileName}__{timestamp}{extension}";

        return Path.Combine(directory, finalFileName);
    }
}