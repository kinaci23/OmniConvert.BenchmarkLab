using ImageMagick;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class RasterMagickPipeline : IConversionPipeline
{
    public string Name => "RasterMagickPipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Raster;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var image = new MagickImage(request.InputPath);

            ApplyProfile(image, request.Profile);

            image.Write(request.OutputPath);

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
    }

    private static void ApplyProfile(MagickImage image, ConversionProfile profile)
    {
        image.Density = new Density(profile.Dpi, profile.Dpi);
        image.Format = MagickFormat.Tiff;

        switch (profile.ColorMode)
        {
            case TargetColorMode.Binary1Bit:
                image.Grayscale();

                if (profile.Threshold.HasValue)
                {
                    image.Threshold(new Percentage(profile.Threshold.Value / 255.0 * 100.0));
                }
                else
                {
                    image.Threshold(new Percentage(50));
                }

                break;

            case TargetColorMode.Grayscale8Bit:
                image.Grayscale();
                break;

            case TargetColorMode.Rgb24Bit:
                break;

            default:
                throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}");
        }

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