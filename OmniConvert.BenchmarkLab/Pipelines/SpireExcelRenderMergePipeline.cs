using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OmniConvert.BenchmarkLab.Core;
using Spire.Xls;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class SpireExcelRenderMergePipeline : IConversionPipeline
{
    public string Name => "SpireExcelRenderMergePipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Excel;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        string finalOutputPath = BuildUniqueOutputPath(request.OutputPath, Name, request.Profile.Name);

        var createdImages = new List<Image>();
        string? runWorkDirectoryToCleanup = null;

        try
        {
            if (!CanHandle(request))
            {
                throw new NotSupportedException(
                    $"{Name} sadece Excel kaynaklarını destekler. Gelen kaynak tipi: {request.SourceType}");
            }

            if (!File.Exists(request.InputPath))
            {
                throw new FileNotFoundException("Excel input dosyası bulunamadı.", request.InputPath);
            }

            string extension = Path.GetExtension(request.InputPath);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"{Name} şu an sadece .xlsx / .xls destekler. Gelen uzantı: {extension}");
            }

            string outputDirectory = Path.GetDirectoryName(finalOutputPath) ?? AppContext.BaseDirectory;
            Directory.CreateDirectory(outputDirectory);

            string tempWorkRoot = Path.Combine(outputDirectory, "temp-work");
            Directory.CreateDirectory(tempWorkRoot);

            string runWorkDirectory = Path.Combine(
                tempWorkRoot,
                $"{Path.GetFileNameWithoutExtension(request.InputPath)}_{Guid.NewGuid():N}");

            Directory.CreateDirectory(runWorkDirectory);
            runWorkDirectoryToCleanup = runWorkDirectory;

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();

            var workbook = new Workbook();
            workbook.LoadFromFile(request.InputPath);

            workbook.ConverterSetting.XDpi = request.Profile.Dpi;
            workbook.ConverterSetting.YDpi = request.Profile.Dpi;
            workbook.ConverterSetting.IsReCalculateOnConvert = true;
            workbook.ConverterSetting.ClearCacheOnConverted = true;
            workbook.ConverterSetting.SheetFitToPage = false;
            workbook.ConverterSetting.SheetFitToWidth = false;

            for (int i = 0; i < workbook.Worksheets.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sheet = workbook.Worksheets[i];
                string tempImagePath = Path.Combine(runWorkDirectory, $"sheet_{i + 1}.png");

                // Spire resmi örnekte tüm worksheet için SaveToImage(path) kullanıyor.
                sheet.SaveToImage(tempImagePath);

                using var sourceImage = Image.FromFile(tempImagePath);
                Image prepared = PrepareFrame(sourceImage, request.Profile);
                createdImages.Add(prepared);
            }

            if (createdImages.Count == 0)
            {
                throw new InvalidOperationException("Spire hiçbir worksheet render edemedi.");
            }

            SaveAsMultipageTiff(createdImages, finalOutputPath, request.Profile);

            stopwatch.Stop();

            long outputBytes = File.Exists(finalOutputPath)
                ? new FileInfo(finalOutputPath).Length
                : 0;

            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = finalOutputPath,
                Success = File.Exists(finalOutputPath),
                ErrorMessage = File.Exists(finalOutputPath) ? null : "Spire çıktı TIFF dosyasını oluşturmadı.",
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                PeakPrivateBytes = 0,
                FinalPrivateBytes = 0,
                OutputFileBytes = outputBytes,
                Validation = null
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
                OutputFileBytes = 0,
                Validation = null
            };
        }
        finally
        {
            foreach (var image in createdImages)
            {
                image.Dispose();
            }

            if (!string.IsNullOrWhiteSpace(runWorkDirectoryToCleanup) &&
                Directory.Exists(runWorkDirectoryToCleanup))
            {
                try
                {
                    Directory.Delete(runWorkDirectoryToCleanup, recursive: true);
                }
                catch
                {
                    // cleanup best-effort
                }
            }
        }

        await Task.CompletedTask;
    }

    private static Image PrepareFrame(Image source, ConversionProfile profile)
    {
        using var sourceBitmap = new Bitmap(source);

        Bitmap prepared = profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => ConvertToBinary1Bpp(sourceBitmap, profile.Threshold ?? 180),
            TargetColorMode.Grayscale8Bit => ConvertToGrayscale24Bpp(sourceBitmap),
            _ => new Bitmap(sourceBitmap)
        };

        prepared.SetResolution(profile.Dpi, profile.Dpi);
        return prepared;
    }

    private static Bitmap ConvertToGrayscale24Bpp(Bitmap source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);

        using (Graphics graphics = Graphics.FromImage(result))
        using (var attributes = new ImageAttributes())
        {
            var colorMatrix = new ColorMatrix(new float[][]
            {
                new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                new[] { 0f,     0f,     0f,     1f, 0f },
                new[] { 0f,     0f,     0f,     0f, 1f }
            });

            attributes.SetColorMatrix(colorMatrix);
            graphics.DrawImage(
                source,
                new Rectangle(0, 0, result.Width, result.Height),
                0,
                0,
                source.Width,
                source.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        return result;
    }

    private static Bitmap ConvertToBinary1Bpp(Bitmap source, int threshold)
    {
        using Bitmap gray = ConvertToGrayscale24Bpp(source);

        int width = gray.Width;
        int height = gray.Height;

        var binary = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

        Rectangle grayRect = new Rectangle(0, 0, width, height);
        Rectangle binRect = new Rectangle(0, 0, width, height);

        BitmapData grayData = gray.LockBits(grayRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        BitmapData binData = binary.LockBits(binRect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        try
        {
            int grayStride = grayData.Stride;
            int binStride = binData.Stride;

            byte[] grayBytes = new byte[grayStride * height];
            byte[] binBytes = new byte[binStride * height];

            Marshal.Copy(grayData.Scan0, grayBytes, 0, grayBytes.Length);

            for (int y = 0; y < height; y++)
            {
                int grayRow = y * grayStride;
                int binRow = y * binStride;

                for (int x = 0; x < width; x++)
                {
                    byte grayValue = grayBytes[grayRow + (x * 3)];
                    bool isBlack = grayValue < threshold;

                    if (isBlack)
                    {
                        binBytes[binRow + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                    }
                }
            }

            Marshal.Copy(binBytes, 0, binData.Scan0, binBytes.Length);
        }
        finally
        {
            gray.UnlockBits(grayData);
            binary.UnlockBits(binData);
        }

        return binary;
    }

    private static void SaveAsMultipageTiff(IReadOnlyList<Image> images, string outputPath, ConversionProfile profile)
    {
        ImageCodecInfo tiffEncoder = GetEncoderInfo("image/tiff")
            ?? throw new InvalidOperationException("TIFF encoder bulunamadı.");

        using var encoderParameters = new EncoderParameters(2);
        encoderParameters.Param[0] = new EncoderParameter(
            Encoder.Compression,
            (long)MapCompression(profile));
        encoderParameters.Param[1] = new EncoderParameter(
            Encoder.SaveFlag,
            (long)EncoderValue.MultiFrame);

        using Image firstFrame = (Image)images[0].Clone();
        firstFrame.Save(outputPath, tiffEncoder, encoderParameters);

        for (int i = 1; i < images.Count; i++)
        {
            using Image nextFrame = (Image)images[i].Clone();

            encoderParameters.Param[1] = new EncoderParameter(
                Encoder.SaveFlag,
                (long)EncoderValue.FrameDimensionPage);

            firstFrame.SaveAdd(nextFrame, encoderParameters);
        }

        encoderParameters.Param[1] = new EncoderParameter(
            Encoder.SaveFlag,
            (long)EncoderValue.Flush);

        firstFrame.SaveAdd(encoderParameters);
    }

    private static EncoderValue MapCompression(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.Ccitt4 => EncoderValue.CompressionCCITT4,
            TiffCompressionKind.Lzw => EncoderValue.CompressionLZW,
            TiffCompressionKind.Jpeg => EncoderValue.CompressionLZW,
            _ => EncoderValue.CompressionLZW
        };
    }

    private static ImageCodecInfo? GetEncoderInfo(string mimeType)
    {
        return ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(codec => codec.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildUniqueOutputPath(string originalPath, string pipelineName, string profileName)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? AppContext.BaseDirectory;
        string extension = Path.GetExtension(originalPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        string finalFileName = $"{fileNameWithoutExtension}__{pipelineName}__{profileName}__{timestamp}{extension}";
        return Path.Combine(directory, finalFileName);
    }
}