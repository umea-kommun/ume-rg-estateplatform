namespace Umea.se.EstateService.Shared.Exceptions;

public sealed class FeedbackException : EstateServiceException
{
    public FeedbackException(string message)
        : base(message)
    {
    }
}
