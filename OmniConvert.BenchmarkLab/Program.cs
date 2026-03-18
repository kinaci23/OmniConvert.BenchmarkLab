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
const string wordInputsFolder = $@"{projectBaseFolder}\Inputs\word";
const string excelInputsFolder = $@"{projectBaseFolder}\Inputs\excel";


Console.OutputEncoding = Encoding.UTF8;
Console.Title = "OmniConvert.BenchmarkLab";

Console.WriteLine(new string('=', 70));
Console.WriteLine("OmniConvert BenchmarkLab");
Console.WriteLine(new string('=', 70));
Console.WriteLine($"Current Directory : {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Raster Inputs     : {rasterInputsFolder}");
Console.WriteLine($"PDF Inputs        : {pdfInputsFolder}");
Console.WriteLine($"Word Inputs : {wordInputsFolder}");
Console.WriteLine($"Excel Inputs : {excelInputsFolder}");
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

var wordSamples = Directory.Exists(wordInputsFolder)
    ? datasetLoader.LoadWordSamples(wordInputsFolder)
    : new List<InputSample>();

var excelSamples = Directory.Exists(excelInputsFolder)
    ? datasetLoader.LoadExcelSamples(excelInputsFolder)
    : new List<InputSample>();

var scenarioFactory = new ScenarioFactory();

var scenarios = samples.Count > 0
    ? scenarioFactory.CreateRasterScenarios(
        samples,
        BuiltInProfiles.RasterMatrixProfiles,
        outputFolder,
        warmupRuns: 0,
        measuredRuns: 1)
    : new List<BenchmarkScenario>();

var pdfScenarios = pdfSamples.Count > 0
    ? scenarioFactory.CreatePdfScenarios(
        pdfSamples,
        BuiltInProfiles.PdfMatrixProfiles,
        outputFolder,
        warmupRuns: 0,
        measuredRuns: 1).ToList()
    : new List<BenchmarkScenario>();

var wordScenarios = wordSamples.Count > 0
    ? scenarioFactory.CreateWordScenarios(
        wordSamples,
        BuiltInProfiles.OfficeAll,
        outputFolder,
        warmupRuns: 0,
        measuredRuns: 1).ToList()
    : new List<BenchmarkScenario>();

var excelScenarios = excelSamples.Count > 0
    ? scenarioFactory.CreateExcelScenarios(
        excelSamples,
        BuiltInProfiles.OfficeAll,
        outputFolder,
        warmupRuns: 0,
        measuredRuns: 1).ToList()
    : new List<BenchmarkScenario>();

pdfScenarios = pdfScenarios
    .Where(x =>
        (x.Request.InputPath.Contains("pdf_text_invoice", StringComparison.OrdinalIgnoreCase) &&
         (x.Request.Profile.Name == "PdfOcrGray300" ||
          x.Request.Profile.Name == "PdfOcrBinary300" ||
          x.Request.Profile.Name == "PdfVisualLzw300")) ||

        (x.Request.InputPath.Contains("pdf_scan_invoice", StringComparison.OrdinalIgnoreCase) &&
         (x.Request.Profile.Name == "PdfOcrGray300" ||
          x.Request.Profile.Name == "PdfOcrBinary300")) ||

        (x.Request.InputPath.Contains("pdf_photo_heavy", StringComparison.OrdinalIgnoreCase) &&
         x.Request.Profile.Name == "PdfVisualLzw300")
    )
    .ToList();

Console.WriteLine($"Raster sample sayısı : {samples.Count}");
Console.WriteLine($"Raster scenario sayısı : {scenarios.Count}");
Console.WriteLine($"PDF sample sayısı    : {pdfSamples.Count}");
Console.WriteLine($"PDF scenario sayısı  : {pdfScenarios.Count}");
Console.WriteLine($"Word sample sayısı : {wordSamples.Count}");
Console.WriteLine($"Word scenario sayısı : {wordScenarios.Count}");
Console.WriteLine($"Excel sample sayısı : {excelSamples.Count}");
Console.WriteLine($"Excel scenario sayısı : {excelScenarios.Count}");
Console.WriteLine();

var rasterPipelines = new List<IConversionPipeline>
{
    new RasterMagickPipeline()
};

var finalPdfPipelines = new IConversionPipeline[]
{
    new GhostscriptScaledPipeline(),
    new PdfiumPipeline(),
    new MuPdfPipeline(),
    new AsposePdfPipeline()
};

