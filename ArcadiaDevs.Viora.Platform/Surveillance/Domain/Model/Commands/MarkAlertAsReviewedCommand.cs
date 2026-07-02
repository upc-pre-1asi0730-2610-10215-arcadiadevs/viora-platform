namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

public record MarkAlertAsReviewedCommand(long AlertId);

/// <summary>
///     SURV-003: confirm an alert from inspection.
///     Wraps <see cref="Surveillance.Domain.Model.Aggregates.Alert.ConfirmFromInspection"/>.
/// </summary>
public record ConfirmAlertCommand(long AlertId);

/// <summary>
///     SURV-003: dismiss an alert.
///     Wraps <see cref="Surveillance.Domain.Model.Aggregates.Alert.Dismiss"/>.
/// </summary>
/// <param name="AlertId">The alert id.</param>
/// <param name="Reason">Optional caller-supplied dismissal reason (REQ-5).</param>
public record DismissAlertCommand(long AlertId, string? Reason = null);

/// <summary>
///     SURV-003: escalate an alert's severity by one level.
///     Wraps <see cref="Surveillance.Domain.Model.Aggregates.Alert.Escalate"/>.
/// </summary>
public record EscalateAlertCommand(long AlertId);

/// <summary>
///     SURV-003: link a pest sighting report to an alert.
///     Wraps <see cref="Surveillance.Domain.Model.Aggregates.Alert.LinkReport"/>.
/// </summary>
public record LinkAlertReportCommand(long AlertId, long ReportId);

/// <summary>
///     REQ-4: resolve an alert.
///     Wraps <see cref="Surveillance.Domain.Model.Aggregates.Alert.Resolve"/>.
/// </summary>
public record ResolveAlertCommand(long AlertId);
