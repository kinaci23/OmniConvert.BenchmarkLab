using System.Diagnostics;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class GhostscriptPipeline : IConversionPipeline
{
    private const string GhostscriptExePath = @"C:\Program Files\gs\gs10.06.0\bin\gswin64c.exe";

    public string Name => "GhostscriptPipeline";

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

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

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

            Console.WriteLine($"[GS] Input     : {request.InputPath}");
            Console.WriteLine($"[GS] Output    : {finalOutputPath}");
            Console.WriteLine($"[GS] Profile   : {request.Profile.Name}");
            Console.WriteLine($"[GS] Device    : {device}");
            Console.WriteLine($"[GS] Args      : {arguments}");

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

            Console.WriteLine($"[GS] ExitCode  : {process.ExitCode}");

            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                Console.WriteLine("[GS] STDOUT:");
                Console.WriteLine(stdOutput);
            }

            if (!string.IsNullOrWhiteSpace(stdError))
            {
                Console.WriteLine("[GS] STDERR:");
                Console.WriteLine(stdError);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Ghostscript process başarısız oldu. ExitCode={process.ExitCode}{Environment.NewLine}{stdError}");
            }

            if (!File.Exists(finalOutputPath))
            {
                throw new FileNotFoundException("Ghostscript çıktı dosyasını üretmedi.", finalOutputPath);
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
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => "tiffg4",
            TargetColorMode.Grayscale8Bit => "tiffgray",
            TargetColorMode.Rgb24Bit => profile.Compression == TiffCompressionKind.Jpeg ? "tiffsep" : "tiff24nc",
            _ => throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}")
        };
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
        return profile.ColorMode switch
        {
            TargetColorMode.Binary1Bit => string.Empty,
            TargetColorMode.Grayscale8Bit => string.Empty,
            TargetColorMode.Rgb24Bit => string.Empty,
            _ => throw new NotSupportedException($"Desteklenmeyen ColorMode: {profile.ColorMode}")
        };
    }

    private static string BuildUniqueOutputPath(string originalPath, string pipelineName, string profileName)
    {
        string directory = Path.GetDirectoryName(originalPath) ?? AppContext.BaseDirectory;
        string extension = Path.GetExtension(originalPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);

        string safePipeline = SanitizeFileNamePart(pipelineName);
        string safeProfile = SanitizeFileNamePart(profileName);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        string finalFileName = $"{fileNameWithoutExtension}__{safePipeline}__{safeProfile}__{timestamp}{extension}";
        return Path.Combine(directory, finalFileName);
    }

    private static string SanitizeFileNamePart(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Replace(' ', '_');
    }
}