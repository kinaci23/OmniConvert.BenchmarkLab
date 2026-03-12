using System.Text;
using OmniConvert.BenchmarkLab.Benchmarking;
using OmniConvert.BenchmarkLab.Core;
using OmniConvert.BenchmarkLab.Inputs;
using OmniConvert.BenchmarkLab.Pipelines;
using OmniConvert.BenchmarkLab.Reporting;
using OmniConvert.BenchmarkLab.Validation;

const string projectBaseFolder = @"C:\Users\Arda\Desktop\projelerim\Diğer Projeler\OmniConvert.BenchmarkLab\OmniConvert.BenchmarkLab";
const string rasterInputsFolder = $@"{projectBaseFolder}\Inputs\raster";
const string pdfInputsFolder = $@"{projectBaseFolder}\Inputs\pdf";
const string outputFolder = @"C:\Users\Arda\Desktop\OmniConvertLab\BenchmarkOutputs";

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "OmniConvert.BenchmarkLab";

Console.WriteLine(new string('=', 70));
Console.WriteLine("OmniConvert BenchmarkLab");
Console.WriteLine(new string('=', 70));
Console.WriteLine($"Current Directory : {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Raster Inputs     : {rasterInputsFolder}");
Console.WriteLine($"PDF Inputs        : {pdfInputsFolder}");
Console.WriteLine($"Output Folder     : {outputFolder}");
Console.WriteLine(new string('=', 70));
Console.WriteLine();

Directory.CreateDirectory(outputFolder);

var datasetLoader = new InputDatasetLoader();

var samples = Directory.Exists(rasterInputsFolder)
    ? datasetLoader.LoadRasterSamples(rasterInputsFolder)
    : new List<InputSample>();

var pdfSamples = Directory.Exists(pdfInputsFolder)
    ? datasetLoader.LoadPdfSamples(pdfInputsFolder)
    : new List<InputSample>();

var scenarioFactory = new ScenarioFactory();

var scenarios = samples.Count > 0
    ? scenarioFactory.CreateRasterScenarios(
        samples,
        BuiltInProfiles.RasterMatrixProfiles,
        outputFolder,
        warmupRuns: 3,
        measuredRuns: 5)
    : new List<BenchmarkScenario>();

var pdfScenarios = pdfSamples.Count > 0
    ? scenarioFactory.CreatePdfScenarios(
        pdfSamples,
        BuiltInProfiles.PdfMatrixProfiles,
        outputFolder,
        warmupRuns: 0,
        measuredRuns: 1).ToList()
    : new List<BenchmarkScenario>();

pdfScenarios = pdfScenarios
    .Where(x => x.Request.InputPath.Contains("pdf_text_invoice", StringComparison.OrdinalIgnoreCase))
    .Where(x => x.Request.Profile.Name == "PdfOcrGray300")
    .Take(1)
    .ToList();

Console.WriteLine($"Raster sample sayısı : {samples.Count}");
Console.WriteLine($"Raster scenario sayısı : {scenarios.Count}");
Console.WriteLine($"PDF sample sayısı    : {pdfSamples.Count}");
Console.WriteLine($"PDF scenario sayısı  : {pdfScenarios.Count}");
Console.WriteLine();

var registry = new PipelineRegistry(new IConversionPipeline[]
{
    new MuPdfPipeline(),
});

var validator = new TiffOutputValidator();
var runner = new BenchmarkRunner(validator);
var reporter = new ConsoleReporter();

var csvReporter = new CsvBenchmarkReporter();
const string csvReportPath = @"C:\Users\Arda\Desktop\OmniConvertLab\BenchmarkOutputs\benchmark_results.csv";

if (false && scenarios.Count > 0)     // ŞUAN RASTER BENCHMARK KAPALI !!!
{
    Console.WriteLine("=== RASTER DATASET BENCHMARK ===");
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

    Console.WriteLine("Raster dataset benchmark tamamlandı.");
}
else
{
    Console.WriteLine("[UYARI] Raster scenario bulunamadı. Raster benchmark atlandı.");
}

Console.WriteLine();

if (pdfScenarios.Count > 0)
{
    var activePdfPipelineName = registry.Resolve(pdfScenarios.First().Request).Name;
    Console.WriteLine($"=== PDF DATASET BENCHMARK ({activePdfPipelineName}) ===");
    Console.WriteLine();

    foreach (var scenario in pdfScenarios)
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
            $"PDF BENCHMARK RAPORU - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
            results);

        reporter.PrintSummary(summary);
        csvReporter.AppendSummary(
        csvReportPath,
        summary,
        pipeline.Name,
        Path.GetFileName(request.InputPath),
        request.Profile.Name,
        results.LastOrDefault()?.OutputPath ?? request.OutputPath);
        Console.WriteLine($"[CSV] Summary appended: {csvReportPath}");
    }

    Console.WriteLine("PDF dataset benchmark tamamlandı.");
}
else
{
    Console.WriteLine("[UYARI] PDF scenario bulunamadı. PDF benchmark atlandı.");
}

Console.WriteLine();
Console.WriteLine("=== RASTER PARALLEL BENCHMARK ===");
Console.WriteLine("[INFO] Raster parallel benchmark bu aşamada devre dışı.");