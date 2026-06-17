namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents the origin or source that generated the alert.
/// </summary>
public enum EAlertSource
{
    CLIMATE,
    MANUAL_REPORT,
    COMMUNITY,
    SATELLITE,
    IOT,
    SYSTEM
}
