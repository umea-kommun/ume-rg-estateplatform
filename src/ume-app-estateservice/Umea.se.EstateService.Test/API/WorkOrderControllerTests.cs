using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.API;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.Test.API;

/// <summary>
/// HTTP-level integration tests for the workOrder endpoints.
/// Business logic validation is covered by WorkOrderHandlerTests.
/// These tests focus on: HTTP plumbing, form binding, auth, response shapes, file uploads.
/// </summary>
[Collection("DataStoreTests")]
public class WorkOrderControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;

    public WorkOrderControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        MockManager.SetupUser(user => user.WithEmail("test@example.com").WithActualAuthorization());
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
        SeedTestData();
        CleanupWorkOrdersTable();
    }

    private void SeedTestData()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 100, Name = "Test Building", PopularName = "TestB", WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService] }
            ],
            rooms:
            [
                new RoomEntity { Id = 200, Name = "Room A", PopularName = "A", BuildingId = 100 }
            ]);
    }

    private void CleanupWorkOrdersTable()
    {
        if (WebAppFactory.Services.GetService(typeof(IDbContextFactory<EstateDbContext>)) is not IDbContextFactory<EstateDbContext> factory)
        {
            return;
        }

        using EstateDbContext db = factory.CreateDbContext();
        db.WorkOrderFiles.ExecuteDelete();
        db.WorkOrders.ExecuteDelete();
    }

    [Fact]
    public async Task CreateAndGetWorkOrder_RoundTrip_ReturnsCorrectData()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent("+46 70 123 45 67"), "NotifierPhone");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.SyncStatus.ShouldBe("Pending");

        // GET detail by UID
        HttpResponseMessage detailResponse = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{created.Id}");
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDetailModel? detail = await detailResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(created.Id);
        detail.BuildingName.ShouldBe("Test Building");
        detail.RoomName.ShouldBe("Room A");
        detail.Location.ShouldBe("Indoor");
        detail.Description.ShouldBe("Broken window");

        // GET list
        HttpResponseMessage listResponse = await _client.GetAsync(ApiRoutes.WorkOrders);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<WorkOrderListItemModel>? workOrders = await listResponse.Content.ReadFromJsonAsync<List<WorkOrderListItemModel>>();
        workOrders.ShouldNotBeNull();
        workOrders.Count.ShouldBeGreaterThanOrEqualTo(1);
        workOrders.ShouldContain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task GetWorkOrderByUid_NotFound_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SyncWorkOrder_ReturnsOkWithDetail()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("Sync test"), "Description");
        content.Add(new StringContent("+46 70 123 45 67"), "NotifierPhone");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();

        HttpResponseMessage syncResponse = await _client.PostAsync($"{ApiRoutes.WorkOrders}/{created.Id}/sync", null);
        syncResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDetailModel? detail = await syncResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(created.Id);
        detail.Description.ShouldBe("Sync test");
    }

    [Fact]
    public async Task SyncWorkOrder_NotFound_Returns404()
    {
        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/{Guid.NewGuid()}/sync", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_WithFile_ReturnsCreatedAndFileCount()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("Photo attached"), "Description");
        content.Add(new StringContent("+46 70 123 45 67"), "NotifierPhone");

        ByteArrayContent fileContent = new([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, .. Encoding.UTF8.GetBytes("fake image data")]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "Files", "photo.jpg");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();

        HttpResponseMessage detailResponse = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{created.Id}");
        WorkOrderDetailModel? detail = await detailResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.FileCount.ShouldBe(1);
        detail.Files.Count.ShouldBe(1);
        detail.Files[0].FileName.ShouldBe("photo.jpg");
    }

    [Fact]
    public async Task CreateWorkOrder_MissingDescription_ReturnsCamelCaseFieldError()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        // Description intentionally omitted

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("description");
        problem.Errors["description"].ShouldContain("required");
    }

    [Fact]
    public async Task CreateWorkOrder_InvalidEmailAndPhone_ReturnsMultipleFieldErrors()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent("not-an-email"), "NotifierEmail");
        content.Add(new StringContent("!!!invalid!!!"), "NotifierPhone");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("notifierEmail");
        problem.Errors["notifierEmail"].ShouldContain("invalid_format");
        problem.Errors.ShouldContainKey("notifierPhone");
        problem.Errors["notifierPhone"].ShouldContain("invalid_format");
    }

    [Fact]
    public async Task CreateWorkOrder_WithValidNotifierPhone_Succeeds()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent("+46 70 123 45 67"), "NotifierPhone");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateWorkOrder_MissingNotifierPhone_ReturnsRequiredError()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        // NotifierPhone intentionally omitted

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("notifierPhone");
        problem.Errors["notifierPhone"].ShouldContain("required");
    }

    [Fact]
    public async Task CreateWorkOrder_DescriptionTooLong_ReturnsMaxLengthError()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent(new string('x', 2001)), "Description");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("description");
        problem.Errors["description"].ShouldContain("max_length");
    }

    [Fact]
    public async Task GetDefaults_NoHistory_ReturnsNullPhone()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/defaults");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDefaultsResponse? defaults = await response.Content.ReadFromJsonAsync<WorkOrderDefaultsResponse>();
        defaults.ShouldNotBeNull();
        defaults.NotifierPhone.ShouldBeNull();
    }

    [Fact]
    public async Task GetDefaults_AfterSubmissions_ReturnsMostRecentNotifierPhone()
    {
        await SubmitErrorReportWithPhone("+46 70 111 11 11");
        await SubmitErrorReportWithPhone("+46 70 222 22 22");

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/defaults");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDefaultsResponse? defaults = await response.Content.ReadFromJsonAsync<WorkOrderDefaultsResponse>();
        defaults.ShouldNotBeNull();
        defaults.NotifierPhone.ShouldBe("+46 70 222 22 22");
    }

    [Fact]
    public async Task GetDefaults_WhitespaceOnlyPhoneOnNewestReport_IsIgnored()
    {
        // The API rejects whitespace-only phones, but legacy rows (created before phone became
        // required) can hold them. A whitespace-only phone is not "a phone", so the real phone
        // on the older report should win rather than prefilling blanks.
        SeedWorkOrder("+46 70 111 11 11", createdAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        SeedWorkOrder("   ", createdAt: DateTimeOffset.UtcNow);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/defaults");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDefaultsResponse? defaults = await response.Content.ReadFromJsonAsync<WorkOrderDefaultsResponse>();
        defaults.ShouldNotBeNull();
        defaults.NotifierPhone.ShouldBe("+46 70 111 11 11");
    }

    private void SeedWorkOrder(string? notifierPhone, DateTimeOffset createdAt)
    {
        if (WebAppFactory.Services.GetService(typeof(IDbContextFactory<EstateDbContext>)) is not IDbContextFactory<EstateDbContext> factory)
        {
            return;
        }

        using EstateDbContext db = factory.CreateDbContext();
        db.WorkOrders.Add(new WorkOrderEntity
        {
            Uid = Guid.NewGuid(),
            BuildingId = 100,
            BuildingName = "Test Building",
            Description = "Seeded",
            WorkOrderTypeId = 1,
            CreatedByEmail = "test@example.com",
            NotifierPhone = notifierPhone,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
        db.SaveChanges();
    }

    private async Task SubmitErrorReportWithPhone(string phone)
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent(phone), "NotifierPhone");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetConfig_ReturnsFileUploadConfig()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/config");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderConfigResponse? config = await response.Content.ReadFromJsonAsync<WorkOrderConfigResponse>();
        config.ShouldNotBeNull();
        config.MaxFileCount.ShouldBeGreaterThan(0);
        config.MaxFileSizeBytes.ShouldBeGreaterThan(0);
        config.AllowedContentTypes.ShouldNotBeEmpty();
        config.AllowedContentTypes.ShouldContain("application/pdf");
    }

    // --- Admin: failed work orders (status dashboard) ---

    [Fact]
    public async Task GetFailedCount_ReturnsPermanentlyFailedCount()
    {
        SeedFailedWorkOrder();
        SeedFailedWorkOrder();
        SeedFailedWorkOrder(nextSyncAt: DateTimeOffset.UtcNow); // retry scheduled, not counted

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/failed/count");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        FailedWorkOrdersCountResponse? body = await response.Content.ReadFromJsonAsync<FailedWorkOrdersCountResponse>();
        body.ShouldNotBeNull();
        body.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetFailedList_ReturnsFailedOrders()
    {
        Guid uid = SeedFailedWorkOrder(errorMessage: "Pythagoras rejected");

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/failed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<FailedWorkOrderModel>? body = await response.Content.ReadFromJsonAsync<List<FailedWorkOrderModel>>();
        body.ShouldNotBeNull();
        body.Count.ShouldBe(1);
        body[0].Id.ShouldBe(uid);
        body[0].ErrorMessage.ShouldBe("Pythagoras rejected");
        body[0].SyncStatus.ShouldBe("Failed");
    }

    [Fact]
    public async Task AdminRetry_PermanentlyFailed_ReturnsOk()
    {
        Guid uid = SeedFailedWorkOrder();

        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/failed/{uid}/retry", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        FailedWorkOrderModel? body = await response.Content.ReadFromJsonAsync<FailedWorkOrderModel>();
        body.ShouldNotBeNull();
        body.SyncStatus.ShouldBe("Pending");
    }

    [Fact]
    public async Task AdminRetry_NotFound_Returns404()
    {
        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/failed/{Guid.NewGuid()}/retry", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminRetry_NotPermanentlyFailed_Returns409()
    {
        Guid uid = SeedFailedWorkOrder(nextSyncAt: DateTimeOffset.UtcNow); // retry scheduled

        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/failed/{uid}/retry", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdminDismiss_PermanentlyFailed_ReturnsOk()
    {
        Guid uid = SeedFailedWorkOrder();

        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/failed/{uid}/dismiss", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        FailedWorkOrderModel? body = await response.Content.ReadFromJsonAsync<FailedWorkOrderModel>();
        body.ShouldNotBeNull();
        body.SyncStatus.ShouldBe("Dismissed");
    }

    [Fact]
    public async Task AdminDismiss_NotPermanentlyFailed_Returns409()
    {
        Guid uid = SeedFailedWorkOrder(nextSyncAt: DateTimeOffset.UtcNow); // retry scheduled

        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/failed/{uid}/dismiss", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public void WorkOrderAdminController_IsProtectedByApiKey()
    {
        // The admin failed-workorder endpoints carry no user context and must be API-key protected.
        // Actual enforcement is mocked out in integration tests (ApiKeyAuthorizerMock always allows),
        // so guard the intent at the type level: the controller must be decorated with [AuthorizeApiKey].
        Attribute? attribute = Attribute.GetCustomAttribute(typeof(WorkOrderAdminController), typeof(AuthorizeApiKeyAttribute));

        attribute.ShouldNotBeNull();
    }

    private Guid SeedFailedWorkOrder(DateTimeOffset? nextSyncAt = null, string errorMessage = "boom")
    {
        Guid uid = Guid.NewGuid();
        if (WebAppFactory.Services.GetService(typeof(IDbContextFactory<EstateDbContext>)) is not IDbContextFactory<EstateDbContext> factory)
        {
            return uid;
        }

        using EstateDbContext db = factory.CreateDbContext();
        db.WorkOrders.Add(new WorkOrderEntity
        {
            Uid = uid,
            BuildingId = 100,
            BuildingName = "Test Building",
            Description = "Failed order",
            WorkOrderTypeId = 1,
            SyncStatus = WorkOrderSyncStatus.Failed,
            NextSyncAt = nextSyncAt,
            ErrorMessage = errorMessage,
            RetryCount = 3,
            CreatedByEmail = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();
        return uid;
    }
}
