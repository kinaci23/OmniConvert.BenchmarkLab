using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Benchmarking;

public static class BenchmarkStatistics
{
    public static double Median(IEnumerable<long> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();

        if (ordered.Length == 0)
            return 0;

        int middle = ordered.Length / 2;

        if (ordered.Length % 2 == 0)
            return (ordered[middle - 1] + ordered[middle]) / 2.0;

        return ordered[middle];
    }

    public static double Percentile(IEnumerable<long> values, double percentile)
    {
        var ordered = values.OrderBy(x => x).ToArray();

        if (ordered.Length == 0)
            return 0;

        double rank = (percentile / 100.0) * (ordered.Length - 1);

        int low = (int)Math.Floor(rank);
        int high = (int)Math.Ceiling(rank);

        if (low == high)
            return ordered[low];

        double weight = rank - low;

        return ordered[low] + (ordered[high] - ordered[low]) * weight;
    }

    public static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();

        if (ordered.Length == 0)
            return 0;

        int middle = ordered.Length / 2;

        if (ordered.Length % 2 == 0)
            return (ordered[middle - 1] + ordered[middle]) / 2.0;

        return ordered[middle];
    }

    public static double Percentile(IEnumerable<double> values, double percentile)
    {
        var ordered = values.OrderBy(x => x).ToArray();

        if (ordered.Length == 0)
            return 0;

        double rank = (percentile / 100.0) * (ordered.Length - 1);

        int low = (int)Math.Floor(rank);
        int high = (int)Math.Ceiling(rank);

        if (low == high)
            return ordered[low];

        double weight = rank - low;

        return ordered[low] + (ordered[high] - ordered[low]) * weight;
    }

    public static BenchmarkSummary BuildSummary(string title, IReadOnlyList<ConversionExecutionResult> results)
    {
        var successResults = results.Where(x => x.Success).ToList();
        var failedResults = results.Where(x => !x.Success).ToList();

        var elapsedValues = successResults.Select(x => x.ElapsedMilliseconds).ToList();
        var peakRamValuesMb = successResults.Select(x => BytesToMb(x.PeakPrivateBytes)).ToList();
        var outputSizeValuesMb = successResults.Select(x => BytesToMb(x.OutputFileBytes)).ToList();

        return new BenchmarkSummary
        {
            Title = title,
            TotalRuns = results.Count,
            SuccessfulRuns = successResults.Count,
            FailedRuns = failedResults.Count,

            MinElapsedMs = elapsedValues.Count > 0 ? elapsedValues.Min() : 0,
            MedianElapsedMs = Median(elapsedValues),
            P95ElapsedMs = Percentile(elapsedValues, 95),
            MaxElapsedMs = elapsedValues.Count > 0 ? elapsedValues.Max() : 0,

            MinPeakPrivateRamMb = peakRamValuesMb.Count > 0 ? peakRamValuesMb.Min() : 0,
            MedianPeakPrivateRamMb = Median(peakRamValuesMb),
            P95PeakPrivateRamMb = Percentile(peakRamValuesMb, 95),
            MaxPeakPrivateRamMb = peakRamValuesMb.Count > 0 ? peakRamValuesMb.Max() : 0,

            MinOutputFileSizeMb = outputSizeValuesMb.Count > 0 ? outputSizeValuesMb.Min() : 0,
            MedianOutputFileSizeMb = Median(outputSizeValuesMb),
            P95OutputFileSizeMb = Percentile(outputSizeValuesMb, 95),
            MaxOutputFileSizeMb = outputSizeValuesMb.Count > 0 ? outputSizeValuesMb.Max() : 0,

            Errors = failedResults
                .Select(x => x.ErrorMessage ?? "Bilinmeyen hata")
                .Concat(successResults
                    .Where(x => x.Validation is not null && !x.Validation.IsValid)
                    .Select(x => $"VALIDATION: {x.Validation!.Message}"))
                .ToList()
        };
    }

    private static double BytesToMb(long bytes)
    {
        return bytes / (1024.0 * 1024.0);
    }
}