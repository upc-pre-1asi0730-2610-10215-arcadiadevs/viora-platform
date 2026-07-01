namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents the evaluation status of a pest sighting report.
/// </summary>
public enum EReportStatus
{
    /// <summary>
    /// Legacy binary model value, kept for back-compat with persisted reports.
    /// </summary>
    UNDER_REVIEW,
    /// <summary>
    /// Middle triage outcome: a real signal exists but is not yet corroborated.
    /// </summary>
    NEEDS_INSPECTION,
    /// <summary>
    /// Confirmed pest threat that requires immediate attention.
    /// </summary>
    CONFIRMED,
    /// <summary>
    /// Weak signal: recorded for community epidemiology, no alert raised.
    /// </summary>
    LOGGED
}
