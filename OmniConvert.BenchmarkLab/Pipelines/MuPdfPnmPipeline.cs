using System.Diagnostics;
using ImageMagick;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class MuPdfPnmPipeline : IConversionPipeline
{
    private const string MuToolExePath = @"C:\Users\Arda\Desktop\MuPDF\mutool.exe";

    public string Name => "MuPdfPnmPipeline";

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
            if (!File.Exists(MuToolExePath))
                throw new FileNotFoundException("mutool.exe bulunamadı.", MuToolExePath);

            Console.WriteLine($"[MUPDF-PNM] Input     : {request.InputPath}");
            Console.WriteLine($"[MUPDF-PNM] Output    : {finalOutputPath}");
            Console.WriteLine($"[MUPDF-PNM] Profile   : {request.Profile.Name}");

            string colorSpace = ResolveColorSpace(request.Profile);
            string extension = ResolveIntermediateExtension(request.Profile);

            string tempPattern = Path.Combine(
                Path.GetTempPath(),
                $"omniconvert_mupdf_pnm_{Guid.NewGuid():N}_%d.{extension}");

            string arguments =
                $"draw " +
                $"-L " +
                $"-r {request.Profile.Dpi} " +
                $"-c {colorSpace} " +
                $"-o \"{tempPattern}\" " +
                $"\"{request.InputPath}\" " +
                $"1-N";

            Console.WriteLine($"[MUPDF-PNM] Args      : {arguments}");

            var startInfo = new ProcessStartInfo
            {
                FileName = MuToolExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            Task<string> stdOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stdErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(stdOutputTask, stdErrorTask, process.WaitForExitAsync(cancellationToken));

            string stdOutput = stdOutputTask.Result;
            string stdError = stdErrorTask.Result;

            Console.WriteLine($"[MUPDF-PNM] ExitCode  : {process.ExitCode}");

            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                Console.WriteLine("[MUPDF-PNM] STDOUT:");
                Console.WriteLine(stdOutput);
            }

            if (!string.IsNullOrWhiteSpace(stdError))
            {
                Console.WriteLine("[MUPDF-PNM] STDERR:");
                Console.WriteLine(stdError);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"MuPDF PNM process başarısız oldu. ExitCode={process.ExitCode}{Environment.NewLine}{stdError}");
            }

            string tempDirectory = Path.GetDirectoryName(tempPattern)!;
            string tempBaseName = Path.GetFileNameWithoutExtension(tempPattern).Replace("%d", string.Empty);

            tempFiles = Directory
                .GetFiles(tempDirectory, $"omniconvert_mupdf_pnm_*.{extension}", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith(tempBaseName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => ExtractPageNumber(x))
                .ToList();

            if (tempFiles.Count == 0)
                throw new FileNotFoundException("MuPDF PNM geçici çıktıları bulunamadı.");

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
                throw new FileNotFoundException("MuPDF PNM pipeline çıktı dosyasını üretmedi.", finalOutputPath);

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

    private static string ResolveColorSpace(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => "mono",
            TargetColorMode.Grayscale8Bit => "gray",
            TargetColorMode.Rgb24Bit => "rgb",
            _ => throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}")
        };
    }

    private static string ResolveIntermediateExtension(ConversionProfile profile)
    {
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => "pbm",
            TargetColorMode.Grayscale8Bit => "pgm",
            TargetColorMode.Rgb24Bit => "ppm",
            _ => throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}")
        };
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

    private static int ExtractPageNumber(string filePath)
    {
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        int lastUnderscoreIndex = fileNameWithoutExtension.LastIndexOf('_');

        if (lastUnderscoreIndex < 0)
            return int.MaxValue;

        string pagePart = fileNameWithoutExtension[(lastUnderscoreIndex + 1)..];

        return int.TryParse(pagePart, out int pageNumber)
            ? pageNumber
            : int.MaxValue;
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