using System.Globalization;
using System.Text;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Reporting;

public sealed class CsvBenchmarkReporter
{
    public void AppendSummary(
    string csvPath,
    BenchmarkSummary summary,
    string engineName,
    string inputFile,
    string inputCategory,
    string profileName,
    string intent,
    string pipelineType,
    string outputPath,
    int dpi,
    string colorMode,
    string compression,
    string benchmarkStatus)
    {
        bool fileExists = File.Exists(csvPath);

        string? directory = Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var stream = new FileStream(csvPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, Encoding.UTF8);

        if (!fileExists)
        {
            writer.WriteLine(
                "Timestamp;Engine;InputFile;InputCategory;Profile;Intent;PipelineType;Dpi;ColorMode;Compression;BenchmarkStatus;OutputPath;Title;TotalRuns;SuccessfulRuns;FailedRuns;ErrorCount;MedianElapsedMs;P95ElapsedMs;MedianPeakPrivateRamMb;MedianOutputFileSizeMb");
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        string line = string.Join(";",
            Escape(timestamp),
            Escape(engineName),
            Escape(inputFile),
            Escape(inputCategory),
            Escape(profileName),
            Escape(intent),
            Escape(pipelineType),
            dpi.ToString(CultureInfo.InvariantCulture),
            Escape(colorMode),
            Escape(compression),
            Escape(benchmarkStatus),
            Escape(outputPath),
            Escape(summary.Title),
            summary.TotalRuns.ToString(CultureInfo.InvariantCulture),
            summary.SuccessfulRuns.ToString(CultureInfo.InvariantCulture),
            summary.FailedRuns.ToString(CultureInfo.InvariantCulture),
            summary.Errors.Count.ToString(CultureInfo.InvariantCulture),
            summary.MedianElapsedMs.ToString("F2", CultureInfo.InvariantCulture),
            summary.P95ElapsedMs.ToString("F2", CultureInfo.InvariantCulture),
            summary.MedianPeakPrivateRamMb.ToString("F2", CultureInfo.InvariantCulture),
            summary.MedianOutputFileSizeMb.ToString("F2", CultureInfo.InvariantCulture)
        );

        writer.WriteLine(line);
    }

    private static string Escape(string value)
    {
        if (value.Contains('"'))
            value = value.Replace("\"", "\"\"");

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value}\"";

        return value;
    }
}