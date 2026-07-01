using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

/// <summary>
///     Domain event published by <c>PestSightingCommandService.Handle(CreatePestSightingReportCommand)</c>
///     after a pest sighting report has been evaluated. The <see cref="Status"/> field is the new
///     tri-state triage signal (mirrors the OS's <c>String status</c>) that drives the 3-way
///     switch in <c>PestSightingReportEvaluatedEventHandler</c>.
/// </summary>
/// <param name="ReportId">The pest sighting report id.</param>
/// <param name="PlotId">The affected plot id.</param>
/// <param name="ReporterUserId">The user id of the reporter.</param>
/// <param name="CalculatedRisk">The computed risk severity (EAlertSeverity as string).</param>
/// <param name="ProbableThreat">The inferred threat type (EThreatType as string).</param>
/// <param name="AlertConfirmed">Legacy boolean: preserved for back-compat with persisted events.</param>
/// <param name="Status">The triage status (EReportStatus as string): CONFIRMED | NEEDS_INSPECTION | LOGGED | UNDER_REVIEW.</param>
public record PestSightingReportEvaluatedEvent(
    long ReportId,
    long PlotId,
    long ReporterUserId,
    string CalculatedRisk,
    string ProbableThreat,
    bool AlertConfirmed,
    string Status
) : IEvent;
