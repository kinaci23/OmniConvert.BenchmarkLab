using OmniConvert.BenchmarkLab.Core;
using Aspose.Cells;
using Aspose.Cells.Rendering;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class AsposeCellsDirectTiffPipeline : IConversionPipeline
{
    public string Name => "AsposeCellsDirectTiffPipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Excel;
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
                    $"{Name} sadece Excel kaynaklarını destekler. Gelen kaynak tipi: {request.SourceType}");
            }

            if (!File.Exists(request.InputPath))
            {
                throw new FileNotFoundException("Excel input dosyası bulunamadı.", request.InputPath);
            }

            string extension = Path.GetExtension(request.InputPath);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    $"{Name} şu an sadece .xlsx destekler. Gelen uzantı: {extension}");
            }

            string? outputDirectory = Path.GetDirectoryName(finalOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var workbook = new Workbook(request.InputPath);
            var options = BuildImageOptions(request.Profile);

            var firstWorksheet = workbook.Worksheets[0];
            var renderer = new SheetRender(firstWorksheet, options);

            renderer.ToTiff(finalOutputPath);

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

    private static ImageOrPrintOptions BuildImageOptions(ConversionProfile profile)
    {
        var options = new ImageOrPrintOptions
        {
            SaveFormat = SaveFormat.Tiff,
            HorizontalResolution = profile.Dpi,
            VerticalResolution = profile.Dpi,
            OnePagePerSheet = false,
            TiffCompression = MapCompression(profile),
            TiffColorDepth = MapColorDepth(profile)
        };

        if (profile.Compression == TiffCompressionKind.Jpeg && profile.JpegQuality.HasValue)
        {
            options.Quality = profile.JpegQuality.Value;
        }

        return options;
    }

    private static Aspose.Cells.Rendering.TiffCompression MapCompression(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.Lzw => Aspose.Cells.Rendering.TiffCompression.CompressionLZW,
            TiffCompressionKind.Ccitt4 => Aspose.Cells.Rendering.TiffCompression.CompressionCCITT4,
            TiffCompressionKind.Jpeg => Aspose.Cells.Rendering.TiffCompression.CompressionLZW,
            _ => Aspose.Cells.Rendering.TiffCompression.CompressionLZW
        };
    }

    private static Aspose.Cells.Rendering.ColorDepth MapColorDepth(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => Aspose.Cells.Rendering.ColorDepth.Format1bpp,
            TargetColorMode.Grayscale8Bit => Aspose.Cells.Rendering.ColorDepth.Format8bpp,
            TargetColorMode.Rgb24Bit => Aspose.Cells.Rendering.ColorDepth.Format24bpp,
            _ => Aspose.Cells.Rendering.ColorDepth.Default
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