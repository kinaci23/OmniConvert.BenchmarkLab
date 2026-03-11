namespace OmniConvert.BenchmarkLab.Core;

public sealed record OutputValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;

    public bool FileExists { get; init; }
    public long FileSizeBytes { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }

    public double? DpiX { get; init; }
    public double? DpiY { get; init; }

    public string? Format { get; init; }
}