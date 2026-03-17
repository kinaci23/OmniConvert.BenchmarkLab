using System.Diagnostics;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Pipelines;

public sealed class LibreOfficeExcelPdfBridgePipeline : IConversionPipeline
{
    private static readonly string[] LibreOfficeCandidatePaths =
    {
        @"C:\Program Files\LibreOffice\program\soffice.exe",
        @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
    };

    private readonly GhostscriptScaledPipeline _ghostscriptPipeline = new();

    public string Name => "LibreOfficeExcelPdfBridgePipeline";

    public bool CanHandle(ConversionRequest request)
    {
        return request.SourceType == ConversionSourceType.Excel;
    }

    public async Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        string finalOutputPath = BuildUniqueOutputPath(request.OutputPath, Name, request.Profile.Name);

        string? runWorkDirectoryToCleanup = null;

        string outputDirectory = Path.GetDirectoryName(finalOutputPath) ?? AppContext.BaseDirectory;

        string tempWorkRoot = Path.Combine(outputDirectory, "temp-work");
        Directory.CreateDirectory(tempWorkRoot);

        string runWorkDirectory = Path.Combine(
            tempWorkRoot,
            $"{Path.GetFileNameWithoutExtension(request.InputPath)}_{Guid.NewGuid():N}");

        Directory.CreateDirectory(runWorkDirectory);
        runWorkDirectoryToCleanup = runWorkDirectory;

        string tempPdfPath = Path.Combine(
            runWorkDirectory,
            $"{Path.GetFileNameWithoutExtension(request.InputPath)}.pdf");

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

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string libreOfficeExePath = ResolveLibreOfficeExePath();

            string tempProfilePath = Path.Combine(runWorkDirectory, "lo-profile");
            Directory.CreateDirectory(tempProfilePath);

            string tempProfileUri = new Uri(tempProfilePath).AbsoluteUri;

            string outputDirectoryForPdf = Path.GetDirectoryName(tempPdfPath)
                ?? throw new InvalidOperationException("Temp PDF klasörü çözümlenemedi.");

            string arguments =
                $"--headless " +
                $"--nologo " +
                $"--norestore " +
                $"--nolockcheck " +
                $"--nodefault " +
                $"--convert-to pdf " +
                $"--outdir \"{outputDirectoryForPdf}\" " +
                $"\"{request.InputPath}\" " +
                $"-env:UserInstallation={tempProfileUri}";

            Console.WriteLine($"[LO-EXCEL] Input : {request.InputPath}");
            Console.WriteLine($"[LO-EXCEL] TempPdf : {tempPdfPath}");
            Console.WriteLine($"[LO-EXCEL] FinalOutput : {finalOutputPath}");
            Console.WriteLine($"[LO-EXCEL] Profile : {request.Profile.Name}");
            Console.WriteLine($"[LO-EXCEL] Args : {arguments}");

            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = Stopwatch.StartNew();

            var startInfo = new ProcessStartInfo
            {
                FileName = libreOfficeExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.Start();

            string standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string standardError = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            stopwatch.Stop();

            Console.WriteLine($"[LO-EXCEL] ExitCode : {process.ExitCode}");

            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                Console.WriteLine($"[LO-EXCEL] STDOUT : {standardOutput}");
            }

            if (!string.IsNullOrWhiteSpace(standardError))
            {
                Console.WriteLine($"[LO-EXCEL] STDERR : {standardError}");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"LibreOffice process başarısız oldu. ExitCode={process.ExitCode}, STDERR={standardError}");
            }

            if (!File.Exists(tempPdfPath))
            {
                throw new FileNotFoundException(
                    $"LibreOffice çalıştı ama beklenen PDF oluşmadı: {tempPdfPath}");
            }

            long pdfBytes = new FileInfo(tempPdfPath).Length;

            Console.WriteLine($"[LO-EXCEL] PDF üretildi. Boyut: {pdfBytes} bytes");
            Console.WriteLine("[LO-EXCEL] Ghostscript bridge başlatılıyor...");

            var ghostscriptRequest = new ConversionRequest
            {
                ScenarioName = $"{request.ScenarioName} | LO->PDF->GS",
                SourceType = ConversionSourceType.Pdf,
                InputPath = tempPdfPath,
                OutputPath = request.OutputPath,
                Profile = request.Profile,
                PageIndex = null
            };

            var ghostscriptResult = await _ghostscriptPipeline.ExecuteAsync(
                ghostscriptRequest,
                cancellationToken);

            if (!ghostscriptResult.Success)
            {
                throw new InvalidOperationException(
                    $"Ghostscript bridge başarısız oldu: {ghostscriptResult.ErrorMessage}");
            }

            return new ConversionExecutionResult
            {
                ScenarioName = request.ScenarioName,
                OutputPath = ghostscriptResult.OutputPath,
                Success = true,
                ErrorMessage = null,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds + ghostscriptResult.ElapsedMilliseconds,
                PeakPrivateBytes = ghostscriptResult.PeakPrivateBytes,
                FinalPrivateBytes = ghostscriptResult.FinalPrivateBytes,
                OutputFileBytes = ghostscriptResult.OutputFileBytes,
                Validation = ghostscriptResult.Validation
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
            if (!string.IsNullOrWhiteSpace(runWorkDirectoryToCleanup) &&
                Directory.Exists(runWorkDirectoryToCleanup))
            {
                try
                {
                    Directory.Delete(runWorkDirectoryToCleanup, recursive: true);
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"[LO-EXCEL] Cleanup warning: {cleanupEx.Message}");
                }
            }
        }
    }

    private static string ResolveLibreOfficeExePath()
    {
        foreach (var candidate in LibreOfficeCandidatePaths)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "LibreOffice executable bulunamadı. Kontrol edilen pathler: " +
            string.Join(", ", LibreOfficeCandidatePaths));
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