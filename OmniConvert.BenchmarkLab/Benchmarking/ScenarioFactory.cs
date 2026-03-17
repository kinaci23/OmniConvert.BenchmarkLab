using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Benchmarking;

public sealed class ScenarioFactory
{
    public IReadOnlyList<BenchmarkScenario> CreateRasterScenarios(
        IReadOnlyList<InputSample> samples,
        IReadOnlyList<ConversionProfile> profiles,
        string outputFolder,
        int warmupRuns = 3,
        int measuredRuns = 5)
    {
        var scenarios = new List<BenchmarkScenario>();

        foreach (var sample in samples)
        {
            foreach (var profile in profiles)
            {
                string outputPath = Path.Combine(
                    outputFolder,
                    $"{Path.GetFileNameWithoutExtension(sample.Name)}_{profile.Name}.tiff");

                var request = new ConversionRequest
                {
                    ScenarioName = $"Raster | {sample.Name} | {profile.Name}",
                    SourceType = sample.SourceType,
                    InputPath = sample.FullPath,
                    OutputPath = outputPath,
                    Profile = profile,
                    PageIndex = null
                };

                var scenario = new BenchmarkScenario
                {
                    Name = $"Raster Benchmark - {sample.Name} - {profile.Name}",
                    Request = request,
                    WarmupRuns = warmupRuns,
                    MeasuredRuns = measuredRuns
                };

                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }

    public IReadOnlyList<BenchmarkScenario> CreatePdfScenarios(
    IReadOnlyList<InputSample> samples,
    IReadOnlyList<ConversionProfile> profiles,
    string outputFolder,
    int warmupRuns = 3,
    int measuredRuns = 5)
    {
        var scenarios = new List<BenchmarkScenario>();

        foreach (var sample in samples)
        {
            foreach (var profile in profiles)
            {
                string outputPath = Path.Combine(
                    outputFolder,
                    $"{Path.GetFileNameWithoutExtension(sample.Name)}_{profile.Name}.tiff");

                var request = new ConversionRequest
                {
                    ScenarioName = $"PDF | {sample.Name} | {profile.Name}",
                    SourceType = sample.SourceType,
                    InputPath = sample.FullPath,
                    OutputPath = outputPath,
                    Profile = profile,
                    PageIndex = null
                };

                var scenario = new BenchmarkScenario
                {
                    Name = $"PDF Benchmark - {sample.Name} - {profile.Name}",
                    Request = request,
                    WarmupRuns = warmupRuns,
                    MeasuredRuns = measuredRuns
                };

                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }

    public IReadOnlyList<BenchmarkScenario> CreateWordScenarios(
    IReadOnlyList<InputSample> samples,
    IReadOnlyList<ConversionProfile> profiles,
    string outputFolder,
    int warmupRuns = 3,
    int measuredRuns = 5)
    {
        var scenarios = new List<BenchmarkScenario>();

        foreach (var sample in samples)
        {
            foreach (var profile in profiles)
            {
                string outputPath = Path.Combine(
                    outputFolder,
                    $"{Path.GetFileNameWithoutExtension(sample.Name)}_{profile.Name}.tiff");

                var request = new ConversionRequest
                {
                    ScenarioName = $"WORD | {sample.Name} | {profile.Name}",
                    SourceType = sample.SourceType,
                    InputPath = sample.FullPath,
                    OutputPath = outputPath,
                    Profile = profile,
                    PageIndex = null
                };

                var scenario = new BenchmarkScenario
                {
                    Name = $"Word Benchmark - {sample.Name} - {profile.Name}",
                    Request = request,
                    WarmupRuns = warmupRuns,
                    MeasuredRuns = measuredRuns
                };

                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }

    public IReadOnlyList<BenchmarkScenario> CreateExcelScenarios(
    IReadOnlyList<InputSample> samples,
    IReadOnlyList<ConversionProfile> profiles,
    string outputFolder,
    int warmupRuns = 3,
    int measuredRuns = 5)
    {
        var scenarios = new List<BenchmarkScenario>();

        foreach (var sample in samples)
        {
            foreach (var profile in profiles)
            {
                string outputPath = Path.Combine(
                    outputFolder,
                    $"{Path.GetFileNameWithoutExtension(sample.Name)}_{profile.Name}.tiff");

                var request = new ConversionRequest
                {
                    ScenarioName = $"EXCEL | {sample.Name} | {profile.Name}",
                    SourceType = sample.SourceType,
                    InputPath = sample.FullPath,
                    OutputPath = outputPath,
                    Profile = profile,
                    PageIndex = null
                };

                var scenario = new BenchmarkScenario
                {
                    Name = $"Excel Benchmark - {sample.Name} - {profile.Name}",
                    Request = request,
                    WarmupRuns = warmupRuns,
                    MeasuredRuns = measuredRuns
                };

                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }
}