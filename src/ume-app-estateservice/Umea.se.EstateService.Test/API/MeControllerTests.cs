using System.Net;
using System.Net.Http.Json;
using Umea.se.EstateService.API;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

public class MeControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;

    public MeControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);

        MockManager.SetupUser(user => user
            .WithName("Integration Tester")
            .WithEmail("test@example.com")
            .WithActualAuthorization());
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsIdentityFromToken()
    {
        CurrentUserResponse? result = await _client.GetFromJsonAsync<CurrentUserResponse>(ApiRoutes.Me);

        result.ShouldNotBeNull();
        result.Email.ShouldBe("test@example.com");
        result.FullName.ShouldBe("Integration Tester");
    }

    [Fact]
    public async Task GetCurrentUser_NoTypeGated_ReturnsAllWorkOrderTypes()
    {
        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Me);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        CurrentUserResponse? result = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        result.ShouldNotBeNull();
        result.Permissions.WorkOrderTypes.ShouldBe(Enum.GetValues<WorkOrderType>(), ignoreOrder: true);
    }
}
