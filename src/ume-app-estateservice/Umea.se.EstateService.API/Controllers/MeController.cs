using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.EstateService.API.Controllers;

/// <summary>
/// Endpoints about the calling user. Deliberately not feature-gated: the client asks
/// who it is talking to and what it may show, and must get an answer even when an
/// area's feature flag is off.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Me)]
[Authorize]
public class MeController(WorkOrderAccessPolicy workOrderAccessPolicy, UserToken userToken) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Get current user", Description = "Identity and permissions for the calling user, so the client can gate navigation without reading the token. The API remains the enforcement point.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Current user.", typeof(CurrentUserResponse))]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        return Ok(new CurrentUserResponse
        {
            Email = userToken.Email,
            FullName = userToken.FullName,
            Permissions = new CurrentUserPermissions
            {
                WorkOrderTypes = [.. Enum.GetValues<WorkOrderType>()
                    .Where(type => workOrderAccessPolicy.IsTypeAllowed(type, userToken.Groups))]
            }
        });
    }
}
