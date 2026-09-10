using System.IO.Compression;
using System.Text;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderFileValidator(ApplicationConfig appConfig)
{
    private readonly WorkOrderFileValidationConfig _config = appConfig.WorkOrderProcessing.FileValidation;

    // Magic byte signatures for common file types.
    // Order matters: longer/more-specific signatures should come first for a given MIME type.
    private static readonly (string MimeType, byte[] Magic)[] _magicSignatures =
    [
        ("application/pdf",  [0x25, 0x50, 0x44, 0x46]),                         // %PDF
        ("image/png",        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]), // .PNG....
        ("image/jpeg",       [0xFF, 0xD8, 0xFF]),                               // JFIF/EXIF/generic JPEG
        ("image/gif",        [0x47, 0x49, 0x46, 0x38]),                         // GIF8 (covers GIF87a and GIF89a)
        ("image/webp",       [0x52, 0x49, 0x46, 0x46]),                         // RIFF (WebP container)
        ("image/bmp",        [0x42, 0x4D]),                                     // BM
        ("image/tiff",       [0x49, 0x49, 0x2A, 0x00]),                         // II*. (little-endian TIFF)
        ("image/tiff",       [0x4D, 0x4D, 0x00, 0x2A]),                         // MM.* (big-endian TIFF)
        ("image/heic",       []),                                               // checked via ftyp box below
    ];

    // ftyp-based formats (HEIC/HEIF) — the "ftyp" marker sits at offset 4
    private static readonly byte[] _ftypMarker = [0x66, 0x74, 0x79, 0x70];       // "ftyp"
    private static readonly string[] _heicBrands = ["heic", "heix", "mif1"];

    // ZIP-container formats (OOXML). The PK signature only says "some zip archive", so the
    // concrete format is confirmed from the content types the package declares for its parts.
    private static readonly byte[] _zipMagic = [0x50, 0x4B, 0x03, 0x04];         // PK..
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    // Every OPC package declares a content type for each part here. Part *names* are resolved
    // through relationships and are not fixed (a repaired .docx keeps its main part in
    // word/document2.xml), so the declared content type is what identifies the format.
    private const string ContentTypesEntry = "[Content_Types].xml";
    private const string DocxMainPartContentType = "wordprocessingml.document.main+xml";

    // Macro carriers. A .docm has the same layout as a .docx, and its VBA part can be named
    // anything, so both the declared content types and the entry names are checked.
    private static readonly string[] _macroContentTypeMarkers = ["vbaProject", "macroEnabled"];
    private const string VbaProjectEntrySuffix = "vbaproject.bin";

    // The declared content types of a Word package are a few hundred bytes; anything far
    // larger is not a document we want to parse.
    private const int MaxContentTypesBytes = 256 * 1024;

    // A Word document has tens of parts. A higher count means a generic archive — and reading
    // the central directory of one is what costs: a 20 MB archive can declare ~250k entries.
    private const int MaxZipEntries = 1_000;

    // End Of Central Directory record: 4-byte signature, 22-byte record, and it can sit up to
    // a 64 KB comment away from the end of the file.
    private const int MinEocdBytes = 22;
    private const int MaxEocdSearchBytes = 65_557;

    // Maximum bytes we need to read for any signature check
    private const int MaxHeaderBytes = 12;

    public async Task ValidateAsync(IReadOnlyList<WorkOrderFileUpload> files, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = new();

        if (files.Count > _config.MaxFileCount)
        {
            errors.AddError("files", ValidationErrorCode.TooManyFiles);
        }

        for (int i = 0; i < files.Count; i++)
        {
            WorkOrderFileUpload file = files[i];
            string key = $"files[{i}]";

            if (file.FileSize > _config.MaxFileSizeBytes)
            {
                errors.AddError(key, ValidationErrorCode.FileTooLarge);
            }

            if (!IsAllowedContentType(file.ContentType))
            {
                errors.AddError(key, ValidationErrorCode.InvalidContentType);
                continue; // Skip magic byte check when content type is already disallowed
            }

            string? detectedType = await DetectContentTypeAsync(file.Stream, cancellationToken);

            if (detectedType is null)
            {
                errors.AddError(key, ValidationErrorCode.UnrecognizedFileContent);
            }
            else if (!ContentTypeMatchesClaimed(detectedType, file.ContentType))
            {
                errors.AddError(key, ValidationErrorCode.ContentTypeMismatch);
            }
        }

        errors.ThrowIfErrors();
    }

    private static async Task<string?> DetectContentTypeAsync(Stream stream, CancellationToken ct)
    {
        byte[] header = new byte[MaxHeaderBytes];
        long originalPosition = stream.CanSeek ? stream.Position : -1;

        int bytesRead = await ReadExactAsync(stream, header, ct);

        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        if (bytesRead < 2)
        {
            return null;
        }

        // Check ftyp-based formats (HEIC/HEIF) — "ftyp" at offset 4, brand at offset 8
        if (bytesRead >= 12 && header.AsSpan(4, 4).SequenceEqual(_ftypMarker))
        {
            string brand = System.Text.Encoding.ASCII.GetString(header, 8, 4).ToLowerInvariant();
            if (Array.Exists(_heicBrands, b => b == brand))
            {
                return "image/heic";
            }
        }

        // Check ZIP-container formats (docx) — needs the whole archive, not just the header
        if (bytesRead >= _zipMagic.Length && header.AsSpan(0, _zipMagic.Length).SequenceEqual(_zipMagic))
        {
            return DetectZipContainerType(stream);
        }

        // Check fixed-offset magic signatures
        foreach ((string mimeType, byte[] magic) in _magicSignatures)
        {
            if (magic.Length == 0)
            {
                continue; // handled above (ftyp-based)
            }

            if (bytesRead >= magic.Length && header.AsSpan(0, magic.Length).SequenceEqual(magic))
            {
                // RIFF container could be WebP or something else — verify "WEBP" at offset 8
                if (mimeType == "image/webp")
                {
                    if (bytesRead >= 12 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                    {
                        return "image/webp";
                    }

                    continue; // RIFF but not WebP
                }

                return mimeType;
            }
        }

        return null;
    }

    /// <summary>
    /// Identifies a ZIP archive by the content types its package declares. Only Word documents
    /// (.docx) are recognized; every other archive — including .xlsx, macro-enabled .docm and
    /// plain .zip — returns null and is reported as unrecognized content.
    /// </summary>
    private static string? DetectZipContainerType(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return null; // Cannot inspect the archive without being able to rewind afterwards
        }

        long originalPosition = stream.Position;

        try
        {
            if (!HasPlausibleEntryCount(stream))
            {
                return null;
            }

            stream.Position = originalPosition;

            using ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: true);

            if (archive.Entries.Any(e => e.FullName.EndsWith(VbaProjectEntrySuffix, StringComparison.OrdinalIgnoreCase)))
            {
                return null; // Carries a VBA project
            }

            string? contentTypes = ReadContentTypes(archive);

            if (contentTypes is null
                || Array.Exists(_macroContentTypeMarkers, m => contentTypes.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                return null; // Not an OPC package, or a macro-enabled one
            }

            return contentTypes.Contains(DocxMainPartContentType, StringComparison.OrdinalIgnoreCase)
                ? DocxContentType
                : null;
        }
        catch (InvalidDataException)
        {
            return null; // Truncated or corrupt archive
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Reads the entry count straight out of the End Of Central Directory record, so a hostile
    /// archive is rejected before <see cref="ZipArchive"/> materializes its whole directory.
    /// </summary>
    private static bool HasPlausibleEntryCount(Stream stream)
    {
        int searchLength = (int)Math.Min(stream.Length, MaxEocdSearchBytes);

        if (searchLength < MinEocdBytes)
        {
            return false;
        }

        byte[] tail = new byte[searchLength];
        stream.Position = stream.Length - searchLength;
        stream.ReadExactly(tail);

        // Scan backwards: the record sits at the end, ahead of an optional comment
        for (int i = searchLength - MinEocdBytes; i >= 0; i--)
        {
            if (tail[i] != 0x50 || tail[i + 1] != 0x4B || tail[i + 2] != 0x05 || tail[i + 3] != 0x06)
            {
                continue;
            }

            // Total entries is a 16-bit field at offset 10; 0xFFFF means the real count lives
            // in a ZIP64 record, which is orders of magnitude past what a document needs.
            int entryCount = tail[i + 10] | (tail[i + 11] << 8);
            return entryCount <= MaxZipEntries;
        }

        return false; // No End Of Central Directory record — not a usable archive
    }

    /// <summary>
    /// Reads [Content_Types].xml as text, capped so a compressed bomb cannot be expanded.
    /// The file contains nothing but content-type declarations, so the caller matches on
    /// substrings rather than parsing XML from an untrusted archive.
    /// </summary>
    private static string? ReadContentTypes(ZipArchive archive)
    {
        ZipArchiveEntry? entry = archive.GetEntry(ContentTypesEntry);

        if (entry is null || entry.Length > MaxContentTypesBytes)
        {
            return null;
        }

        byte[] buffer = new byte[MaxContentTypesBytes];
        using Stream entryStream = entry.Open();
        int totalRead = 0;

        while (totalRead < buffer.Length)
        {
            int read = entryStream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return Encoding.UTF8.GetString(buffer, 0, totalRead);
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), ct);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }

    /// <summary>
    /// Checks whether the detected MIME type is compatible with the claimed type.
    /// For example, detected "image/jpeg" is compatible with claimed "image/jpeg",
    /// and detected "image/heic" with claimed "image/heif". Outside the image family
    /// the types must match exactly.
    /// </summary>
    private static bool ContentTypeMatchesClaimed(string detected, string claimed)
    {
        if (string.Equals(detected, claimed, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Allow same family for images only — e.g. detected "image/heic" is fine if claimed
        // is "image/heif". The "application" family is far too coarse for this: it would let
        // a PDF pass as a Word document and vice versa.
        string? detectedFamily = GetFamily(detected);
        string? claimedFamily = GetFamily(claimed);

        return detectedFamily is "image" && string.Equals(detectedFamily, claimedFamily, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetFamily(string contentType)
    {
        int slash = contentType.IndexOf('/');
        return slash > 0 ? contentType[..slash] : null;
    }

    private bool IsAllowedContentType(string contentType)
    {
        foreach (string allowed in _config.AllowedContentTypes)
        {
            if (allowed.EndsWith("/*", StringComparison.Ordinal))
            {
                string prefix = allowed[..^1]; // "image/*" → "image/"
                if (contentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(contentType, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
