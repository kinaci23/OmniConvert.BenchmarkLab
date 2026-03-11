namespace OmniConvert.BenchmarkLab.Core;

public sealed record BenchmarkScenario
{
    public required string Name { get; init; }
    public required ConversionRequest Request { get; init; }

    public int WarmupRuns { get; init; } = 3;
    public int MeasuredRuns { get; init; } = 5;
}