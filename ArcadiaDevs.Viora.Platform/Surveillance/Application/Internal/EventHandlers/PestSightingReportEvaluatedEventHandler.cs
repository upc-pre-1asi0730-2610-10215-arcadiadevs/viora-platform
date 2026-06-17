using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using Cortex.Mediator;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.EventHandlers;

public class PestSightingReportEvaluatedEventHandler(IAlertCommandService alertCommandService)
    : IEventHandler<PestSightingReportEvaluatedEvent>
{
    public async Task Handle(PestSightingReportEvaluatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!domainEvent.AlertConfirmed)
        {
            return;
        }

        var command = new CreateAlertCommand(
            PlotId: domainEvent.PlotId,
            AlertType: domainEvent.ProbableThreat,
            Severity: domainEvent.CalculatedRisk,
            Title: "Confirmed pest threat detected",
            RiskExplanation: "A manual report was evaluated and confirmed to be a significant threat for the plot. Immediate inspection is recommended.",
            Sources: ["MANUAL_REPORT"],
            DataProviders: ["Viora Manual Reporting"],
            SupportingData: new Dictionary<string, string> { { "Report ID", domainEvent.ReportId.ToString() } }
        );

        await alertCommandService.Handle(command, cancellationToken);
    }
}
