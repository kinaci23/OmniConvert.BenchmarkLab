namespace OmniConvert.BenchmarkLab.Core;

public sealed record BenchmarkSummary
{
    public required string Title { get; init; }

    public required int TotalRuns { get; init; }
    public required int SuccessfulRuns { get; init; }
    public required int FailedRuns { get; init; }

    public required double MinElapsedMs { get; init; }
    public required double MedianElapsedMs { get; init; }
    public required double P95ElapsedMs { get; init; }
    public required double MaxElapsedMs { get; init; }

    public required double MinPeakPrivateRamMb { get; init; }
    public required double MedianPeakPrivateRamMb { get; init; }
    public required double P95PeakPrivateRamMb { get; init; }
    public required double MaxPeakPrivateRamMb { get; init; }

    public required double MinOutputFileSizeMb { get; init; }
    public required double MedianOutputFileSizeMb { get; init; }
    public required double P95OutputFileSizeMb { get; init; }
    public required double MaxOutputFileSizeMb { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}