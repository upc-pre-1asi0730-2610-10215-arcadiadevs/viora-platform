using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.EventHandlers;

/// <summary>
///     Surveillance-side handler for the cross-BC
///     <see cref="NdviDroppedIntegrationEvent"/>.
///     <para>
///         Wraps the primitive <see cref="NdviDroppedIntegrationEvent.PlotId"/>
///         in the BC-local <see cref="PlotId"/> value object (CC-1) and delegates
///         to <see cref="IAlertCommandService"/> with a
///         <c>PHENOLOGICAL_RISK</c> <see cref="EThreatType"/>,
///         <c>MEDIUM</c> <see cref="EAlertSeverity"/>.
///     </para>
///     <para>
///         Handler failures are logged at <c>Error</c> and swallowed so the
///         originating producer is never rolled back (CC-2 best-effort).
///     </para>
/// </summary>
public class AgronomicNdviDroppedEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<AgronomicNdviDroppedEventHandler> logger)
    : IEventHandler<NdviDroppedIntegrationEvent>
{
    public async Task Handle(NdviDroppedIntegrationEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alertCommandService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(evt);

        var plotId = evt.PlotId;

        try
        {
            var surveillancePlotId = new PlotId(plotId);

            var alertType = EThreatType.PHENOLOGICAL_RISK.ToString();
            var severity = EAlertSeverity.MEDIUM.ToString();

            var riskExplanation =
                $"The plot's NDVI has dropped below the safe threshold, suggesting possible phenological risks or crop stress.";

            var supportingData = new Dictionary<string, string>
            {
                ["Current NDVI"] = evt.CurrentNdvi.ToString("F4"),
                ["Historical Average"] = evt.HistoricalAverage.ToString("F4")
            };

            var command = new CreateAlertCommand(
                PlotId: surveillancePlotId.Value,
                AlertType: alertType,
                Severity: severity,
                Title: "Low NDVI zone detected",
                RiskExplanation: riskExplanation,
                Sources: new List<string> { "SATELLITE" },
                DataProviders: new List<string> { "AgroMonitoring", "Viora model" },
                SupportingData: supportingData
            );

            await alertCommandService.Handle(command, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to handle NdviDroppedIntegrationEvent for plot {PlotId}",
                plotId);
        }
    }
}
