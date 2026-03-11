namespace OmniConvert.BenchmarkLab.Core;

public sealed class PipelineRegistry
{
    private readonly IReadOnlyList<IConversionPipeline> _pipelines;

    public PipelineRegistry(IEnumerable<IConversionPipeline> pipelines)
    {
        _pipelines = pipelines.ToList();
    }

    public IConversionPipeline Resolve(ConversionRequest request)
    {
        var pipeline = _pipelines.FirstOrDefault(p => p.CanHandle(request));

        if (pipeline is null)
        {
            throw new InvalidOperationException(
                $"Uygun pipeline bulunamadı. SourceType: {request.SourceType}, Scenario: {request.ScenarioName}");
        }

        return pipeline;
    }

    public IReadOnlyList<IConversionPipeline> GetAll()
    {
        return _pipelines;
    }
}