using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Aspose.Words;
using Aspose.Words.Saving;
using ImageMagick;
using PdfiumViewer;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;

const string baseFolder = @"C:\Users\Arda\Desktop\OmniConvertLab";

const string rasterInputFilePath = $@"{baseFolder}\test.jpg";
const string rasterOutputLzwPath = $@"{baseFolder}\output_lzw.tiff";
const string rasterOutputJpegPath = $@"{baseFolder}\output_jpeg.tiff";

const string pdfInputFilePath = $@"{baseFolder}\test.pdf";
const string pdfOutputRgbPath = $@"{baseFolder}\output_pdf_rgb.tiff";
const string pdfOutputGrayPath = $@"{baseFolder}\output_pdf_gray.tiff";
const string pdfOutputMonoPath = $@"{baseFolder}\output_pdf_mono.tiff";
const string pdfOutputDirectOcrPath = $@"{baseFolder}\output_pdf_ocr_direct.tiff";

const string officeInputFilePath = $@"{baseFolder}\test.docx";
const string officeOutputSyncfusionPath = $@"{baseFolder}\output_docx_syncfusion.tiff";
const string officeOutputAsposePath = $@"{baseFolder}\output_docx_aspose.tiff";

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "OmniConvert.BenchmarkLab";

Console.WriteLine("==============================================================");
Console.WriteLine("                OmniConvert.BenchmarkLab");
Console.WriteLine("==============================================================");
Console.WriteLine($"Çalışma Klasörü : {baseFolder}");
Console.WriteLine();

if (!File.Exists(rasterInputFilePath))
{
    Console.WriteLine($"[HATA] JPG girdi dosyası bulunamadı: {rasterInputFilePath}");
    return;
}

if (!File.Exists(pdfInputFilePath))
{
    Console.WriteLine($"[HATA] PDF girdi dosyası bulunamadı: {pdfInputFilePath}");
    return;
}

if (!File.Exists(officeInputFilePath))
{
    Console.WriteLine($"[HATA] DOCX girdi dosyası bulunamadı: {officeInputFilePath}");
    return;
}

RasterTest(rasterInputFilePath, rasterOutputLzwPath, rasterOutputJpegPath);

PdfTest(
    pdfInputFilePath,
    pdfOutputRgbPath,
    pdfOutputGrayPath,
    pdfOutputMonoPath,
    pdfOutputDirectOcrPath);

OfficeTest(
    officeInputFilePath,
    officeOutputSyncfusionPath,
    officeOutputAsposePath);

Console.WriteLine("Tüm testler tamamlandı.");
Console.WriteLine("==============================================================");

static void RasterTest(string inputPath, string lzwOutputPath, string jpegOutputPath)
{
    Console.WriteLine("--------------------------------------------------------------");
    Console.WriteLine("RASTER TESTLERİ");
    Console.WriteLine("--------------------------------------------------------------");

    BenchmarkRunner("Magick.NET | JPG -> TIFF | LZW | 300 DPI", () =>
    {
        using var image = new MagickImage(inputPath);

        image.Density = new Density(300, 300);
        image.Format = MagickFormat.Tiff;
        image.Settings.Compression = CompressionMethod.LZW;

        image.Write(lzwOutputPath);
    });

    BenchmarkRunner("Magick.NET | JPG -> TIFF | JPEG | Q=80 | 300 DPI", () =>
    {
        using var image = new MagickImage(inputPath);

        image.Density = new Density(300, 300);
        image.Format = MagickFormat.Tiff;
        image.Settings.Compression = CompressionMethod.JPEG;
        image.Quality = 80;

        image.Write(jpegOutputPath);
    });

    Console.WriteLine();
}