var wordPipelines = new List<IConversionPipeline>
{
    new LibreOfficeWordPdfBridgePipeline(),
    new AsposeWordsDirectTiffPipeline(),

    new SpireWordRenderMergePipeline(),
    new SyncfusionWordDirectTiffPipeline(), 
    new GemBoxWordDirectTiffPipeline()
};

var excelPipelines = new List<IConversionPipeline>
{
    new LibreOfficeExcelPdfBridgePipeline(),
    new AsposeCellsDirectTiffPipeline(),
    new SyncfusionExcelRenderMergePipeline(),
    new SpireExcelRenderMergePipeline()
    
};

Console.WriteLine($"Raster pipeline sayısı : {rasterPipelines.Count}");
Console.WriteLine($"PDF pipeline sayısı    : {finalPdfPipelines.Length}");
Console.WriteLine($"Word pipeline sayısı   : {wordPipelines.Count}");
Console.WriteLine($"Excel pipeline sayısı  : {excelPipelines.Count}");

var validator = new TiffOutputValidator();
var runner = new BenchmarkRunner(validator);
var reporter = new ConsoleReporter();

var csvReporter = new CsvBenchmarkReporter();
const string csvReportPath = @"C:\Users\Arda\Desktop\OmniConvertLab\BenchmarkOutputs\benchmark_results.csv";



if (scenarios.Count > 0)
{
    if (rasterPipelines.Count == 0)
    {
        Console.WriteLine("[UYARI] Raster pipeline bulunamadı. Raster benchmark atlandı.");
    }
    else
    {
        foreach (var pipeline in rasterPipelines)
        {
            Console.WriteLine($"=== RASTER DATASET BENCHMARK ({pipeline.Name}) ===");
            Console.WriteLine();

            foreach (var scenario in scenarios)
            {
                var request = scenario.Request;

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
                    $"RASTER BENCHMARK RAPORU - {pipeline.Name} - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
                    results);

                reporter.PrintSummary(summary);

                string benchmarkStatus = "Full";
                string inputCategory = InferInputCategoryFromFileName(request.InputPath);
                string pipelineType = InferPipelineType(pipeline.Name);

                csvReporter.AppendSummary(
                    csvReportPath,
                    summary,
                    pipeline.Name,
                    Path.GetFileName(request.InputPath),
                    inputCategory,
                    request.Profile.Name,
                    request.Profile.Intent.ToString(),
                    pipelineType,
                    results.LastOrDefault()?.OutputPath ?? request.OutputPath,
                    request.Profile.Dpi,
                    request.Profile.ColorMode.ToString(),
                    request.Profile.Compression.ToString(),
                    benchmarkStatus);

                Console.WriteLine($"[CSV] Summary appended: {csvReportPath}");
            }

            Console.WriteLine($"Raster dataset benchmark tamamlandı: {pipeline.Name}");
            Console.WriteLine();
        }
    }
}
else
{
    Console.WriteLine("[UYARI] Raster scenario bulunamadı. Raster benchmark atlandı.");
}

Console.WriteLine();

Console.WriteLine();


if (pdfScenarios.Count > 0)
{
    foreach (var pipeline in finalPdfPipelines)
    {
        Console.WriteLine($"=== PDF DATASET BENCHMARK ({pipeline.Name}) ===");
        Console.WriteLine();

        foreach (var scenario in pdfScenarios)
        {
            var request = scenario.Request;

            if (pipeline.Name == "MuPdfPipeline" &&
                 request.Profile.Name == "PdfOcrBinary300")
            {
                Console.WriteLine($"[SKIP] {pipeline.Name} does not support stable binary OCR for {Path.GetFileName(request.InputPath)}");
                continue;
            }

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
                $"PDF BENCHMARK RAPORU - {pipeline.Name} - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
                results);

            reporter.PrintSummary(summary);

            string benchmarkStatus = pipeline.Name == "AsposePdfPipeline" ? "Limited" : "Full";
            string inputCategory = InferInputCategoryFromFileName(request.InputPath);
            string pipelineType = InferPipelineType(pipeline.Name);

            csvReporter.AppendSummary(
                csvReportPath,
                summary,
                pipeline.Name,
                Path.GetFileName(request.InputPath),
                inputCategory,
                request.Profile.Name,
                request.Profile.Intent.ToString(),
                pipelineType,
                results.LastOrDefault()?.OutputPath ?? request.OutputPath,
                request.Profile.Dpi,
                request.Profile.ColorMode.ToString(),
                request.Profile.Compression.ToString(),
                benchmarkStatus);

            Console.WriteLine($"[CSV] Summary appended: {csvReportPath}");
        }

        Console.WriteLine($"PDF dataset benchmark tamamlandı: {pipeline.Name}");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine("[UYARI] PDF scenario bulunamadı. PDF benchmark atlandı.");
}



