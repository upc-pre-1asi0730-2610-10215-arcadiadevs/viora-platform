namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
///     State-machine helpers for <see cref="EAlertSeverity"/>.
///     <see cref="RaiseOne"/> implements the severity escalation ladder
///     <c>LOW → MEDIUM → HIGH → CRITICAL</c>, capped at <c>CRITICAL</c>
///     (no overflow).
/// </summary>
public static class EAlertSeverityExtensions
{
    /// <summary>
    ///     Returns the next severity up the escalation ladder. Stays at
    ///     <see cref="EAlertSeverity.CRITICAL"/> when already at the top.
    /// </summary>
    public static EAlertSeverity RaiseOne(this EAlertSeverity severity) => severity switch
    {
        EAlertSeverity.LOW => EAlertSeverity.MEDIUM,
        EAlertSeverity.MEDIUM => EAlertSeverity.HIGH,
        EAlertSeverity.HIGH => EAlertSeverity.CRITICAL,
        EAlertSeverity.CRITICAL => EAlertSeverity.CRITICAL,
        _ => severity
    };
}
