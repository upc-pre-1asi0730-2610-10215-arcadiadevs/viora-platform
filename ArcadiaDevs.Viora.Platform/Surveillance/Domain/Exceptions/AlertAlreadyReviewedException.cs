namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Exceptions;

public class AlertAlreadyReviewedException : Exception
{
    public AlertAlreadyReviewedException(long alertId) 
        : base($"Alert {alertId} is already under review, resolved or dismissed.")
    {
    }
}
