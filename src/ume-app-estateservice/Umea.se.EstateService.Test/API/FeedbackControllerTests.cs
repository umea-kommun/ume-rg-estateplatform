using System.Net;
using System.Net.Http.Json;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

[Collection("DataStoreTests")]
public class FeedbackControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private const string RateEndpoint = $"{ApiRoutes.Feedback}/rate";
    private const string CommentEndpoint = $"{ApiRoutes.Feedback}/comment";

    private readonly HttpClient _client;

    public FeedbackControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);

        MockManager.SetupUser(user => user.WithActualAuthorization());
    }

    [Fact]
    public async Task Rate_WithValidPayload()
    {
        object payload = new
        {
            category = "estateOrder",
            rating = 4,
            additionalInfo = new { source = "unit-test" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(RateEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Rate_MissingCategoryEmptyString(string? category)
    {
        object payload = new
        {
            category,
            rating = 3
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(RateEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rate_MissingCategoryNull()
    {
        object payload = new
        {
            category = (string?)null,
            rating = 3
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(RateEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task Rate_RatingOutsideAcceptedRange(int rating)
    {
        object payload = new
        {
            category = "estateOrder",
            rating
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(RateEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Comment_WithValidPayload()
    {
        object payload = new
        {
            category = "estateOrder",
            rating = 5,
            comment = "Detailed feedback"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(CommentEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Comment_WithoutCommentText()
    {
        object payload = new
        {
            category = "estateOrder",
            rating = 4,
            comment = string.Empty
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(CommentEndpoint, payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
