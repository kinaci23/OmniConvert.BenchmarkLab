namespace OmniConvert.BenchmarkLab.Core;

public sealed record InputSample
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required ConversionSourceType SourceType { get; init; }

    public string Category { get; init; } = "unknown";
    public string Notes { get; init; } = string.Empty;
}