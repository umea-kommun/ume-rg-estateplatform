using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Umea.se.Toolkit.Images;

public static class ImageVariantEncoder
{
    private const int MaximumSourceDimension = 20000;

    public static ImageVariantBytes Resize(
        byte[] source,
        int? maxWidth,
        int? maxHeight,
        ImageOutputOptions outputOptions)
    {
        using MemoryStream input = new(source, writable: false);
        return Resize(input, maxWidth, maxHeight, outputOptions);
    }

    public static ImageVariantBytes Resize(
        Stream source,
        int? maxWidth,
        int? maxHeight,
        ImageOutputOptions outputOptions)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(outputOptions);
        outputOptions.Validate();

        using MemoryStream? buffered = source.CanSeek ? null : Buffer(source);
        Stream input = buffered ?? source;

        input.Position = 0;
        ImageInfo info = Image.Identify(input);

        if (info.Width > MaximumSourceDimension || info.Height > MaximumSourceDimension)
        {
            throw new ImageTooLargeException(
                $"Image dimensions ({info.Width}x{info.Height}) exceed maximum allowed ({MaximumSourceDimension}x{MaximumSourceDimension})");
        }

        input.Position = 0;
        using Image image = Image.Load(input);
        (int targetW, int targetH) = CalculateTargetDimensions(info.Width, info.Height, maxWidth, maxHeight);

        if (targetW < info.Width || targetH < info.Height)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetW, targetH),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3,
            }));
        }

        using MemoryStream output = new();
        Save(image, output, outputOptions);
        return new ImageVariantBytes(output.ToArray(), outputOptions.ContentType);
    }

    private static MemoryStream Buffer(Stream source)
    {
        MemoryStream buffered = new();
        source.CopyTo(buffered);
        buffered.Position = 0;
        return buffered;
    }

    private static void Save(Image image, Stream output, ImageOutputOptions outputOptions)
    {
        switch (outputOptions.Format)
        {
            case ImageOutputFormat.WebP:
                image.SaveAsWebp(output, new WebpEncoder { Quality = outputOptions.Quality });
                break;
            case ImageOutputFormat.Jpeg:
                image.SaveAsJpeg(output, new JpegEncoder { Quality = outputOptions.Quality });
                break;
            case ImageOutputFormat.Png:
                image.SaveAsPng(output, new PngEncoder());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outputOptions), outputOptions.Format, "Unsupported image format.");
        }
    }

    private static (int width, int height) CalculateTargetDimensions(int sourceW, int sourceH, int? maxW, int? maxH)
    {
        double wRatio = maxW is > 0 ? (double)maxW.Value / sourceW : double.MaxValue;
        double hRatio = maxH is > 0 ? (double)maxH.Value / sourceH : double.MaxValue;
        double ratio = Math.Min(Math.Min(wRatio, hRatio), 1.0);

        return ((int)Math.Round(sourceW * ratio), (int)Math.Round(sourceH * ratio));
    }
}
