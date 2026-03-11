namespace OmniConvert.BenchmarkLab.Core;

public sealed record ConversionExecutionResult
{
    public required string ScenarioName { get; init; }
    public required string OutputPath { get; init; }

    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public long ElapsedMilliseconds { get; init; }
    public long PeakPrivateBytes { get; init; }
    public long FinalPrivateBytes { get; init; }
    public long OutputFileBytes { get; init; }
    public OutputValidationResult? Validation { get; init; }

}