static void PdfTest(
    string pdfPath,
    string outputRgbPath,
    string outputGrayPath,
    string outputMonoPath,
    string outputDirectOcrPath)
{
    Console.WriteLine("--------------------------------------------------------------");
    Console.WriteLine("PDF TESTLERİ");
    Console.WriteLine("--------------------------------------------------------------");

    BenchmarkRunner("Pdfium + Magick.NET | PDF Sayfa 0 -> TIFF | RGB | LZW | 300 DPI", () =>
    {
        using var document = PdfDocument.Load(pdfPath);
        using var renderedBitmap = RenderPdfPage(document, pageIndex: 0, dpi: 300, grayscale: false);
        using var memoryStream = new MemoryStream();

        renderedBitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;

        using var magickImage = new MagickImage(memoryStream);

        magickImage.Density = new Density(300, 300);
        magickImage.Format = MagickFormat.Tiff;
        magickImage.Settings.Compression = CompressionMethod.LZW;

        magickImage.Write(outputRgbPath);
    });

    BenchmarkRunner("Pdfium + Magick.NET | PDF Sayfa 0 -> TIFF | Grayscale 8-bit | LZW | 300 DPI", () =>
    {
        using var document = PdfDocument.Load(pdfPath);
        using var renderedBitmap = RenderPdfPage(document, pageIndex: 0, dpi: 300, grayscale: true);
        using var memoryStream = new MemoryStream();

        renderedBitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;

        using var magickImage = new MagickImage(memoryStream);

        magickImage.Density = new Density(300, 300);
        magickImage.Grayscale();
        magickImage.Format = MagickFormat.Tiff;
        magickImage.Settings.Compression = CompressionMethod.LZW;

        magickImage.Write(outputGrayPath);
    });

    BenchmarkRunner("Pdfium | PDF Sayfa 0 -> TIFF | Monochrome 1-bit | CCITT4 | 300 DPI", () =>
    {
        using var document = PdfDocument.Load(pdfPath);
        using var renderedBitmap = RenderPdfPage(document, pageIndex: 0, dpi: 300, grayscale: true);
        using var monoBitmap = ConvertTo1BppFast(renderedBitmap, threshold: 180);

        SaveAsCcitt4Tiff(monoBitmap, outputMonoPath, 300);
    });

    BenchmarkRunner("Pdfium Direct OCR Pipeline | PDF Sayfa 0 -> TIFF | Direct 1-bit | CCITT4 | 300 DPI", () =>
    {
        RenderPdfPageDirectToCcitt4Tiff(pdfPath, pageIndex: 0, dpi: 300, threshold: 180, outputPath: outputDirectOcrPath);
    });

    Console.WriteLine();
}

static void OfficeTest(
    string docxPath,
    string outputSyncfusionPath,
    string outputAsposePath)
{
    Console.WriteLine("--------------------------------------------------------------");
    Console.WriteLine("OFFICE (WORD) TESTLERİ");
    Console.WriteLine("--------------------------------------------------------------");

    BenchmarkRunner("Syncfusion DocIO | DOCX Sayfa 0 -> TIFF | LZW | 300 DPI", () =>
    {
        using var docStream = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var wordDocument = new Syncfusion.DocIO.DLS.WordDocument(docStream, Syncfusion.DocIO.FormatType.Docx);
        using var renderer = new DocIORenderer();
        using Stream imageStream = wordDocument.RenderAsImages(0, Syncfusion.DocIO.ExportImageFormat.Jpeg);

        imageStream.Position = 0;

        using var magickImage = new MagickImage(imageStream);
        magickImage.Density = new Density(300, 300);
        magickImage.Format = MagickFormat.Tiff;
        magickImage.Settings.Compression = CompressionMethod.LZW;

        magickImage.Write(outputSyncfusionPath);
    });

    BenchmarkRunner("Aspose.Words | DOCX -> TIFF | LZW | 300 DPI | İlk Sayfa", () =>
    {
        var document = new Aspose.Words.Document(docxPath);

        var options = new Aspose.Words.Saving.ImageSaveOptions(Aspose.Words.SaveFormat.Tiff)
        {
            Resolution = 300,
            TiffCompression = Aspose.Words.Saving.TiffCompression.Lzw,
            PageSet = new Aspose.Words.Saving.PageSet(0)
        };

        document.Save(outputAsposePath, options);
    });

    Console.WriteLine();
}

static Bitmap RenderPdfPage(PdfDocument document, int pageIndex, int dpi, bool grayscale)
{
    var pageSizeInPoints = document.PageSizes[pageIndex];

    int width = (int)Math.Ceiling(pageSizeInPoints.Width / 72.0 * dpi);
    int height = (int)Math.Ceiling(pageSizeInPoints.Height / 72.0 * dpi);

    var flags = PdfRenderFlags.ForPrinting;
    if (grayscale)
    {
        flags |= PdfRenderFlags.Grayscale;
    }

    return (Bitmap)document.Render(
        pageIndex,
        width,
        height,
        dpi,
        dpi,
        flags);
}

