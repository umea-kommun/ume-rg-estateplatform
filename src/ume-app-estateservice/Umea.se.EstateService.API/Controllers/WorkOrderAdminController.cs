using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

/// <summary>
/// Service-to-service endpoints for monitoring and remediating permanently failed work orders,
/// consumed by the status dashboard. Protected by an API key (no user context), unlike the
/// user-scoped <see cref="WorkOrderController"/>.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.WorkOrders)]
[AuthorizeApiKey]
public class WorkOrderAdminController(IWorkOrderHandler workOrderHandler) : ControllerBase
{
    [HttpGet("failed/count")]
    [SwaggerOperation(Summary = "Count failed work orders", Description = "Number of permanently failed work orders awaiting admin remediation.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Failed work order count.", typeof(FailedWorkOrdersCountResponse))]
    public async Task<ActionResult<FailedWorkOrdersCountResponse>> GetFailedWorkOrdersCountAsync(CancellationToken cancellationToken)
    {
        int count = await workOrderHandler.GetFailedCountAsync(cancellationToken);

        return Ok(new FailedWorkOrdersCountResponse { Count = count });
    }

    [HttpGet("failed")]
    [SwaggerOperation(Summary = "List failed work orders", Description = "List permanently failed work orders with their error details for admin remediation.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Failed work orders.", typeof(IReadOnlyList<FailedWorkOrderModel>))]
    public async Task<ActionResult<IReadOnlyList<FailedWorkOrderModel>>> GetFailedWorkOrdersAsync(CancellationToken cancellationToken)
    {
        return Ok(await workOrderHandler.GetFailedWorkOrdersAsync(cancellationToken));
    }

    [HttpPost("failed/{id:guid}/retry")]
    [SwaggerOperation(Summary = "Retry failed work order", Description = "Re-queue a permanently failed work order for submission to Pythagoras.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Work order queued for retry.", typeof(FailedWorkOrderModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "WorkOrder not found.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "WorkOrder is not in a permanently failed state.")]
    public async Task<ActionResult<FailedWorkOrderModel>> RetryFailedWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await workOrderHandler.AdminRetryWorkOrderAsync(id, cancellationToken));
    }

    [HttpPost("failed/{id:guid}/dismiss")]
    [SwaggerOperation(Summary = "Dismiss failed work order", Description = "Mark a permanently failed work order as manually resolved; it will not be sent to Pythagoras.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Work order dismissed.", typeof(FailedWorkOrderModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "WorkOrder not found.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "WorkOrder is not in a permanently failed state.")]
    public async Task<ActionResult<FailedWorkOrderModel>> DismissFailedWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await workOrderHandler.AdminDismissWorkOrderAsync(id, cancellationToken));
    }
}
