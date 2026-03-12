using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Inputs;

public sealed class InputDatasetLoader
{
    public IReadOnlyList<InputSample> LoadRasterSamples(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Input klasörü bulunamadı: {folderPath}");
        }

        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".tif",
            ".tiff"
        };

        var files = Directory
            .GetFiles(folderPath)
            .Where(path => supportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => path)
            .ToList();

        return files.Select(path =>
        {
            string fileName = Path.GetFileName(path);
            var (category, notes) = InferRasterMetadata(fileName);

            return new InputSample
            {
                Name = fileName,
                FullPath = path,
                SourceType = ConversionSourceType.Raster,
                Category = category,
                Notes = notes
            };
        }).ToList();
    }

    public IReadOnlyList<InputSample> LoadPdfSamples(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Input klasörü bulunamadı: {folderPath}");
        }

        var files = Directory
            .GetFiles(folderPath, "*.pdf")
            .OrderBy(path => path)
            .ToList();

        return files.Select(path =>
        {
            string fileName = Path.GetFileName(path);
            var (Category, Notes) = InferRasterMetadata(fileName);

            return new InputSample
            {
                Name = fileName,
                FullPath = path,
                SourceType = ConversionSourceType.Pdf,
                Category = Category,
                Notes = Notes
            };
        }).ToList();
    }

    private static (string Category, string Notes) InferRasterMetadata(string fileName)
    {
        string normalized = fileName.ToLowerInvariant();

        if (normalized.Contains("large"))
            return ("large-photo", "Yüksek boyutlu raster görsel");

        if (normalized.Contains("small"))
            return ("small-photo", "Düşük boyutlu raster görsel");

        if (normalized.Contains("ui") || normalized.Contains("png"))
            return ("ui-graphic", "Arayüz veya grafik ağırlıklı görsel");

        if (normalized.Contains("scan"))
            return ("document-scan", "Belge taraması benzeri örnek");

        return ("unknown", "Otomatik sınıflandırılamadı");
    }

    private static (string Category, string Notes) InferPdfMetadata(string fileName)
    {
        string normalized = fileName.ToLowerInvariant();

        if (normalized.Contains("text"))
            return ("text-document", "Metin ağırlıklı PDF örneği");

        if (normalized.Contains("scan"))
            return ("scanned-document", "OCR odaklı taranmış PDF örneği");

        if (normalized.Contains("photo") || normalized.Contains("image"))
            return ("photo-heavy-document", "Görsel/fotoğraf ağırlıklı PDF örneği");

        return ("unknown-pdf", "Otomatik sınıflandırılamadı");
    }
}