if (wordScenarios.Count > 0 && wordPipelines.Count > 0)
{
    foreach (var pipeline in wordPipelines)
    {
        Console.WriteLine($"=== WORD DATASET BENCHMARK ({pipeline.Name}) ===");
        Console.WriteLine();

        foreach (var scenario in wordScenarios)
        {
            var request = scenario.Request;

            Console.WriteLine();
            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"Scenario : {scenario.Name}");
            Console.WriteLine($"Input File : {Path.GetFileName(request.InputPath)}");
            Console.WriteLine($"Profile : {request.Profile.Name}");
            Console.WriteLine($"Intent : {request.Profile.Intent}");
            Console.WriteLine($"DPI : {request.Profile.Dpi}");
            Console.WriteLine($"Color Mode : {request.Profile.ColorMode}");
            Console.WriteLine($"Compression : {request.Profile.Compression}");
            Console.WriteLine($"Output Base Path : {request.OutputPath}");
            Console.WriteLine($"Pipeline : {pipeline.Name}");
            Console.WriteLine(new string('-', 70));

            var results = await runner.RunAsync(pipeline, scenario);

            var summary = BenchmarkStatistics.BuildSummary(
                $"WORD BENCHMARK RAPORU - {pipeline.Name} - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
                results);

            reporter.PrintSummary(summary);

            string benchmarkStatus = pipeline.Name switch
            {
                "AsposeWordsDirectTiffPipeline" => "EvaluationOnly",
                "LibreOfficeWordPdfBridgePipeline" => "BridgePipeline",
                "SpireWordRenderMergePipeline" => "Experimental",
                "GemBoxWordDirectTiffPipeline" => "EvaluationOnly",
                "SyncfusionWordDirectTiffPipeline" => "Experimental",
                _ => "Unknown"
            };

            string inputCategory = InferOfficeInputCategoryFromFileName(request.InputPath, request.SourceType);
            string pipelineType = InferPipelineType(pipeline.Name);

            csvReporter.AppendSummary(
                csvReportPath,
                summary,
                pipeline.Name,
                Path.GetFileName(request.InputPath),
                inputCategory,
                request.Profile.Name,
                request.Profile.Intent.ToString(),
                pipelineType,
                results.LastOrDefault()?.OutputPath ?? request.OutputPath,
                request.Profile.Dpi,
                request.Profile.ColorMode.ToString(),
                request.Profile.Compression.ToString(),
                benchmarkStatus);

            Console.WriteLine($"[CSV] Summary appended: {csvReportPath}");
        }

        Console.WriteLine($"Word dataset benchmark tamamlandı: {pipeline.Name}");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine("[UYARI] Word scenario veya pipeline bulunamadı.");
    Console.WriteLine("Word benchmark atlandı.");
}




