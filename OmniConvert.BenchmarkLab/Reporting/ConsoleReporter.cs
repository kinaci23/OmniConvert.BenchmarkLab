using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Reporting;

public sealed class ConsoleReporter
{
    public void PrintSummary(BenchmarkSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine(summary.Title);
        Console.WriteLine(new string('=', 70));

        Console.WriteLine($"Runs              : {summary.TotalRuns}");
        Console.WriteLine($"Successful        : {summary.SuccessfulRuns}");
        Console.WriteLine($"Failed            : {summary.FailedRuns}");
        Console.WriteLine($"Invalid Outputs   : {summary.Errors.Count(x => x.Contains("VALIDATION:", StringComparison.OrdinalIgnoreCase))}");
        Console.WriteLine();

        Console.WriteLine("[Latency / ms]");
        Console.WriteLine($"  Min             : {summary.MinElapsedMs:N0}");
        Console.WriteLine($"  Median          : {summary.MedianElapsedMs:N2}");
        Console.WriteLine($"  P95             : {summary.P95ElapsedMs:N2}");
        Console.WriteLine($"  Max             : {summary.MaxElapsedMs:N0}");
        Console.WriteLine();

        Console.WriteLine("[Peak Private RAM / MB]");
        Console.WriteLine($"  Min             : {summary.MinPeakPrivateRamMb:N2}");
        Console.WriteLine($"  Median          : {summary.MedianPeakPrivateRamMb:N2}");
        Console.WriteLine($"  P95             : {summary.P95PeakPrivateRamMb:N2}");
        Console.WriteLine($"  Max             : {summary.MaxPeakPrivateRamMb:N2}");
        Console.WriteLine();

        Console.WriteLine("[Output Size / MB]");
        Console.WriteLine($"  Min             : {summary.MinOutputFileSizeMb:N2}");
        Console.WriteLine($"  Median          : {summary.MedianOutputFileSizeMb:N2}");
        Console.WriteLine($"  P95             : {summary.P95OutputFileSizeMb:N2}");
        Console.WriteLine($"  Max             : {summary.MaxOutputFileSizeMb:N2}");

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