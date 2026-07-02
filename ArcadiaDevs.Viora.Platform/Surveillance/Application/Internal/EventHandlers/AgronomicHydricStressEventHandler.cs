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
///     <see cref="HydricStressDetectedIntegrationEvent"/>.
///     <para>
///         Wraps the primitive <see cref="HydricStressDetectedIntegrationEvent.PlotId"/>
///         in the BC-local <see cref="PlotId"/> value object (CC-1) and delegates
///         to <see cref="IAlertCommandService"/> with a
///         <c>WATER_STRESS</c> <see cref="EThreatType"/>,
///         <c>HIGH</c> <see cref="EAlertSeverity"/>.
///     </para>
///     <para>
///         Handler failures are logged at <c>Error</c> and swallowed so the
///         originating producer is never rolled back (CC-2 best-effort).
///     </para>
/// </summary>
public class AgronomicHydricStressEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<AgronomicHydricStressEventHandler> logger)
    : IEventHandler<HydricStressDetectedIntegrationEvent>
{
    public async Task Handle(HydricStressDetectedIntegrationEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alertCommandService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(evt);

        var plotId = evt.PlotId;

        try
        {
            var surveillancePlotId = new PlotId(plotId);

            var alertType = EThreatType.WATER_STRESS.ToString();
            var severity = EAlertSeverity.HIGH.ToString();

            var riskExplanation =
                $"Soil moisture levels have dropped below critical thresholds. Immediate irrigation is recommended.";

            var supportingData = new Dictionary<string, string>
            {
                ["Sensor ID"] = evt.SensorId,
                ["Current Moisture"] = evt.CurrentMoisture.ToString("F2"),
                ["Threshold"] = evt.Threshold.ToString("F2")
            };

            var command = new CreateAlertCommand(
                PlotId: surveillancePlotId.Value,
                AlertType: alertType,
                Severity: severity,
                Title: "Hydric stress warning",
                RiskExplanation: riskExplanation,
                Sources: new List<string> { "IOT" },
                DataProviders: new List<string> { "Viora Sensors" },
                SupportingData: supportingData
            );

            await alertCommandService.Handle(command, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to handle HydricStressDetectedIntegrationEvent for plot {PlotId}",
                plotId);
        }
    }
}
