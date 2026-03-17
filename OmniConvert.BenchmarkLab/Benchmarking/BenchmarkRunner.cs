using System.Diagnostics;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Benchmarking;

public class BenchmarkRunner
{
    private readonly IOutputValidator? _validator;

    public BenchmarkRunner(IOutputValidator? validator = null)
    {
        _validator = validator;
    }

    public async Task<List<ConversionExecutionResult>> RunAsync(
        IConversionPipeline pipeline,
        BenchmarkScenario scenario)
    {
        var results = new List<ConversionExecutionResult>();

        for (int i = 0; i < scenario.WarmupRuns; i++)
        {
            var warmupRequest = CreateRequestForRun(scenario.Request, i + 1, isWarmup: true);
            await pipeline.ExecuteAsync(warmupRequest);
        }

        for (int i = 0; i < scenario.MeasuredRuns; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var measuredRequest = CreateRequestForRun(scenario.Request, i + 1, isWarmup: false);

            await using var sampler = new ProcessMemorySampler();
            sampler.Start();

            var sw = Stopwatch.StartNew();

            var result = await pipeline.ExecuteAsync(measuredRequest);

            sw.Stop();

            OutputValidationResult? validation = null;
            if (result.Success && _validator is not null)
            {
                var validationRequest = measuredRequest with
                {
                    OutputPath = result.OutputPath
                };

                validation = await _validator.ValidateAsync(validationRequest);
            }

            result = result with
            {
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                PeakPrivateBytes = sampler.PeakPrivateBytes,
                FinalPrivateBytes = sampler.LastPrivateBytes,
                Validation = validation
            };

            results.Add(result);
        }

        return results;
    }

    private static ConversionRequest CreateRequestForRun(
        ConversionRequest baseRequest,
        int runNumber,
        bool isWarmup)
    {
        string outputPath = BuildRunOutputPath(baseRequest.OutputPath, runNumber, isWarmup);

        return baseRequest with
        {
            OutputPath = outputPath
        };
    }

    private static string BuildRunOutputPath(string baseOutputPath, int runNumber, bool isWarmup)
    {
        string directory = Path.GetDirectoryName(baseOutputPath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseOutputPath);
        string extension = Path.GetExtension(baseOutputPath);

        string suffix = isWarmup
            ? $"_warmup_{runNumber}"
            : $"_run_{runNumber}";

        return Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}{extension}");
    }
}