using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Reporting;

public sealed class ParallelConsoleReporter
{
    public void PrintSummary(ParallelBenchmarkSummary summary)
    {
        Console.WriteLine("==============================================================");
        Console.WriteLine(summary.Title);
        Console.WriteLine("==============================================================");

        Console.WriteLine($"Worker Sayısı     : {summary.WorkerCount}");
        Console.WriteLine($"Toplam Operasyon  : {summary.TotalOperations}");
        Console.WriteLine($"Başarılı          : {summary.SuccessfulOperations}");
        Console.WriteLine($"Başarısız         : {summary.FailedOperations}");
        Console.WriteLine($"Toplam Süre       : {summary.TotalElapsedMs:N2} ms");
        Console.WriteLine($"Throughput        : {summary.ThroughputOpsPerSecond:N2} ops/sec");
        Console.WriteLine();

        Console.WriteLine("SÜRE (ms)");
        Console.WriteLine($"Min              : {summary.MinElapsedMs:N0}");
        Console.WriteLine($"Median           : {summary.MedianElapsedMs:N2}");
        Console.WriteLine($"P95              : {summary.P95ElapsedMs:N2}");
        Console.WriteLine($"Max              : {summary.MaxElapsedMs:N0}");
        Console.WriteLine();

        Console.WriteLine("PEAK PRIVATE RAM (MB)");
        Console.WriteLine($"Max              : {summary.MaxPeakPrivateRamMb:N2}");
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