namespace Umea.se.Toolkit.Images;

public sealed record ImageVariantRequest(
    string ImageId,
    int? MaxWidth = null,
    int? MaxHeight = null,
    ImageOutputOptions? Output = null);

public sealed record ImageOutputOptions(ImageOutputFormat Format = ImageOutputFormat.WebP, int Quality = 80)
{
    public string Extension => Format switch
    {
        ImageOutputFormat.WebP => "webp",
        ImageOutputFormat.Jpeg => "jpg",
        ImageOutputFormat.Png => "png",
        _ => throw new ArgumentOutOfRangeException(nameof(Format))
    };

    public string ContentType => Format switch
    {
        ImageOutputFormat.WebP => "image/webp",
        ImageOutputFormat.Jpeg => "image/jpeg",
        ImageOutputFormat.Png => "image/png",
        _ => throw new ArgumentOutOfRangeException(nameof(Format))
    };

    public void Validate()
    {
        if (Quality is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Quality), Quality, "Image quality must be between 1 and 100.");
        }
    }
}

public enum ImageOutputFormat
{
    WebP,
    Jpeg,
    Png
}

public sealed record ImageVariantBytes(byte[] Bytes, string ContentType);
