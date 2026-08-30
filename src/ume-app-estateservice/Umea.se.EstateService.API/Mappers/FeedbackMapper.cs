using Umea.se.EstateService.API.Requests;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.CommonModels;

namespace Umea.se.EstateService.API.Mappers;

internal static class FeedbackMapper
{
    public static FeedbackModel MapFeedbackToLogic(FeedbackRequest feedbackRequest, string? loggedInPersonSsNo)
    {
        string? loggedInPersonGender = null;
        int? loggedInPersonAge = null;

        if (loggedInPersonSsNo != null && Ssn.TryParse(loggedInPersonSsNo, out Ssn? validSsn) && validSsn is not null)
        {
            loggedInPersonGender = validSsn.GetSex().ToString();
            loggedInPersonAge = validSsn.GetAge();
        }

        return new()
        {
            Category = feedbackRequest.Category,
            Rating = feedbackRequest.Rating,
            AdditionalInfo = feedbackRequest.AdditionalInfo,
            Comment = feedbackRequest.Comment,
            LoggedInPersonAge = loggedInPersonAge,
            LoggedInPersonGender = loggedInPersonGender
        };
    }
}
