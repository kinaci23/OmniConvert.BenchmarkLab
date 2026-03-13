using System.Diagnostics;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class GhostscriptScaledPipeline : IConversionPipeline
{
    private const string GhostscriptExePath = @"C:\Program Files\gs\gs10.06.0\bin\gswin64c.exe";

    public string Name => "GhostscriptScaledPipeline";

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
            if (!File.Exists(GhostscriptExePath))
            {
                throw new FileNotFoundException("Ghostscript executable bulunamadı.", GhostscriptExePath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            string device = ResolveGhostscriptDevice(request.Profile);
            string compressionArguments = ResolveCompressionArguments(request.Profile);
            string colorArguments = ResolveColorArguments(request.Profile);

            string arguments =
                $"-dBATCH " +
                $"-dNOPAUSE " +
                $"-dSAFER " +
                $"-sDEVICE={device} " +
                $"-r{request.Profile.Dpi} " +
                $"{colorArguments} " +
                $"{compressionArguments} " +
                $"-sOutputFile=\"{finalOutputPath}\" " +
                $"\"{request.InputPath}\"";

            Console.WriteLine($"[GS-SCALED] Input     : {request.InputPath}");
            Console.WriteLine($"[GS-SCALED] Output    : {finalOutputPath}");
            Console.WriteLine($"[GS-SCALED] Profile   : {request.Profile.Name}");
            Console.WriteLine($"[GS-SCALED] Device    : {device}");
            Console.WriteLine($"[GS-SCALED] Compression: {request.Profile.Compression}");
            Console.WriteLine($"[GS-SCALED] Args      : {arguments}");

            var startInfo = new ProcessStartInfo
            {
                FileName = GhostscriptExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            string stdOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string stdError = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            Console.WriteLine($"[GS-SCALED] ExitCode  : {process.ExitCode}");

            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                Console.WriteLine("[GS-SCALED] STDOUT:");
                Console.WriteLine(stdOutput);
            }

            if (!string.IsNullOrWhiteSpace(stdError))
            {
                Console.WriteLine("[GS-SCALED] STDERR:");
                Console.WriteLine(stdError);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Ghostscript scaled process başarısız oldu. ExitCode={process.ExitCode}{Environment.NewLine}{stdError}");
            }

            if (!File.Exists(finalOutputPath))
            {
                throw new FileNotFoundException("Ghostscript scaled çıktı dosyasını üretmedi.", finalOutputPath);
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

    private static string ResolveGhostscriptDevice(ConversionProfile profile)
    {
        if (profile.ColorMode == TargetColorMode.Binary1Bit)
            return "tiffscaled";

        if (profile.ColorMode == TargetColorMode.Grayscale8Bit)
            return "tiffscaled8";

        if (profile.ColorMode == TargetColorMode.Rgb24Bit)
            return profile.Compression == TiffCompressionKind.Jpeg
                ? "tiff24nc"
                : "tiffscaled24";

        throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}");
    }

    private static string ResolveCompressionArguments(ConversionProfile profile)
    {
        return profile.Compression switch
        {
            TiffCompressionKind.None => "-sCompression=none",
            TiffCompressionKind.Lzw => "-sCompression=lzw",
            TiffCompressionKind.Jpeg => $"-dJPEGQ={(profile.JpegQuality ?? 85)}",
            TiffCompressionKind.Ccitt4 => "-sCompression=g4",
            _ => throw new NotSupportedException($"Desteklenmeyen Compression: {profile.Compression}")
        };
    }

    private static string ResolveColorArguments(ConversionProfile profile)
    {
        return string.Empty;
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