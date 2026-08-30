using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Mappers;
using Umea.se.EstateService.API.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.EstateService.API.Controllers;

/// <summary>
/// Feedback is fire-and-forget telemetry: a rejected submission is logged and swallowed so the
/// completion screens never fail on it. The caller always gets 200.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Feedback)]
[Authorize]
public class FeedbackController(
    FeedbackHandler feedbackHandler,
    UserToken userToken,
    ILogger<FeedbackController> logger) : ControllerBase
{
    /// <summary>
    /// Captures a rating for the specified feedback category.
    /// </summary>
    /// <param name="feedbackRequest">The feedback without comment submitted by the caller.</param>
    [HttpPost("rate")]
    [SwaggerOperation(Summary = "Submit feedback rating", Description = "Captures a rating for the specified feedback category and records it as an Application Insights custom event.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Feedback accepted.")]
    public async Task Rate([FromBody] FeedbackRequest feedbackRequest)
    {
        try
        {
            FeedbackModel feedback = FeedbackMapper.MapFeedbackToLogic(feedbackRequest, userToken.SsNo);
            await feedbackHandler.TrackFeedback(feedback, requiresComment: false);
        }
        catch (FeedbackException ex)
        {
            logger.LogWarning("Feedback rate request failed: {Message}", ex.Message);
        }

        return;
    }

    /// <summary>
    /// Captures a comment and rating for the specified feedback category.
    /// </summary>
    /// <param name="feedbackRequest">The feedback with comment submitted by the caller.</param>
    [HttpPost("comment")]
    [SwaggerOperation(Summary = "Submit feedback comment", Description = "Captures a comment and rating for the specified feedback category and records it as an Application Insights custom event.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Feedback accepted.")]
    public async Task Comment([FromBody] FeedbackRequest feedbackRequest)
    {
        try
        {
            FeedbackModel feedback = FeedbackMapper.MapFeedbackToLogic(feedbackRequest, userToken.SsNo);
            await feedbackHandler.TrackFeedback(feedback, requiresComment: true);
        }
        catch (FeedbackException ex)
        {
            logger.LogWarning("Feedback comment request failed: {Message}", ex.Message);
        }

        return;
    }
}
