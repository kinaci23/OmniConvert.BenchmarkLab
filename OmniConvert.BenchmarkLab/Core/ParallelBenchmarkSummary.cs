namespace OmniConvert.BenchmarkLab.Core;

public sealed record ParallelBenchmarkSummary
{
    public required string Title { get; init; }

    public required int WorkerCount { get; init; }
    public required int TotalOperations { get; init; }
    public required int SuccessfulOperations { get; init; }
    public required int FailedOperations { get; init; }

    public required double TotalElapsedMs { get; init; }
    public required double ThroughputOpsPerSecond { get; init; }

    public required double MinElapsedMs { get; init; }
    public required double MedianElapsedMs { get; init; }
    public required double P95ElapsedMs { get; init; }
    public required double MaxElapsedMs { get; init; }

    public required double MaxPeakPrivateRamMb { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}