namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents the current status of an alert.
/// </summary>
public enum EAlertStatus
{
    ACTIVE,
    SUGGEST,
    UNDER_REVIEW,
    RESOLVED,
    DISMISSED
}
