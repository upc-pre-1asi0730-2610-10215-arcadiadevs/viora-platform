using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.EventHandlers;

/// <summary>
///     Surveillance-side handler for the cross-BC
///     <see cref="AgronomicChillDeficitIntegrationEvent"/> (AGRO-005 / A5).
///     <para>
///         Wraps the primitive
///         <see cref="AgronomicChillDeficitIntegrationEvent.PlotId"/> in the
///         BC-local <see cref="PlotId"/> value object (CC-1) and delegates
///         to <see cref="IAlertCommandService"/> with a
///         <c>CHILL_DEFICIT</c> <see cref="EThreatType"/>,
///         <c>HIGH</c> <see cref="EAlertSeverity"/>, and a templated
///         <c>RiskExplanation</c> that embeds the deficit gap and the
///         temperature anomaly sign.
///     </para>
///     <para>
///         The event is consumed on the in-process bus (Cortex.Mediator
///         <see cref="IEvent"/>; CC-2). The <c>CreateAlertCommand</c>'s
///         <c>AlertType</c> is the Surveillance BC's
///         <c>Surveillance.Domain.Model.ValueObjects.EThreatType.CHILL_DEFICIT</c>
///         (13-value enum) &mdash; NOT the Agronomic BC's local
///         <c>EThreatType</c> from PR-D1. The two coexist with different
///         namespaces; the fully-qualified name is used at every call
///         site that crosses BC boundaries (CC-11).
///     </para>
///     <para>
///         The <see cref="IMediator"/> dependency is held for
///         test-side cross-checking that the resulting
///         <c>CHILL_DEFICIT</c> alert does NOT re-publish an
///         <c>AlertGeneratedIntegrationEvent</c> (the existing
///         <c>PHENOLOGICAL_RISK</c> filter in
///         <c>AlertCommandService.Handle(CreateAlertCommand)</c> at line 50
///         is preserved).
///     </para>
///     <para>
///         Handler failures are logged at <c>Error</c> and swallowed so
///         the originating Agronomic transaction (or any future
///         <c>IHostedService</c> producer's tick) is never rolled back.
///         The event is best-effort; process restart loses in-flight
///         events (CC-2 constraint).
///     </para>
/// </summary>
public class AgronomicChillDeficitIntegrationEventHandler(
    IAlertCommandService alertCommandService,
    IMediator mediator,
    ILogger<AgronomicChillDeficitIntegrationEventHandler> logger)
    : IEventHandler<AgronomicChillDeficitIntegrationEvent>
{
    /// <summary>
    ///     Dispatch a <c>CHILL_DEFICIT</c> alert for the plot identified by
    ///     the event's <see cref="AgronomicChillDeficitIntegrationEvent.PlotId"/>.
    ///     <para>
    ///         Best-effort: any exception thrown by
    ///         <see cref="IAlertCommandService.Handle(CreateAlertCommand, CancellationToken)"/>
    ///         is logged at <c>Error</c> and swallowed (CC-2, CC-9).
    ///     </para>
    /// </summary>
    /// <param name="evt">
    ///     The <see cref="AgronomicChillDeficitIntegrationEvent"/> published
    ///     by the Agronomic BC (or the future <c>ChillDeficitMonitor</c>
    ///     <c>IHostedService</c>).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task Handle(
        AgronomicChillDeficitIntegrationEvent evt,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alertCommandService);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(evt);

        // Capture the plot id outside the try so the catch block can
        // log it even if the event payload itself triggered the
        // exception (defensive: a null evt is already guarded above).
        var plotId = evt.PlotId;

        try
        {
            // CC-1: wrap the primitive long in the BC-local PlotId VO
            // so the downstream command is type-safe. The numeric value
            // is the only cross-BC contract; the recipient re-binds
            // the id into its own domain semantics.
            var surveillancePlotId = new PlotId(plotId);

            // CC-11: fully-qualified reference to the Surveillance BC's
            // 13-value EThreatType (CHILL_DEFICIT lives in the existing
            // enum; the Agronomic BC's local 5-value EThreatType from
            // PR-D1 is a SEPARATE type).
            var alertType = EThreatType.CHILL_DEFICIT.ToString();
            var severity = EAlertSeverity.HIGH.ToString();

            var riskExplanation =
                $"Plot {plotId} accumulated {evt.CurrentChillAccumulation} chill portions " +
                $"against a target of {evt.TargetChill}; gap is " +
                $"{evt.TargetChill - evt.CurrentChillAccumulation}; " +
                $"temperature anomaly is {evt.TemperatureAnomaly:+0.0;-0.0} C.";

            var supportingData = new Dictionary<string, string>
            {
                ["Current chill accumulation"] = evt.CurrentChillAccumulation.ToString("F2"),
                ["Target for current stage"] = evt.TargetChill.ToString("F2"),
                ["Gap"] = (evt.TargetChill - evt.CurrentChillAccumulation).ToString("F2"),
                ["Temperature anomaly"] = evt.TemperatureAnomaly.ToString("+0.00;-0.00") + " C"
            };

            var command = new CreateAlertCommand(
                PlotId: surveillancePlotId.Value,
                AlertType: alertType,
                Severity: severity,
                Title: "Chill deficit warning",
                RiskExplanation: riskExplanation,
                Sources: new List<string> { "CLIMATE" },
                DataProviders: new List<string> { "AgroMonitoring", "Viora model" },
                SupportingData: supportingData
            );

            await alertCommandService.Handle(command, ct);
        }
        catch (Exception ex)
        {
            // Per CC-2: handler exceptions are logged and swallowed; the
            // originating event is on the in-process bus and has no DB
            // write to roll back. Best-effort; process restart loses
            // in-flight events (CC-2 constraint).
            logger.LogError(
                ex,
                "Failed to handle AgronomicChillDeficitIntegrationEvent for plot {PlotId}",
                plotId);
        }
    }
}
