using OmniConvert.BenchmarkLab.Core;
using Aspose.Words;
using Aspose.Words.Saving;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class AsposeWordsDirectTiffPipeline : IConversionPipeline
{
    public string Name => "AsposeWordsDirectTiffPipeline";

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
            if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"{Name} şu an sadece .docx destekler. Gelen uzantı: {extension}");
            }

            string? outputDirectory = Path.GetDirectoryName(finalOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var document = new Document(request.InputPath);

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
                ErrorMessage = File.Exists(finalOutputPath) ? null : "Aspose çıktı TIFF dosyasını oluşturmadı.",
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
        var options = new ImageSaveOptions(SaveFormat.Tiff)
        {
            Resolution = profile.Dpi
        };

        options.TiffCompression = MapCompression(profile);
        options.ImageColorMode = MapColorMode(profile);

        return options;
    }

    private static TiffCompression MapCompression(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.Lzw => TiffCompression.Lzw,
            TiffCompressionKind.Ccitt4 => TiffCompression.Ccitt4,
            TiffCompressionKind.Jpeg => TiffCompression.None,
            _ => TiffCompression.Lzw
        };
    }

    private static ImageColorMode MapColorMode(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => ImageColorMode.BlackAndWhite,
            TargetColorMode.Grayscale8Bit => ImageColorMode.Grayscale,
            TargetColorMode.Rgb24Bit => ImageColorMode.None,
            _ => ImageColorMode.None
        };
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