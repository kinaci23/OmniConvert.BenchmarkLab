using System.Diagnostics;
using GemBox.Document;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class GemBoxWordDirectTiffPipeline : IConversionPipeline
{
    private static bool _licenseInitialized;

    public string Name => "GemBoxWordDirectTiffPipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Word;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        string finalOutputPath = BuildUniqueOutputPath(request.OutputPath, Name, request.Profile.Name);

        try
        {
            EnsureLicense();

            if (!CanHandle(request))
            {
                throw new NotSupportedException(
                    $"{Name} sadece Word kaynaklarını destekler. Gelen kaynak tipi: {request.SourceType}");
            }

            if (!File.Exists(request.InputPath))
            {
                throw new FileNotFoundException("Word input dosyası bulunamadı.", request.InputPath);
            }

            string extension = Path.GetExtension(request.InputPath);
            if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".doc", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"{Name} şu an sadece .docx / .doc destekler. Gelen uzantı: {extension}");
            }

            string? outputDirectory = Path.GetDirectoryName(finalOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();

            var document = DocumentModel.Load(request.InputPath);

            var saveOptions = BuildTiffSaveOptions(request.Profile);
            document.Save(finalOutputPath, saveOptions);

            stopwatch.Stop();

            long outputBytes = File.Exists(finalOutputPath)
                ? new FileInfo(finalOutputPath).Length
                : 0;

            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = finalOutputPath,
                Success = File.Exists(finalOutputPath),
                ErrorMessage = File.Exists(finalOutputPath) ? null : "GemBox çıktı TIFF dosyasını oluşturmadı.",
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

        await Task.CompletedTask;
    }

    private static ImageSaveOptions BuildTiffSaveOptions(ConversionProfile profile)
    {
        var options = new ImageSaveOptions(ImageSaveFormat.Tiff)
        {
            DpiX = profile.Dpi,
            DpiY = profile.Dpi,
            PageCount = int.MaxValue,
            TiffCompression = MapCompression(profile),
            PixelFormat = MapPixelFormat(profile)
        };

        return options;
    }

    private static void EnsureLicense()
    {
        if (_licenseInitialized)
            return;

        ComponentInfo.SetLicense("FREE-LIMITED-KEY");
        _licenseInitialized = true;
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

    private static TiffCompression MapCompression(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.Ccitt4 => TiffCompression.Ccitt4,
            TiffCompressionKind.Lzw => TiffCompression.Lzw,
            TiffCompressionKind.Jpeg => TiffCompression.Lzw,
            _ => TiffCompression.Lzw
        };
    }

    private static GemBox.Document.PixelFormat MapPixelFormat(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => GemBox.Document.PixelFormat.BlackWhite,
            TargetColorMode.Grayscale8Bit => GemBox.Document.PixelFormat.Gray8,
            TargetColorMode.Rgb24Bit => GemBox.Document.PixelFormat.Rgb24,
            _ => GemBox.Document.PixelFormat.Rgb24
        };
    }
}