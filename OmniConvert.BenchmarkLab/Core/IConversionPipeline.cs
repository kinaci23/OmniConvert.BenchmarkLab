namespace OmniConvert.BenchmarkLab.Core;

public interface IConversionPipeline
{
    string Name { get; }

    bool CanHandle(ConversionRequest request);

    Task<ConversionExecutionResult> ExecuteAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default);
}