if (excelScenarios.Count > 0 && excelPipelines.Count > 0)
{
    foreach (var pipeline in excelPipelines)
    {
        Console.WriteLine($"=== EXCEL DATASET BENCHMARK ({pipeline.Name}) ===");
        Console.WriteLine();

        foreach (var scenario in excelScenarios)
        {
            var request = scenario.Request;

            Console.WriteLine();
            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"Scenario : {scenario.Name}");
            Console.WriteLine($"Input File : {Path.GetFileName(request.InputPath)}");
            Console.WriteLine($"Profile : {request.Profile.Name}");
            Console.WriteLine($"Intent : {request.Profile.Intent}");
            Console.WriteLine($"DPI : {request.Profile.Dpi}");
            Console.WriteLine($"Color Mode : {request.Profile.ColorMode}");
            Console.WriteLine($"Compression : {request.Profile.Compression}");
            Console.WriteLine($"Output Base Path : {request.OutputPath}");
            Console.WriteLine($"Pipeline : {pipeline.Name}");
            Console.WriteLine(new string('-', 70));

            var results = await runner.RunAsync(pipeline, scenario);

            var summary = BenchmarkStatistics.BuildSummary(
                $"EXCEL BENCHMARK RAPORU - {pipeline.Name} - {Path.GetFileName(request.InputPath)} - {request.Profile.Name}",
                results);

            reporter.PrintSummary(summary);

            string benchmarkStatus = pipeline.Name switch
            {
                "AsposeCellsDirectTiffPipeline" => "EvaluationOnly",
                "LibreOfficeExcelPdfBridgePipeline" => "BridgePipeline",
                "SyncfusionExcelRenderMergePipeline" => "Full",
                "SpireExcelRenderMergePipeline" => "Full",
                _ => "Unknown"
            };

            string inputCategory = InferOfficeInputCategoryFromFileName(request.InputPath, request.SourceType);
            string pipelineType = InferPipelineType(pipeline.Name);

            csvReporter.AppendSummary(
                csvReportPath,
                summary,
                pipeline.Name,
                Path.GetFileName(request.InputPath),
                inputCategory,
                request.Profile.Name,
                request.Profile.Intent.ToString(),
                pipelineType,
                results.LastOrDefault()?.OutputPath ?? request.OutputPath,
                request.Profile.Dpi,
                request.Profile.ColorMode.ToString(),
                request.Profile.Compression.ToString(),
                benchmarkStatus);

            Console.WriteLine($"[CSV] Summary appended: {csvReportPath}");
        }

        Console.WriteLine($"Excel dataset benchmark tamamlandı: {pipeline.Name}");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine("[UYARI] Excel scenario veya pipeline bulunamadı.");
    Console.WriteLine("Excel benchmark atlandı.");
}



Console.WriteLine();
Console.WriteLine("=== BENCHMARK RUN COMPLETED ===");

static string InferInputCategoryFromFileName(string inputPath)
{
    string fileName = Path.GetFileNameWithoutExtension(inputPath).ToLowerInvariant();

    if (fileName.Contains("text"))
        return "TextDocument";

    if (fileName.Contains("scan"))
        return "ScannedDocument";

    if (fileName.Contains("photo"))
        return "PhotoHeavyDocument";

    return "Unknown";
}

static string InferOfficeInputCategoryFromFileName(string inputPath, ConversionSourceType sourceType)
{
    string fileName = Path.GetFileNameWithoutExtension(inputPath).ToLowerInvariant();

    if (sourceType == ConversionSourceType.Word)
    {
        if (fileName.Contains("table")) return "TableHeavyWord";
        if (fileName.Contains("image") || fileName.Contains("shape")) return "ImageShapeWord";
        if (fileName.Contains("unicode") || fileName.Contains("tr")) return "UnicodeWord";
        if (fileName.Contains("template") || fileName.Contains("corporate")) return "CorporateTemplateWord";
        return "TextWord";
    }

    if (sourceType == ConversionSourceType.Excel)
    {
        if (fileName.Contains("merged")) return "MergedCellsExcel";
        if (fileName.Contains("chart")) return "ChartExcel";
        if (fileName.Contains("printarea")) return "PrintAreaExcel";
        if (fileName.Contains("landscape")) return "LandscapeExcel";
        if (fileName.Contains("multisheet")) return "MultiSheetExcel";
        return "GeneralExcel";
    }

    return "Unknown";
}

static string InferPipelineType(string pipelineName)
{
    return pipelineName switch
    {
        "RasterMagickPipeline" => "DirectNativeTiff",
        "GhostscriptPipeline" => "DirectNativeTiff",
        "GhostscriptScaledPipeline" => "DirectNativeTiff",
        "PdfiumPipeline" => "RenderThenMerge",
        "PdfiumPngPipeline" => "RenderThenMerge",
        "MuPdfPipeline" => "RenderThenMerge",
        "MuPdfPnmPipeline" => "RenderThenMerge",
        "AsposePdfPipeline" => "DirectNativeTiff",
        "LibreOfficeWordPdfBridgePipeline" => "BridgeViaPdf",
        "LibreOfficeExcelPdfBridgePipeline" => "BridgeViaPdf",
        "AsposeWordsDirectTiffPipeline" => "DirectNativeTiff",
        "AsposeCellsDirectTiffPipeline" => "DirectNativeTiff",
        "SyncfusionWordDirectTiffPipeline" => "RenderThenMerge",
        "SyncfusionExcelRenderMergePipeline" => "RenderThenMerge",
        "GemBoxWordDirectTiffPipeline" => "DirectNativeTiff",
        "SpireWordRenderMergePipeline" => "RenderThenMerge",
        "SpireExcelRenderMergePipeline" => "RenderThenMerge",
        _ => "Unknown"
    };
}