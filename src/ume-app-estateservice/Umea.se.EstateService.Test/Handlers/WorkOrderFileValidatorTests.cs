using System.Globalization;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderFileValidatorTests
{
    private static readonly byte[] PdfMagic = [0x25, 0x50, 0x44, 0x46, 0x0A];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];
    private static readonly byte[] WebpMagic = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
    private static readonly byte[] RiffNotWebp = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x41, 0x56, 0x49, 0x20];
    private static readonly byte[] HeicMagic = [0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63];

    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private const string DocxContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
          <Default Extension="xml" ContentType="application/xml" />
          <Override PartName="{0}" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" />
        </Types>
        """;

    private const string DocmContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="bin" ContentType="application/vnd.ms-office.vbaProject" />
          <Override PartName="/word/document.xml" ContentType="application/vnd.ms-word.document.macroEnabled.main+xml" />
        </Types>
        """;

    private const string XlsxContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
        </Types>
        """;

    /// <summary>
    /// Builds a ZIP archive from (entry name, content) pairs, mimicking an OPC package.
    /// </summary>
    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using MemoryStream buffer = new();

        using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string name, string content) in entries)
            {
                using StreamWriter writer = new(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A Word package whose main part sits at <paramref name="mainPartName"/> — Word stores it
    /// in word/document2.xml after repairing a document.
    /// </summary>
    private static byte[] DocxBytes(string mainPartName = "/word/document.xml")
        => Zip(
            ("[Content_Types].xml", DocxContentTypesXml.Replace("{0}", mainPartName, StringComparison.Ordinal)),
            (mainPartName.TrimStart('/'), "<document />"));

    private static WorkOrderFileValidator CreateValidator(
        int maxFileCount = 3,
        long maxFileSizeBytes = 1_000,
        params string[] allowedContentTypes)
    {
        if (allowedContentTypes.Length == 0)
        {
            allowedContentTypes = ["application/pdf", "image/*"];
        }

        Dictionary<string, string?> data = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Test",
            ["WorkOrder:FileValidation:MaxFileCount"] = maxFileCount.ToString(CultureInfo.InvariantCulture),
            ["WorkOrder:FileValidation:MaxFileSizeBytes"] = maxFileSizeBytes.ToString(CultureInfo.InvariantCulture),
        };

        for (int i = 0; i < allowedContentTypes.Length; i++)
        {
            data[$"WorkOrder:FileValidation:AllowedContentTypes:{i}"] = allowedContentTypes[i];
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        return new WorkOrderFileValidator(new ApplicationConfig(configuration));
    }

    private static WorkOrderFileUpload Upload(string contentType, byte[] content, long? fileSize = null)
        => new()
        {
            FileName = "file.bin",
            ContentType = contentType,
            FileSize = fileSize ?? content.Length,
            Stream = new MemoryStream(content)
        };

    private static async Task<BusinessValidationException> ValidateExpectingErrors(
        WorkOrderFileValidator validator,
        params WorkOrderFileUpload[] files)
        => await Should.ThrowAsync<BusinessValidationException>(
            async () => await validator.ValidateAsync(files));

    [Fact]
    public async Task ValidateAsync_NoFiles_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([]));
    }

    [Fact]
    public async Task ValidateAsync_ValidPdf_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload("application/pdf", PdfMagic)]));
    }

    [Fact]
    public async Task ValidateAsync_ValidPng_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload("image/png", PngMagic)]));
    }

    [Fact]
    public async Task ValidateAsync_ValidJpeg_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload("image/jpeg", JpegMagic)]));
    }

    [Fact]
    public async Task ValidateAsync_ValidWebp_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload("image/webp", WebpMagic)]));
    }

    [Fact]
    public async Task ValidateAsync_HeicFtypMatchesHeifFamily_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator();

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload("image/heif", HeicMagic)]));
    }

    [Fact]
    public async Task ValidateAsync_TooManyFiles_ReportsTooManyFiles()
    {
        WorkOrderFileValidator validator = CreateValidator(maxFileCount: 2);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("application/pdf", PdfMagic),
            Upload("application/pdf", PdfMagic),
            Upload("application/pdf", PdfMagic));

        ex.Errors.ShouldContainKey("files");
        ex.Errors["files"].ShouldContain(ValidationErrorCode.TooManyFiles);
    }

    [Fact]
    public async Task ValidateAsync_FileTooLarge_ReportsFileTooLarge()
    {
        WorkOrderFileValidator validator = CreateValidator(maxFileSizeBytes: 4);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("application/pdf", PdfMagic, fileSize: 5_000));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.FileTooLarge);
    }

    [Fact]
    public async Task ValidateAsync_DisallowedContentType_ReportsInvalidContentType()
    {
        WorkOrderFileValidator validator = CreateValidator();

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("text/plain", PdfMagic));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.InvalidContentType);
    }

    [Fact]
    public async Task ValidateAsync_UnrecognizedContent_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator();

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("image/png", [0x01, 0x02, 0x03, 0x04]));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_TooShortToDetect_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator();

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("image/png", [0x89]));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_RiffButNotWebp_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator();

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("image/png", RiffNotWebp));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_ContentTypeMismatch_ReportsContentTypeMismatch()
    {
        WorkOrderFileValidator validator = CreateValidator();

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("image/png", PdfMagic));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.ContentTypeMismatch);
    }

    [Fact]
    public async Task ValidateAsync_MultipleInvalidFiles_ReportsPerFileErrors()
    {
        WorkOrderFileValidator validator = CreateValidator(maxFileSizeBytes: 4);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("application/pdf", PdfMagic, fileSize: 5_000),
            Upload("text/plain", PdfMagic));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.FileTooLarge);
        ex.Errors["files[1]"].ShouldContain(ValidationErrorCode.InvalidContentType);
    }

    [Fact]
    public async Task ValidateAsync_ValidDocx_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        await Should.NotThrowAsync(async () => await validator.ValidateAsync([Upload(DocxContentType, DocxBytes())]));
    }

    [Fact]
    public async Task ValidateAsync_RepairedDocxWithRenamedMainPart_DoesNotThrow()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        await Should.NotThrowAsync(async () =>
            await validator.ValidateAsync([Upload(DocxContentType, DocxBytes("/word/document2.xml"))]));
    }

    [Fact]
    public async Task ValidateAsync_MacroEnabledDocument_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(
                ("[Content_Types].xml", DocmContentTypesXml),
                ("word/document.xml", "<document />"),
                ("word/vbaProject.bin", "vba"))));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_MacroPartRenamedToEvadeNameCheck_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(
                ("[Content_Types].xml", DocmContentTypesXml),
                ("word/document.xml", "<document />"),
                ("word/vbaProject2.bin", "vba"))));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_VbaPartWithoutMacroContentType_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(
                ("[Content_Types].xml", DocxContentTypesXml.Replace("{0}", "/word/document.xml", StringComparison.Ordinal)),
                ("word/document.xml", "<document />"),
                ("word/vbaProject.bin", "vba"))));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_SpreadsheetClaimedAsDocx_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(
                ("[Content_Types].xml", XlsxContentTypesXml),
                ("xl/workbook.xml", "<workbook />"))));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_PlainZipClaimedAsDocx_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: DocxContentType);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(("notes.txt", "hello"))));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_ArchiveWithImplausibleEntryCount_ReportsUnrecognizedFileContent()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 2_000_000,
            allowedContentTypes: DocxContentType);

        (string, string)[] entries =
        [
            ("[Content_Types].xml", DocxContentTypesXml.Replace("{0}", "/word/document.xml", StringComparison.Ordinal)),
            ("word/document.xml", "<document />"),
            .. Enumerable.Range(0, 1_200).Select(i => ($"filler/{i}.bin", "x"))
        ];

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, Zip(entries)));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.UnrecognizedFileContent);
    }

    [Fact]
    public async Task ValidateAsync_PdfClaimedAsDocx_ReportsContentTypeMismatch()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: ["application/pdf", DocxContentType]);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload(DocxContentType, PdfMagic));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.ContentTypeMismatch);
    }

    [Fact]
    public async Task ValidateAsync_DocxClaimedAsPdf_ReportsContentTypeMismatch()
    {
        WorkOrderFileValidator validator = CreateValidator(
            maxFileSizeBytes: 200_000,
            allowedContentTypes: ["application/pdf", DocxContentType]);

        BusinessValidationException ex = await ValidateExpectingErrors(
            validator,
            Upload("application/pdf", DocxBytes()));

        ex.Errors["files[0]"].ShouldContain(ValidationErrorCode.ContentTypeMismatch);
    }
}
