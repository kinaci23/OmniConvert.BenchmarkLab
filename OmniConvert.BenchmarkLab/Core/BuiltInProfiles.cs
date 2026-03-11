namespace OmniConvert.BenchmarkLab.Core;

public static class BuiltInProfiles
{
    // VISUAL PROFILES

    public static readonly ConversionProfile RasterVisualLzw300 = new()
    {
        Name = "RasterVisualLzw300",
        Intent = ConversionIntent.Visual,
        Dpi = 300,
        ColorMode = TargetColorMode.Rgb24Bit,
        Compression = TiffCompressionKind.Lzw,
        JpegQuality = null,
        Threshold = null,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };

    public static readonly ConversionProfile RasterVisualJpeg300 = new()
    {
        Name = "RasterVisualJpeg300",
        Intent = ConversionIntent.Visual,
        Dpi = 300,
        ColorMode = TargetColorMode.Rgb24Bit,
        Compression = TiffCompressionKind.Jpeg,
        JpegQuality = 85,
        Threshold = null,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = false
    };

    public static readonly ConversionProfile RasterVisualLzw200 = new()
    {
        Name = "RasterVisualLzw200",
        Intent = ConversionIntent.Visual,
        Dpi = 200,
        ColorMode = TargetColorMode.Rgb24Bit,
        Compression = TiffCompressionKind.Lzw,
        JpegQuality = null,
        Threshold = null,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };

    // OCR PROFILES

    public static readonly ConversionProfile RasterOcrGray300 = new()
    {
        Name = "RasterOcrGray300",
        Intent = ConversionIntent.Ocr,
        Dpi = 300,
        ColorMode = TargetColorMode.Grayscale8Bit,
        Compression = TiffCompressionKind.Lzw,
        JpegQuality = null,
        Threshold = null,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };

    public static readonly ConversionProfile RasterOcrBinary300 = new()
    {
        Name = "RasterOcrBinary300",
        Intent = ConversionIntent.Ocr,
        Dpi = 300,
        ColorMode = TargetColorMode.Binary1Bit,
        Compression = TiffCompressionKind.Ccitt4,
        JpegQuality = null,
        Threshold = 180,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };

    public static readonly ConversionProfile RasterOcrGray200 = new()
    {
        Name = "RasterOcrGray200",
        Intent = ConversionIntent.Ocr,
        Dpi = 200,
        ColorMode = TargetColorMode.Grayscale8Bit,
        Compression = TiffCompressionKind.Lzw,
        JpegQuality = null,
        Threshold = null,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };

    public static IReadOnlyList<ConversionProfile> RasterMatrixProfiles { get; } =
        new List<ConversionProfile>
        {
            RasterVisualLzw300,
            RasterVisualJpeg300,
            RasterVisualLzw200,
            RasterOcrGray300,
            RasterOcrBinary300,
            RasterOcrGray200
        };
}