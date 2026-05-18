namespace Umea.se.Toolkit.Images;

/// <summary>
/// WebP resize utility. Encodes resized output as WebP with the supplied quality.
/// </summary>
public static class WebPResizer
{
    public static byte[] Resize(byte[] source, int? maxWidth, int? maxHeight, int quality)
    {
        using MemoryStream input = new(source, writable: false);
        return Resize(input, maxWidth, maxHeight, quality);
    }

    public static byte[] Resize(Stream source, int? maxWidth, int? maxHeight, int quality)
    {
        ImageVariantBytes variant = ImageVariantEncoder.Resize(
            source,
            maxWidth,
            maxHeight,
            new ImageOutputOptions(ImageOutputFormat.WebP, quality));
        return variant.Bytes;
    }
}
