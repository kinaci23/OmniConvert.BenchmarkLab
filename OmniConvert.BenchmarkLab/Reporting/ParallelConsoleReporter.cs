using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Reporting;

public sealed class ParallelConsoleReporter
{
    public void PrintSummary(ParallelBenchmarkSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine(summary.Title);
        Console.WriteLine(new string('=', 70));

        Console.WriteLine($"Workers           : {summary.WorkerCount}");
        Console.WriteLine($"Total Operations  : {summary.TotalOperations}");
        Console.WriteLine($"Successful        : {summary.SuccessfulOperations}");
        Console.WriteLine($"Failed            : {summary.FailedOperations}");
        Console.WriteLine($"Total Elapsed     : {summary.TotalElapsedMs:N2} ms");
        Console.WriteLine($"Throughput        : {summary.ThroughputOpsPerSecond:N2} ops/sec");
        Console.WriteLine($"Speedup           : {summary.Speedup:N2}x");
        Console.WriteLine($"Efficiency        : {summary.EfficiencyPercent:N2} %");
        Console.WriteLine();

        Console.WriteLine("[Latency / ms]");
        Console.WriteLine($"  Min             : {summary.MinElapsedMs:N0}");
        Console.WriteLine($"  Median          : {summary.MedianElapsedMs:N2}");
        Console.WriteLine($"  P95             : {summary.P95ElapsedMs:N2}");
        Console.WriteLine($"  Max             : {summary.MaxElapsedMs:N0}");
        Console.WriteLine();

        Console.WriteLine("[Peak Private RAM / MB]");
        Console.WriteLine($"  Max             : {summary.MaxPeakPrivateRamMb:N2}");

        if (summary.Errors.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("[Errors]");
            foreach (var error in summary.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        Console.WriteLine(new string('=', 70));
    }
}