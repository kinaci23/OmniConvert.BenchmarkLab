namespace OmniConvert.BenchmarkLab.Core;

public interface IOutputValidator
{
    Task<OutputValidationResult> ValidateAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default);
}