static Bitmap ConvertTo1BppFast(Bitmap source, byte threshold)
{
    int width = source.Width;
    int height = source.Height;

    using var source32 = new Bitmap(width, height, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(source32))
    {
        graphics.DrawImage(source, 0, 0, width, height);
    }

    var destination = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

    var sourceRect = new Rectangle(0, 0, width, height);
    var destRect = new Rectangle(0, 0, width, height);

    BitmapData? sourceData = null;
    BitmapData? destData = null;

    try
    {
        sourceData = source32.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        destData = destination.LockBits(destRect, ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

        int sourceStride = sourceData.Stride;
        int destStride = destData.Stride;

        byte[] sourceBuffer = new byte[sourceStride * height];
        byte[] destBuffer = new byte[destStride * height];

        Marshal.Copy(sourceData.Scan0, sourceBuffer, 0, sourceBuffer.Length);

        for (int y = 0; y < height; y++)
        {
            int sourceRow = y * sourceStride;
            int destRow = y * destStride;

            for (int x = 0; x < width; x++)
            {
                int sourceIndex = sourceRow + (x * 4);

                byte b = sourceBuffer[sourceIndex + 0];
                byte g = sourceBuffer[sourceIndex + 1];
                byte r = sourceBuffer[sourceIndex + 2];

                int luminance = (r * 299 + g * 587 + b * 114) / 1000;

                if (luminance >= threshold)
                {
                    destBuffer[destRow + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }
        }

        Marshal.Copy(destBuffer, 0, destData.Scan0, destBuffer.Length);
    }
    finally
    {
        if (sourceData is not null)
            source32.UnlockBits(sourceData);

        if (destData is not null)
            destination.UnlockBits(destData);
    }

    var palette = destination.Palette;
    palette.Entries[0] = Color.Black;
    palette.Entries[1] = Color.White;
    destination.Palette = palette;

    return destination;
}

static void RenderPdfPageDirectToCcitt4Tiff(string pdfPath, int pageIndex, int dpi, byte threshold, string outputPath)
{
    PdfiumNative.EnsureInitialized();

    IntPtr document = IntPtr.Zero;
    IntPtr page = IntPtr.Zero;
    IntPtr pdfBitmap = IntPtr.Zero;
    IntPtr grayBuffer = IntPtr.Zero;
    IntPtr monoBuffer = IntPtr.Zero;

    try
    {
        document = PdfiumNative.FPDF_LoadDocument(pdfPath, null);
        if (document == IntPtr.Zero)
            throw new InvalidOperationException("PDFium belgeyi açamadı.");

        page = PdfiumNative.FPDF_LoadPage(document, pageIndex);
        if (page == IntPtr.Zero)
            throw new InvalidOperationException("PDFium sayfayı yükleyemedi.");

        double pageWidthPoints = PdfiumNative.FPDF_GetPageWidth(page);
        double pageHeightPoints = PdfiumNative.FPDF_GetPageHeight(page);

        int width = (int)Math.Ceiling(pageWidthPoints / 72.0 * dpi);
        int height = (int)Math.Ceiling(pageHeightPoints / 72.0 * dpi);

        int grayStride = width;
        int grayBytes = grayStride * height;
        grayBuffer = Marshal.AllocHGlobal(grayBytes);

        pdfBitmap = PdfiumNative.FPDFBitmap_CreateEx(
            width,
            height,
            PdfiumNative.FPDFBitmapFormat.Gray,
            grayBuffer,
            grayStride);

        if (pdfBitmap == IntPtr.Zero)
            throw new InvalidOperationException("PDFium grayscale bitmap oluşturamadı.");

        PdfiumNative.FPDFBitmap_FillRect(pdfBitmap, 0, 0, width, height, 0xFF);
        PdfiumNative.FPDF_RenderPageBitmap(
            pdfBitmap,
            page,
            0,
            0,
            width,
            height,
            0,
            PdfiumNative.FPDF_RENDER_NO_SMOOTHIMAGE);

        int monoStride = ((width + 31) / 32) * 4;
        int monoBytes = monoStride * height;
        monoBuffer = Marshal.AllocHGlobal(monoBytes);

        byte[] grayManaged = new byte[grayBytes];
        byte[] monoManaged = new byte[monoBytes];

        Marshal.Copy(grayBuffer, grayManaged, 0, grayBytes);

        for (int y = 0; y < height; y++)
        {
            int grayRow = y * grayStride;
            int monoRow = y * monoStride;

            for (int x = 0; x < width; x++)
            {
                byte pixel = grayManaged[grayRow + x];

                if (pixel >= threshold)
                {
                    monoManaged[monoRow + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                }
            }
        }

        Marshal.Copy(monoManaged, 0, monoBuffer, monoBytes);

        using var monoBitmap = new Bitmap(width, height, monoStride, PixelFormat.Format1bppIndexed, monoBuffer);

        var palette = monoBitmap.Palette;
        palette.Entries[0] = Color.Black;
        palette.Entries[1] = Color.White;
        monoBitmap.Palette = palette;

        SaveAsCcitt4Tiff(monoBitmap, outputPath, dpi);
    }
    finally
    {
        if (pdfBitmap != IntPtr.Zero)
            PdfiumNative.FPDFBitmap_Destroy(pdfBitmap);

        if (page != IntPtr.Zero)
            PdfiumNative.FPDF_ClosePage(page);

        if (document != IntPtr.Zero)
            PdfiumNative.FPDF_CloseDocument(document);

        if (grayBuffer != IntPtr.Zero)
            Marshal.FreeHGlobal(grayBuffer);

        if (monoBuffer != IntPtr.Zero)
            Marshal.FreeHGlobal(monoBuffer);
    }
}

static void SaveAsCcitt4Tiff(Bitmap bitmap, string outputPath, int dpi)
{
    ImageCodecInfo? tiffCodec = ImageCodecInfo.GetImageEncoders()
        .FirstOrDefault(codec => codec.FormatID == ImageFormat.Tiff.Guid);

    if (tiffCodec is null)
        throw new InvalidOperationException("TIFF encoder bulunamadı.");

    using var encoderParameters = new EncoderParameters(1);
    encoderParameters.Param[0] = new EncoderParameter(
        System.Drawing.Imaging.Encoder.Compression,
        (long)EncoderValue.CompressionCCITT4);

    bitmap.SetResolution(dpi, dpi);
    bitmap.Save(outputPath, tiffCodec, encoderParameters);
}

static void BenchmarkRunner(string testName, Action action)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    using var process = Process.GetCurrentProcess();

    process.Refresh();
    long memoryBeforeBytes = process.PrivateMemorySize64;

    var stopwatch = Stopwatch.StartNew();
    action();
    stopwatch.Stop();

    process.Refresh();
    long memoryAfterBytes = process.PrivateMemorySize64;

    long memoryDiffBytes = memoryAfterBytes - memoryBeforeBytes;

    double memoryBeforeMb = BytesToMb(memoryBeforeBytes);
    double memoryAfterMb = BytesToMb(memoryAfterBytes);
    double memoryDiffMb = BytesToMb(memoryDiffBytes);

    Console.WriteLine(new string('=', 82));
    Console.WriteLine($"Test Adı           : {testName}");
    Console.WriteLine($"Süre               : {stopwatch.ElapsedMilliseconds} ms");
    Console.WriteLine($"İlk RAM            : {memoryBeforeMb:N2} MB");
    Console.WriteLine($"Son RAM            : {memoryAfterMb:N2} MB");
    Console.WriteLine($"Private RAM Farkı  : {memoryDiffMb:N2} MB");
    Console.WriteLine(new string('=', 82));
}

static double BytesToMb(long bytes)
{
    return bytes / (1024.0 * 1024.0);
}

internal static class PdfiumNative
{
    private const string PdfiumDll = "pdfium";
    private static bool _initialized;

    public const int FPDF_RENDER_NO_SMOOTHIMAGE = 0x0200;

    public static class FPDFBitmapFormat
    {
        public const int Gray = 1;
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        FPDF_InitLibrary();
        _initialized = true;
    }

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_InitLibrary();

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_DestroyLibrary();

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr FPDF_LoadDocument(string file_path, string? password);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_ClosePage(IntPtr page);

    [DllImport(PdfiumDll, EntryPoint = "FPDF_GetPageWidth", CallingConvention = CallingConvention.Cdecl)]
    public static extern double FPDF_GetPageWidth(IntPtr page);

    [DllImport(PdfiumDll, EntryPoint = "FPDF_GetPageHeight", CallingConvention = CallingConvention.Cdecl)]
    public static extern double FPDF_GetPageHeight(IntPtr page);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDFBitmap_CreateEx(
        int width,
        int height,
        int format,
        IntPtr first_scan,
        int stride);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFBitmap_FillRect(
        IntPtr bitmap,
        int left,
        int top,
        int width,
        int height,
        uint color);

    [DllImport(PdfiumDll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_RenderPageBitmap(
        IntPtr bitmap,
        IntPtr page,
        int start_x,
        int start_y,
        int size_x,
        int size_y,
        int rotate,
        int flags);
}