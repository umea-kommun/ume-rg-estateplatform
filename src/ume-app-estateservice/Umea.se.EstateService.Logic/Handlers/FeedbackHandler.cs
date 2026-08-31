using System.Globalization;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Logging;

namespace Umea.se.EstateService.Logic.Handlers;

public class FeedbackHandler(ILogger<FeedbackHandler> logger)
{
    private const string EventName = "EstateFeedback";

    // Allowed feedback category names. Must be kept in sync with the frontend
    // (see ume-stapp-estateplatform/src/components/shared/RateFeedback.vue).
    private static readonly HashSet<string> ValidFeedbackCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "estateOrder",
        "estateFaultReport",
        "estatePortal",
    };

    public Task TrackFeedback(FeedbackModel feedback, bool requiresComment)
    {
        if (string.IsNullOrWhiteSpace(feedback.Category) || !ValidFeedbackCategories.Contains(feedback.Category))
        {
            throw new FeedbackException("Invalid or missing feedback category.");
        }

        if (feedback.Rating is < 1 or > 5)
        {
            throw new FeedbackException("FeedbackRequest rating must be between 1 and 5.");
        }

        if (requiresComment && string.IsNullOrWhiteSpace(feedback.Comment))
        {
            throw new FeedbackException("FeedbackRequest comment is required when submitting a comment.");
        }

        SendDataToAppInsights(feedback, requiresComment);
        return Task.CompletedTask;
    }

    private void SendDataToAppInsights(FeedbackModel feedback, bool requiresComment)
    {
        logger.LogCustomEvent(EventName, options =>
        {
            if (requiresComment)
            {
                options.WithProperty("Comment", feedback.Comment ?? string.Empty);
            }

            options.WithProperty("Category", feedback.Category);

            // Gender and age are only known when the token carries a parsable SSN. Leave them out
            // entirely when unknown, so they aren't averaged together with real values.
            if (feedback.LoggedInPersonGender != null)
            {
                options.WithProperty("Gender", feedback.LoggedInPersonGender);
            }

#pragma warning disable CS0618 // Measurements are legacy custom event attributes, not real OpenTelemetry metrics.
            options.WithMeasurement("Rating", feedback.Rating);

            if (feedback.LoggedInPersonAge != null)
            {
                options.WithMeasurement("Age", feedback.LoggedInPersonAge.Value);
            }
#pragma warning restore CS0618

            foreach (KeyValuePair<string, object> kv in feedback.AdditionalInfo)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                string key = "ExtraInfo_" + kv.Key;
                string value = kv.Value.ToString() ?? string.Empty;

                // Invariant culture on purpose: the values arrive as JSON, so "1.5" is a number
                // regardless of the host's culture.
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                {
#pragma warning disable CS0618 // See above.
                    options.WithMeasurement(key, number);
#pragma warning restore CS0618
                }
                else if (!string.IsNullOrEmpty(value))
                {
                    options.WithProperty(key, value);
                }
            }
        });
    }
}
