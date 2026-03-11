namespace OmniConvert.BenchmarkLab.Core;

public enum ConversionIntent
{
    Ocr,
    Visual
}

public enum TargetColorMode
{
    Binary1Bit,
    Grayscale8Bit,
    Rgb24Bit
}

public enum TiffCompressionKind
{
    None,
    Lzw,
    Jpeg,
    Ccitt4
}

public sealed record ConversionProfile
{
    public required string Name { get; init; }
    public required ConversionIntent Intent { get; init; }
    public required int Dpi { get; init; }
    public required TargetColorMode ColorMode { get; init; }
    public required TiffCompressionKind Compression { get; init; }

    public int? JpegQuality { get; init; }
    public byte? Threshold { get; init; }

    public bool PreferDirectPdfBinaryPipeline { get; init; }
    public bool PreferLosslessIntermediate { get; init; } = true;

    public static ConversionProfile OcrDefault => new()
    {
        Name = "OCR_PROFILE",
        Intent = ConversionIntent.Ocr,
        Dpi = 300,
        ColorMode = TargetColorMode.Binary1Bit,
        Compression = TiffCompressionKind.Ccitt4,
        Threshold = 180,
        PreferDirectPdfBinaryPipeline = true,
        PreferLosslessIntermediate = true
    };

    public static ConversionProfile VisualDefault => new()
    {
        Name = "VISUAL_PROFILE",
        Intent = ConversionIntent.Visual,
        Dpi = 300,
        ColorMode = TargetColorMode.Rgb24Bit,
        Compression = TiffCompressionKind.Lzw,
        JpegQuality = 85,
        PreferDirectPdfBinaryPipeline = false,
        PreferLosslessIntermediate = true
    };
}