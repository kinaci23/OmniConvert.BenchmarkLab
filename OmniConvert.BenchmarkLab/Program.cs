using System.Text;
using OmniConvert.BenchmarkLab.Benchmarking;
using OmniConvert.BenchmarkLab.Core;
using OmniConvert.BenchmarkLab.Inputs;
using OmniConvert.BenchmarkLab.Pipelines;
using OmniConvert.BenchmarkLab.Reporting;
using OmniConvert.BenchmarkLab.Validation;


const string projectBaseFolder = @"C:\Users\Arda\Desktop\projelerim\Diğer Projeler\OmniConvert.BenchmarkLab\OmniConvert.BenchmarkLab";
const string rasterInputsFolder = $@"{projectBaseFolder}\Inputs\raster";
const string outputFolder = @"C:\Users\Arda\Desktop\OmniConvertLab\BenchmarkOutputs";

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "OmniConvert.BenchmarkLab";

Console.WriteLine(new string('=', 70));
Console.WriteLine("OmniConvert BenchmarkLab");
Console.WriteLine(new string('=', 70));
Console.WriteLine($"Current Directory : {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Raster Inputs     : {rasterInputsFolder}");
Console.WriteLine($"Output Folder     : {outputFolder}");
Console.WriteLine(new string('=', 70));
Console.WriteLine();

Directory.CreateDirectory(outputFolder);

var datasetLoader = new InputDatasetLoader();
var samples = datasetLoader.LoadRasterSamples(rasterInputsFolder);

if (samples.Count == 0)
{
    Console.WriteLine($"[HATA] Input dataset boş: {rasterInputsFolder}");
    return;
}

var scenarioFactory = new ScenarioFactory();
var scenarios = scenarioFactory.CreateRasterScenarios(
    samples,
    BuiltInProfiles.RasterMatrixProfiles,
    outputFolder,
    warmupRuns: 3,
    measuredRuns: 5);

if (scenarios.Count == 0)
{
    Console.WriteLine("[HATA] Benchmark scenario üretilemedi.");
    return;
}

var registry = new PipelineRegistry(new IConversionPipeline[]
{
    new RasterMagickPipeline()
});

var validator = new TiffOutputValidator();
var runner = new BenchmarkRunner(validator);
var reporter = new ConsoleReporter();

Console.WriteLine("Dataset benchmark başlıyor...");
Console.WriteLine();

foreach (var scenario in scenarios)
{
    var request = scenario.Request;
    var pipeline = registry.Resolve(request);

    Console.WriteLine();
    Console.WriteLine(new string('-', 70));
    Console.WriteLine($"Scenario          : {scenario.Name}");
    Console.WriteLine($"Input File        : {Path.GetFileName(request.InputPath)}");
    Console.WriteLine($"Profile           : {request.Profile.Name}");
    Console.WriteLine($"Intent            : {request.Profile.Intent}");
    Console.WriteLine($"DPI               : {request.Profile.Dpi}");
    Console.WriteLine($"Color Mode        : {request.Profile.ColorMode}");
    Console.WriteLine($"Compression       : {request.Profile.Compression}");
    Console.WriteLine($"Output Base Path  : {request.OutputPath}");
    Console.WriteLine($"Pipeline          : {pipeline.Name}");
    Console.WriteLine(new string('-', 70));

    var results = await runner.RunAsync(pipeline, scenario);

    var summary = BenchmarkStatistics.BuildSummary(
        $"RASTER BENCHMARK RAPORU - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
        results);

    reporter.PrintSummary(summary);
}

Console.WriteLine("Dataset benchmark tamamlandı.");

Console.WriteLine();
Console.WriteLine("Parallel benchmark başlıyor...");
Console.WriteLine();

var parallelReporter = new ParallelConsoleReporter();
var parallelRunner = new ParallelBenchmarkRunner(validator);

var firstSample = samples.First();

string parallelOutputPath = Path.Combine(
    outputFolder,
    $"{Path.GetFileNameWithoutExtension(firstSample.Name)}_parallel_output.tiff");

var parallelRequest = new ConversionRequest
{
    ScenarioName = $"Parallel Raster | {firstSample.Name} | Visual | LZW | 300 DPI",
    SourceType = firstSample.SourceType,
    InputPath = firstSample.FullPath,
    OutputPath = parallelOutputPath,
    Profile = BuiltInProfiles.RasterVisualLzw300,
    PageIndex = null
};

var parallelScenario = new BenchmarkScenario
{
    Name = $"Parallel Raster Benchmark - {firstSample.Name}",
    Request = parallelRequest,
    WarmupRuns = 0,
    MeasuredRuns = 5
};

var parallelPipeline = registry.Resolve(parallelRequest);

// 1) Önce baseline al
Console.WriteLine($"Parallel Test | Örnek dosya: {firstSample.Name} | Workers: 1");
Console.WriteLine();

var baselineSummary = await parallelRunner.RunAsync(
    parallelPipeline,
    parallelScenario,
    1,
    0);

baselineSummary = baselineSummary with
{
    Speedup = 1.0,
    EfficiencyPercent = 100.0
};

parallelReporter.PrintSummary(baselineSummary);

double baselineThroughput = baselineSummary.ThroughputOpsPerSecond;

// 2) Sonra diğer worker sayıları
foreach (int workerCount in new[] { 2, 4 })
{
    Console.WriteLine($"Parallel Test | Örnek dosya: {firstSample.Name} | Workers: {workerCount}");
    Console.WriteLine();

    var parallelSummary = await parallelRunner.RunAsync(
        parallelPipeline,
        parallelScenario,
        workerCount,
        baselineThroughput);

    parallelReporter.PrintSummary(parallelSummary);
}

Console.WriteLine("Parallel benchmark tamamlandı.");
