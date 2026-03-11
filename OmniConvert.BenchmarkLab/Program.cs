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

Console.WriteLine($"Current Directory : {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Raster Inputs     : {rasterInputsFolder}");
Console.WriteLine($"Output Folder     : {outputFolder}");
Console.WriteLine();

Directory.CreateDirectory(outputFolder);

var profile = ConversionProfile.VisualDefault with
{
    Name = "RASTER_VISUAL_PROFILE",
    Dpi = 300,
    ColorMode = TargetColorMode.Rgb24Bit,
    Compression = TiffCompressionKind.Lzw
};

var datasetLoader = new InputDatasetLoader();
var samples = datasetLoader.LoadRasterSamples(rasterInputsFolder);

if (samples.Count == 0)
{
    Console.WriteLine($"[HATA] Input dataset boş: {rasterInputsFolder}");
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

foreach (var sample in samples)
{
    string outputPath = Path.Combine(
        outputFolder,
        $"{Path.GetFileNameWithoutExtension(sample.Name)}_benchmark_output.tiff");

    var request = new ConversionRequest
    {
        ScenarioName = $"Raster | {sample.Name} | Visual | LZW | 300 DPI",
        SourceType = sample.SourceType,
        InputPath = sample.FullPath,
        OutputPath = outputPath,
        Profile = profile,
        PageIndex = null
    };

    var scenario = new BenchmarkScenario
    {
        Name = $"Raster Benchmark - {sample.Name}",
        Request = request,
        WarmupRuns = 3,
        MeasuredRuns = 5
    };

    var pipeline = registry.Resolve(request);

    Console.WriteLine($"Örnek dosya      : {sample.Name}");
    Console.WriteLine($"Kategori         : {sample.Category}");
    Console.WriteLine($"Not              : {sample.Notes}");
    Console.WriteLine($"Input Path       : {sample.FullPath}");
    Console.WriteLine($"Output Base Path : {outputPath}");
    Console.WriteLine($"Seçilen Pipeline : {pipeline.Name}");
    Console.WriteLine();

    var results = await runner.RunAsync(pipeline, scenario);
    var summary = BenchmarkStatistics.BuildSummary(
        $"RASTER BENCHMARK RAPORU - {sample.Name}",
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
    Profile = profile,
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
