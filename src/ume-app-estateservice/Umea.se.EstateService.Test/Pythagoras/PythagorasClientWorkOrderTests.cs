using System.Net;
using System.Text;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasClientWorkOrderTests
{
    private static (PythagorasClient client, StubHandler handler, MultiClientFactory factory) Build(
        HttpStatusCode status = HttpStatusCode.OK,
        string content = "{}",
        string contentType = "application/json")
        => Build(new StubHandler(status, content, contentType));

    private static (PythagorasClient client, StubHandler handler, MultiClientFactory factory) Build(StubHandler handler)
    {
        MultiClientFactory factory = new(handler);
        PythagorasClient client = new(factory);
        return (client, handler, factory);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_NullRequest_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await client.CreateWorkOrderAsync(PythagorasWorkOrderType.ErrorReport, PythagorasWorkOrderOrigin.YOURSPACE, null!));
    }

    [Fact]
    public async Task CreateWorkOrderAsync_Success_DeserializesAndBuildsRequest()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"id\":42,\"name\":\"WO\"}");

        WorkOrderDto? result = await client.CreateWorkOrderAsync(
            PythagorasWorkOrderType.SpaceRequirement,
            PythagorasWorkOrderOrigin.YOURSPACE,
            new CreatePythagorasWorkOrderRequest { Description = "Broken lamp" });

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(42);
        result.Name.ShouldBe("WO");

        HttpRequestMessage request = handler.LastRequest.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workordertype/3/workorder/newdefault?origin=YOURSPACE");
    }

    [Fact]
    public async Task CreateWorkOrderAsync_NonSuccess_ThrowsPythagorasApiException()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.BadRequest, "boom");

        PythagorasApiException ex = await Should.ThrowAsync<PythagorasApiException>(
            async () => await client.CreateWorkOrderAsync(
                PythagorasWorkOrderType.ErrorReport,
                PythagorasWorkOrderOrigin.MY_PAGES,
                new CreatePythagorasWorkOrderRequest()));

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.ResponseBody.ShouldBe("boom");
    }

    [Fact]
    public async Task CreateWorkOrderAsync_SuccessButDtoDeserializationFails_ExtractsIdFromBody()
    {
        (PythagorasClient client, _, _) = Build(content: "{\"id\":99,\"vandalism\":\"not-a-bool\"}");

        WorkOrderDto? result = await client.CreateWorkOrderAsync(
            PythagorasWorkOrderType.ErrorReport,
            PythagorasWorkOrderOrigin.MY_PAGES,
            new CreatePythagorasWorkOrderRequest());

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(99);
    }

    [Fact]
    public async Task CreateWorkOrderAsync_SuccessButUnparseableBody_ThrowsPythagorasApiException()
    {
        (PythagorasClient client, _, _) = Build(content: "not json at all");

        await Should.ThrowAsync<PythagorasApiException>(
            async () => await client.CreateWorkOrderAsync(
                PythagorasWorkOrderType.ErrorReport,
                PythagorasWorkOrderOrigin.MY_PAGES,
                new CreatePythagorasWorkOrderRequest()));
    }

    [Fact]
    public async Task GetWorkOrderAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetWorkOrderAsync(0));
    }

    [Fact]
    public async Task GetWorkOrderAsync_Success_ReturnsDto()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"id\":7}");

        WorkOrderDto? result = await client.GetWorkOrderAsync(7);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(7);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workorder/7/info");
    }

    [Fact]
    public async Task GetWorkOrderAsync_NotFound_ReturnsNull()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.NotFound, "");

        WorkOrderDto? result = await client.GetWorkOrderAsync(7);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetWorkOrdersByIdsAsync_EmptyIds_ReturnsEmptyWithoutRequest()
    {
        (PythagorasClient client, StubHandler handler, _) = Build();

        IReadOnlyList<WorkOrderDto> result = await client.GetWorkOrdersByIdsAsync([]);

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task GetWorkOrdersByIdsAsync_WithIds_QueriesAndDeserializes()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "[{\"id\":1},{\"id\":2}]");

        IReadOnlyList<WorkOrderDto> result = await client.GetWorkOrdersByIdsAsync([1, 2]);

        result.Select(w => w.Id).ShouldBe([1, 2]);
        handler.LastRequest!.RequestUri!.ToString().ShouldContain("rest/v1/workorder");
        handler.LastRequest.RequestUri!.ToString().ShouldContain("id%5B%5D=1");
        handler.LastRequest.RequestUri!.ToString().ShouldContain("id%5B%5D=2");
    }

    [Fact]
    public async Task GetWorkOrderInfosByIdsAsync_EmptyIds_ReturnsEmptyWithoutRequest()
    {
        (PythagorasClient client, StubHandler handler, _) = Build();

        IReadOnlyList<WorkOrderInfoDto> result = await client.GetWorkOrderInfosByIdsAsync([]);

        result.ShouldBeEmpty();
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task GetWorkOrderInfosByIdsAsync_Success_PostsAndDeserializes()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "[{\"id\":5,\"statusCategory\":\"OPEN\"}]");

        IReadOnlyList<WorkOrderInfoDto> result = await client.GetWorkOrderInfosByIdsAsync([5]);

        WorkOrderInfoDto info = result.ShouldHaveSingleItem();
        info.Id.ShouldBe(5);
        info.StatusCategory.ShouldBe("OPEN");

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workorder/info");
        handler.LastRequestContent.ShouldBe("[5]");
    }

    [Fact]
    public async Task GetWorkOrderInfosByIdsAsync_NonSuccess_Throws()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.InternalServerError, "fail");

        PythagorasApiException ex = await Should.ThrowAsync<PythagorasApiException>(
            async () => await client.GetWorkOrderInfosByIdsAsync([5]));

        ex.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 0)]
    public async Task SetWorkOrderCategoryAsync_NonPositiveArgs_Throws(int workOrderId, int categoryId)
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.SetWorkOrderCategoryAsync(workOrderId, categoryId));
    }

    [Fact]
    public async Task SetWorkOrderCategoryAsync_Success_PutsCategory()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"id\":12}");

        WorkOrderDto? result = await client.SetWorkOrderCategoryAsync(12, 34);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(12);
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workorder/12/category");
        handler.LastRequestContent.ShouldBe("34");
    }

    [Fact]
    public async Task UploadWorkOrderDocumentAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();
        using MemoryStream stream = new([1, 2, 3]);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.UploadWorkOrderDocumentAsync(0, stream, "f.pdf", 3));
    }

    [Fact]
    public async Task UploadWorkOrderDocumentAsync_Success_PostsMultipart()
    {
        (PythagorasClient client, StubHandler handler, _) = Build();
        using MemoryStream stream = new([1, 2, 3]);

        await client.UploadWorkOrderDocumentAsync(15, stream, "plan.pdf", 3, actionTypeId: 7, actionTypeStatusId: 9);

        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workorder/15/documentfile/record");
        handler.LastRequestContent.ShouldNotBeNull();
        handler.LastRequestContent!.ShouldContain("plan.pdf");
        handler.LastRequestContent.ShouldContain("fileRecordData");
    }

    [Fact]
    public async Task UploadWorkOrderDocumentAsync_NonSuccess_Throws()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.BadRequest, "");
        using MemoryStream stream = new([1, 2, 3]);

        await Should.ThrowAsync<HttpRequestException>(
            async () => await client.UploadWorkOrderDocumentAsync(15, stream, "plan.pdf", 3));
    }

    [Fact]
    public async Task GetWorkOrderCategoriesAsync_NonPositiveModule_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetWorkOrderCategoriesAsync(0));
    }

    [Fact]
    public async Task GetWorkOrderCategoriesAsync_Success_ReturnsList()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "[{\"id\":1},{\"id\":2}]");

        IReadOnlyList<WorkOrderCategoryInfoDto> result = await client.GetWorkOrderCategoriesAsync(50);

        result.Count.ShouldBe(2);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/workordermodule/50/workordercategory/info");
    }

    [Fact]
    public async Task GetBuildingGalleryImagesAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetBuildingGalleryImagesAsync(0));
    }

    [Fact]
    public async Task GetBuildingGalleryImagesAsync_Success_ReturnsList()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "[{\"id\":3,\"name\":\"img\"}]");

        IReadOnlyList<GalleryImageFile> result = await client.GetBuildingGalleryImagesAsync(10);

        GalleryImageFile image = result.ShouldHaveSingleItem();
        image.Id.ShouldBe(3);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/10/galleryimagefile");
    }

    [Fact]
    public async Task GetGalleryImageDataAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetGalleryImageDataAsync(0, GalleryImageVariant.Original));
    }

    [Theory]
    [InlineData(GalleryImageVariant.Original, "https://example.org/rest/v1/galleryimagefile/8/data?pyApp=se.pythagoras.pythagorasweb")]
    [InlineData(GalleryImageVariant.Thumbnail, "https://example.org/rest/v1/galleryimagefile/8/thumbnail/data?pyApp=se.pythagoras.pythagorasweb")]
    public async Task GetGalleryImageDataAsync_Variant_UsesImageClientAndExpectedUri(GalleryImageVariant variant, string expectedUri)
    {
        (PythagorasClient client, StubHandler handler, MultiClientFactory factory) = Build();

        using HttpResponseMessage response = await client.GetGalleryImageDataAsync(8, variant);

        response.IsSuccessStatusCode.ShouldBeTrue();
        handler.LastRequest!.RequestUri!.ToString().ShouldBe(expectedUri);
        factory.RequestedNames.ShouldContain(HttpClientNames.PythagorasImages);
    }

    [Fact]
    public async Task GetGalleryImageDataAsync_UnsupportedVariant_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetGalleryImageDataAsync(8, (GalleryImageVariant)999));
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetFloorBlueprintAsync(0, BlueprintFormat.Svg, workspaceTexts: null));
    }

    [Theory]
    [InlineData(BlueprintFormat.Pdf, "rest/v1/floor/4/gmodel/print/pdf")]
    [InlineData(BlueprintFormat.Svg, "rest/v1/floor/4/gmodel/print/svg")]
    public async Task GetFloorBlueprintAsync_Format_UsesBlueprintClientAndExpectedEndpoint(BlueprintFormat format, string expectedPath)
    {
        (PythagorasClient client, StubHandler handler, MultiClientFactory factory) = Build();

        using HttpResponseMessage response = await client.GetFloorBlueprintAsync(4, format, workspaceTexts: null);

        response.IsSuccessStatusCode.ShouldBeTrue();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe($"https://example.org/{expectedPath}");
        factory.RequestedNames.ShouldContain(HttpClientNames.PythagorasBlueprints);
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_WithWorkspaceTexts_SerializesContent()
    {
        (PythagorasClient client, StubHandler handler, _) = Build();
        Dictionary<int, IReadOnlyList<string>> texts = new() { [101] = ["Room A"] };

        using HttpResponseMessage response = await client.GetFloorBlueprintAsync(4, BlueprintFormat.Svg, texts);

        response.IsSuccessStatusCode.ShouldBeTrue();
        handler.LastRequestContent.ShouldNotBeNull();
        handler.LastRequestContent!.ShouldContain("requestObject");
    }

    [Fact]
    public async Task GetFloorBlueprintAsync_UnsupportedFormat_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetFloorBlueprintAsync(4, (BlueprintFormat)999, workspaceTexts: null));
    }

    [Fact]
    public async Task GetBuildingCalculatedPropertyValuesAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetBuildingCalculatedPropertyValuesAsync(0));
    }

    [Fact]
    public async Task GetBuildingCalculatedPropertyValuesAsync_Success_ReturnsDictionary()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"226\":{\"outputValue\":\"42\",\"valid\":true}}");

        IReadOnlyDictionary<int, CalculatedPropertyValueDto> result =
            await client.GetBuildingCalculatedPropertyValuesAsync(10, new CalculatedPropertyValueRequest { PropertyIds = [226] });

        result.ShouldContainKey(226);
        result[226].OutputValue.ShouldBe("42");
        handler.LastRequest!.RequestUri!.ToString().ShouldContain("rest/v1/building/10/property/calculatedvalue");
        handler.LastRequest.RequestUri!.ToString().ShouldContain("propertyIds%5B%5D=226");
    }

    [Fact]
    public async Task GetCalculatedPropertyValuesForEstateAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetCalculatedPropertyValuesForEstateAsync(0));
    }

    [Fact]
    public async Task GetCalculatedPropertyValuesForEstateAsync_Success_UsesNavigationFolderEndpoint()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{}");

        await client.GetCalculatedPropertyValuesForEstateAsync(20);

        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/navigationfolder/20/property/calculatedvalue");
    }

    [Fact]
    public async Task GetDocument_Success_ReturnsDataAndContentType()
    {
        StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([10, 20, 30])
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
            }
        });
        (PythagorasClient client, _, _) = Build(handler);

        (byte[] data, string contentType) = await client.GetDocument(5);

        data.ShouldBe([10, 20, 30]);
        contentType.ShouldBe("image/png");
    }

    [Fact]
    public async Task GetDocument_NonSuccess_Throws()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.NotFound, "");

        await Should.ThrowAsync<HttpRequestException>(
            async () => await client.GetDocument(5));
    }

    [Fact]
    public async Task GetDocumentInfoAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetDocumentInfoAsync(0));
    }

    [Fact]
    public async Task GetDocumentInfoAsync_NotFound_ReturnsNull()
    {
        (PythagorasClient client, _, _) = Build(HttpStatusCode.NotFound, "");

        FileDocumentInfo? result = await client.GetDocumentInfoAsync(5);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDirectory_Success_ReturnsDirectory()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"id\":9}");

        FileDocumentDirectory? result = await client.GetDirectory(9);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(9);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/documentfolder/9/info");
    }

    [Fact]
    public async Task GetDocumentRecordActionTypeStatusesAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetDocumentRecordActionTypeStatusesAsync(0));
    }

    [Fact]
    public async Task GetWorkOrderDocumentFoldersAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetWorkOrderDocumentFoldersAsync(0));
    }

    [Fact]
    public async Task GetBuildingAscendantsAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetBuildingAscendantsAsync(0));
    }

    [Fact]
    public async Task GetBuildingFloorsAsync_NonPositiveId_Throws()
    {
        (PythagorasClient client, _, _) = Build();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            async () => await client.GetBuildingFloorsAsync(0));
    }

    [Fact]
    public async Task GetBuildingDocumentListAsync_Success_BuildsUriAndDeserializes()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"data\":[{\"id\":1}],\"totalSize\":1}");

        UiListDataResponse<FileDocument> result = await client.GetBuildingDocumentListAsync(10, maxResults: 25);

        result.TotalSize.ShouldBe(1);
        result.Data.ShouldHaveSingleItem().Id.ShouldBe(1);
        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/10/documentfile/uilistdata?maxResults=25");
    }

    [Fact]
    public async Task GetBuildingDocumentListAsync_NoMaxResults_OmitsQuery()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"data\":[],\"totalSize\":0}");

        await client.GetBuildingDocumentListAsync(10);

        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/10/documentfile/uilistdata");
    }

    [Fact]
    public async Task GetDocumentListAsync_WithOrderingOptions_BuildsQuery()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"data\":[],\"totalSize\":0}");

        await client.GetDocumentListAsync(maxResults: 50, orderBy: "name", orderAsc: false);

        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/documentfile/uilistdata?maxResults=50&orderBy=name&orderAsc=false");
    }

    [Fact]
    public async Task GetDocumentListAsync_Defaults_OmitsOptionalQuery()
    {
        (PythagorasClient client, StubHandler handler, _) = Build(content: "{\"data\":[],\"totalSize\":0}");

        await client.GetDocumentListAsync();

        handler.LastRequest!.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/documentfile/uilistdata");
    }

    private sealed class MultiClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public List<string> RequestedNames { get; } = [];

        public HttpClient CreateClient(string name)
        {
            RequestedNames.Add(name);
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://example.org/")
            };
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(HttpStatusCode status, string content, string contentType = "application/json")
            : this(_ => new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, contentType)
            })
        {
        }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestContent = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return _responder(request);
        }
    }
}
