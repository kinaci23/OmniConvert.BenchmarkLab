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
}