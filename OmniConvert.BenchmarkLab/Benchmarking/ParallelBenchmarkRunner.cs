using System.Diagnostics;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Benchmarking;

public sealed class ParallelBenchmarkRunner
{
    private readonly IOutputValidator? _validator;

    public ParallelBenchmarkRunner(IOutputValidator? validator = null)
    {
        _validator = validator;
    }

    public async Task<ParallelBenchmarkSummary> RunAsync(
        IConversionPipeline pipeline,
        BenchmarkScenario scenario,
        int workerCount,
        double baselineThroughputOpsPerSecond,
        CancellationToken cancellationToken = default)
    {
        if (workerCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(workerCount));

        var allResults = new List<ConversionExecutionResult>();
        var sync = new object();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await using var sampler = new ProcessMemorySampler();
        sampler.Start();

        var totalStopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, workerCount)
            .Select(workerId => RunWorkerAsync(
                pipeline,
                scenario,
                workerId,
                allResults,
                sync,
                cancellationToken))
            .ToArray();

        await Task.WhenAll(tasks);

        totalStopwatch.Stop();

        var successResults = allResults.Where(x => x.Success).ToList();
        var failedResults = allResults.Where(x => !x.Success).ToList();

        var elapsedValues = successResults.Select(x => x.ElapsedMilliseconds).ToList();

        double throughput = totalStopwatch.Elapsed.TotalSeconds > 0
            ? successResults.Count / totalStopwatch.Elapsed.TotalSeconds
            : 0;

        double speedup = workerCount == 1
            ? 1.0
            : (baselineThroughputOpsPerSecond > 0
                ? throughput / baselineThroughputOpsPerSecond
                : 0);

        double efficiencyPercent = workerCount == 1
            ? 100.0
            : (workerCount > 0 ? (speedup / workerCount) * 100.0 : 0);

        return new ParallelBenchmarkSummary
        {
            Title = $"{scenario.Name} | Parallel",
            WorkerCount = workerCount,
            TotalOperations = allResults.Count,
            SuccessfulOperations = successResults.Count,
            FailedOperations = failedResults.Count,
            TotalElapsedMs = totalStopwatch.Elapsed.TotalMilliseconds,
            ThroughputOpsPerSecond = throughput,
            MinElapsedMs = elapsedValues.Count > 0 ? elapsedValues.Min() : 0,
            MedianElapsedMs = BenchmarkStatistics.Median(elapsedValues),
            P95ElapsedMs = BenchmarkStatistics.Percentile(elapsedValues, 95),
            MaxElapsedMs = elapsedValues.Count > 0 ? elapsedValues.Max() : 0,
            MaxPeakPrivateRamMb = sampler.PeakPrivateBytes / (1024.0 * 1024.0),
            Speedup = speedup,
            EfficiencyPercent = efficiencyPercent,
            Errors = failedResults
                .Select(x => x.ErrorMessage ?? "Bilinmeyen hata")
                .Concat(successResults
                    .Where(x => x.Validation is not null && !x.Validation.IsValid)
                    .Select(x => $"VALIDATION: {x.Validation!.Message}"))
                .ToList()
        };
    }

    private async Task RunWorkerAsync(
        IConversionPipeline pipeline,
        BenchmarkScenario scenario,
        int workerId,
        List<ConversionExecutionResult> sink,
        object sync,
        CancellationToken cancellationToken)
    {
        for (int i = 1; i <= scenario.MeasuredRuns; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = CreateRequestForWorkerRun(scenario.Request, workerId, i);

            var sw = Stopwatch.StartNew();
            var result = await pipeline.ExecuteAsync(request, cancellationToken);
            sw.Stop();

            OutputValidationResult? validation = null;
            if (result.Success && _validator is not null)
            {
                validation = await _validator.ValidateAsync(request, cancellationToken);
            }

            result = result with
            {
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                Validation = validation
            };

            lock (sync)
            {
                sink.Add(result);
            }
        }
    }

    private static ConversionRequest CreateRequestForWorkerRun(
        ConversionRequest baseRequest,
        int workerId,
        int runNumber)
    {
        string outputPath = BuildWorkerOutputPath(baseRequest.OutputPath, workerId, runNumber);

        return baseRequest with
        {
            OutputPath = outputPath,
            ScenarioName = $"{baseRequest.ScenarioName} | W{workerId} | Run{runNumber}"
        };
    }

    private static string BuildWorkerOutputPath(string baseOutputPath, int workerId, int runNumber)
    {
        string directory = Path.GetDirectoryName(baseOutputPath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseOutputPath);
        string extension = Path.GetExtension(baseOutputPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}_worker_{workerId}_run_{runNumber}{extension}");
    }
}