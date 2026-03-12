using Aspose.Pdf;
using Aspose.Pdf.Devices;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class AsposePdfPipeline : IConversionPipeline
{
    public string Name => "AsposePdfPipeline";

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

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"[ASPOSE] Input     : {request.InputPath}");
            Console.WriteLine($"[ASPOSE] Output    : {finalOutputPath}");
            Console.WriteLine($"[ASPOSE] Profile   : {request.Profile.Name}");

            var document = new Document(request.InputPath);

            var resolution = new Resolution(request.Profile.Dpi);
            var settings = BuildTiffSettings(request.Profile);

            var device = new TiffDevice(resolution, settings);

            await Task.Run(() =>
            {
                int pageCountToProcess = Math.Min(document.Pages.Count, 4);
                device.Process(document, 1, pageCountToProcess, finalOutputPath);
            }, cancellationToken);

            if (!File.Exists(finalOutputPath))
            {
                throw new FileNotFoundException("Aspose çıktı dosyasını üretmedi.", finalOutputPath);
            }

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
    }

    private static TiffSettings BuildTiffSettings(ConversionProfile profile)
    {
        var settings = new TiffSettings
        {
            Compression = ResolveCompression(profile),
            Depth = ResolveDepth(profile),
            SkipBlankPages = false
        };

        return settings;
    }

    private static CompressionType ResolveCompression(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.None => CompressionType.None,
            TiffCompressionKind.Lzw => CompressionType.LZW,
            TiffCompressionKind.Ccitt4 => CompressionType.CCITT4,
            TiffCompressionKind.Jpeg => CompressionType.LZW,
            _ => throw new NotSupportedException($"Desteklenmeyen Compression: {profile.Compression}")
        };
    }

    private static ColorDepth ResolveDepth(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => ColorDepth.Format1bpp,
            TargetColorMode.Grayscale8Bit => ColorDepth.Format8bpp,
            TargetColorMode.Rgb24Bit => ColorDepth.Default,
            _ => throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}")
        };
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