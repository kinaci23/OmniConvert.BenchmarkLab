namespace OmniConvert.BenchmarkLab.Core;

public enum ConversionSourceType
{
    Raster,
    Pdf,
    Word
}

public sealed record ConversionRequest
{
    public required string ScenarioName { get; init; }
    public required ConversionSourceType SourceType { get; init; }
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required ConversionProfile Profile { get; init; }

    public int? PageIndex { get; init; }
}