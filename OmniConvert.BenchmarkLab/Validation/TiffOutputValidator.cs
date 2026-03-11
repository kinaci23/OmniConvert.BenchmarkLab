using ImageMagick;
using OmniConvert.BenchmarkLab.Core;

namespace OmniConvert.BenchmarkLab.Validation;

public sealed class TiffOutputValidator : IOutputValidator
{
    public async Task<OutputValidationResult> ValidateAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(request.OutputPath))
            {
                return new OutputValidationResult
                {
                    IsValid = false,
                    Message = "Çıktı dosyası bulunamadı.",
                    FileExists = false,
                    FileSizeBytes = 0
                };
            }

            var fileInfo = new FileInfo(request.OutputPath);

            if (fileInfo.Length <= 0)
            {
                return new OutputValidationResult
                {
                    IsValid = false,
                    Message = "Çıktı dosyası boş.",
                    FileExists = true,
                    FileSizeBytes = 0
                };
            }

            using var image = new MagickImage(request.OutputPath);

            bool isTiff = image.Format == MagickFormat.Tiff || image.Format == MagickFormat.Tif;
            bool hasExpectedDpi =
                Math.Abs(image.Density.X - request.Profile.Dpi) < 0.01 &&
                Math.Abs(image.Density.Y - request.Profile.Dpi) < 0.01;

            bool isValid = isTiff && hasExpectedDpi;

            string message = isValid
                ? "Çıktı geçerli."
                : $"Geçersiz çıktı. Format={image.Format}, DPI=({image.Density.X},{image.Density.Y})";

            return new OutputValidationResult
            {
                IsValid = isValid,
                Message = message,
                FileExists = true,
                FileSizeBytes = fileInfo.Length,
                Width = (int)image.Width,
                Height = (int)image.Height,
                DpiX = image.Density.X,
                DpiY = image.Density.Y,
                Format = image.Format.ToString()
            };
        }, cancellationToken);
    }
}