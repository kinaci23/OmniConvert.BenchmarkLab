using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Reporting;

public sealed class ConsoleReporter
{
    public void PrintSummary(BenchmarkSummary summary)
    {
        Console.WriteLine("==============================================================");
        Console.WriteLine(summary.Title);
        Console.WriteLine("==============================================================");

        Console.WriteLine($"Toplam Run        : {summary.TotalRuns}");
        Console.WriteLine($"Başarılı          : {summary.SuccessfulRuns}");
        Console.WriteLine($"Başarısız         : {summary.FailedRuns}");
        Console.WriteLine($"Geçersiz Output   : {summary.Errors.Count(x => x.Contains("VALIDATION:", StringComparison.OrdinalIgnoreCase))}");
        Console.WriteLine();

        Console.WriteLine("SÜRE (ms)");
        Console.WriteLine($"Min              : {summary.MinElapsedMs:N0}");
        Console.WriteLine($"Median           : {summary.MedianElapsedMs:N2}");
        Console.WriteLine($"P95              : {summary.P95ElapsedMs:N2}");
        Console.WriteLine($"Max              : {summary.MaxElapsedMs:N0}");
        Console.WriteLine();

        Console.WriteLine("PEAK PRIVATE RAM (MB)");
        Console.WriteLine($"Min              : {summary.MinPeakPrivateRamMb:N2}");
        Console.WriteLine($"Median           : {summary.MedianPeakPrivateRamMb:N2}");
        Console.WriteLine($"P95              : {summary.P95PeakPrivateRamMb:N2}");
        Console.WriteLine($"Max              : {summary.MaxPeakPrivateRamMb:N2}");
        Console.WriteLine();

        Console.WriteLine("OUTPUT DOSYA BOYUTU (MB)");
        Console.WriteLine($"Min              : {summary.MinOutputFileSizeMb:N2}");
        Console.WriteLine($"Median           : {summary.MedianOutputFileSizeMb:N2}");
        Console.WriteLine($"P95              : {summary.P95OutputFileSizeMb:N2}");
        Console.WriteLine($"Max              : {summary.MaxOutputFileSizeMb:N2}");
        Console.WriteLine();

        if (summary.Errors.Count > 0)
        {
            Console.WriteLine("HATALAR");
            foreach (var error in summary.Errors)
            {
                Console.WriteLine($"- {error}");
            }

            Console.WriteLine();
        }
    }
}