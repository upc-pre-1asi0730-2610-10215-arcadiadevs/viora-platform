using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.EventHandlers;

/// <summary>
///     Handles <see cref="PestSightingReportEvaluatedEvent"/> with a 3-way switch on
///     the new <c>Status</c> tri-state field (mirrors the OS's
///     <c>PestSightingReportEvaluatedEventHandler.on(...)</c>). Auto-registered via
///     <c>AddCortexMediator([typeof(Program)])</c> at <c>Program.cs:264</c>.
///     <list type="bullet">
///         <item><c>Status == "CONFIRMED"</c> raises a confirmed alert (title "Confirmed pest threat detected").</item>
///         <item><c>Status == "NEEDS_INSPECTION"</c> raises an inspection alert (title "Field inspection recommended").</item>
///         <item>Any other status (including <c>"LOGGED"</c>, <c>"UNDER_REVIEW"</c>, or unknown) logs at Information level and creates no alert.</item>
///     </list>
/// </summary>
public class PestSightingReportEvaluatedEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<PestSightingReportEvaluatedEventHandler> logger)
    : IEventHandler<PestSightingReportEvaluatedEvent>
{
    private const string TitleConfirmed = "Confirmed pest threat detected";
    private const string TitleInspection = "Field inspection recommended";
    private const string ExplanationConfirmed =
        "A manual report was evaluated and confirmed to be a significant threat for the plot. Immediate inspection is recommended.";
    private const string ExplanationInspection =
        "A manual report shows symptoms that are not yet corroborated by satellite data. A field inspection is recommended to confirm or rule out a threat.";
    private const string SourceManualReport = "MANUAL_REPORT";
    private const string DataProviderManualReporting = "Viora Manual Reporting";

    public async Task Handle(PestSightingReportEvaluatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        switch (domainEvent.Status)
        {
            case "CONFIRMED":
                await RaiseAsync(domainEvent, BuildConfirmedAlert(domainEvent), cancellationToken);
                break;
            case "NEEDS_INSPECTION":
                await RaiseAsync(domainEvent, BuildInspectionAlert(domainEvent), cancellationToken);
                break;
            default:
                logger.LogInformation(
                    "Pest sighting report {ReportId} triaged as {Status}. No alert created.",
                    domainEvent.ReportId, domainEvent.Status);
                break;
        }
    }

    private static CreateAlertCommand BuildConfirmedAlert(PestSightingReportEvaluatedEvent e) =>
        new(
            PlotId: e.PlotId,
            AlertType: e.ProbableThreat,
            Severity: e.CalculatedRisk,
            Title: TitleConfirmed,
            RiskExplanation: ExplanationConfirmed,
            Sources: [SourceManualReport],
            DataProviders: [DataProviderManualReporting],
            SupportingData: new Dictionary<string, string> { { "Report ID", e.ReportId.ToString() } });

    private static CreateAlertCommand BuildInspectionAlert(PestSightingReportEvaluatedEvent e) =>
        new(
            PlotId: e.PlotId,
            AlertType: e.ProbableThreat,
            Severity: e.CalculatedRisk,
            Title: TitleInspection,
            RiskExplanation: ExplanationInspection,
            Sources: [SourceManualReport],
            DataProviders: [DataProviderManualReporting],
            SupportingData: new Dictionary<string, string> { { "Report ID", e.ReportId.ToString() } });

    private async Task RaiseAsync(PestSightingReportEvaluatedEvent e, CreateAlertCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Pest sighting report {ReportId} triaged as {Status}. Triggering alert creation.",
            e.ReportId, e.Status);

        var result = await alertCommandService.Handle(command, cancellationToken);

        if (result is Result<Alert, Error>.Failure failure)
        {
            logger.LogWarning(
                "Failed to create alert for plot {PlotId}: {Message}",
                e.PlotId, failure.Error.Message);
        }
    }
}
