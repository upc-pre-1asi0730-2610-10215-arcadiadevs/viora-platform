using System;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Cross-BC integration event published by the Agronomic bounded
///     context on the in-process bus (Cortex.Mediator <see cref="IEvent"/>)
///     when a chill-deficit condition is observed for a plot (AGRO-005 / A5).
///     <para>
///         <b>CC-1 primitive transport</b>: every id is transported as a
///         primitive <see cref="long"/>. The receiving handler MUST wrap
///         <see cref="PlotId"/> in its own BC-local value object
///         (<c>Surveillance.Domain.Model.ValueObjects.PlotId</c>); the
///         Agronomic BC's own <c>PlotId</c> VO is NOT the one to use at
///         the receiving call site. The numeric value is the only
///         contract; the recipient re-binds the id into its own domain
///         semantics.
///     </para>
///     <para>
///         Primitive transport, recipient must wrap PlotId in its own
///         BC-local VO (<c>Surveillance.Domain.Model.ValueObjects.PlotId</c>).
///     </para>
/// </summary>
/// <param name="PlotId">
///     The plot id (primitive) whose chill accumulation is below the target
///     threshold. Recipient wraps in its own
///     <c>Surveillance.Domain.Model.ValueObjects.PlotId</c> VO (CC-1).
/// </param>
/// <param name="CurrentChillAccumulation">
///     The plot's current chill accumulation, in chill portions (decimal
///     for cross-stack parity with the OS event shape; matches
///     <c>os-viora-platform</c>'s <c>currentChillAccumulation</c>).
/// </param>
/// <param name="TargetChill">
///     The target chill accumulation for the plot's current phenological
///     stage, in chill portions (matches <c>targetChill</c> in the OS).
/// </param>
/// <param name="TemperatureAnomaly">
///     The temperature anomaly (signed, in degrees Celsius) observed for
///     the plot at detection time. A positive value means warmer than the
///     reference; a negative value means cooler. The handler formats this
///     into the alert's <c>SupportingData</c> with an explicit sign.
/// </param>
/// <param name="DetectedAt">
///     UTC timestamp the chill-deficit condition was detected at. Useful
///     for downstream correlation and idempotency (not yet enforced).
/// </param>
/// <remarks>
///     <b>CC-1 recipient contract</b>: the event's <see cref="PlotId"/>
///     is a primitive <see cref="long"/>. The Surveillance handler
///     MUST wrap it in
///     <c>Surveillance.Domain.Model.ValueObjects.PlotId</c> before
///     invoking <c>IAlertCommandService.Handle(CreateAlertCommand)</c>.
///     Do not call <c>new PlotId(evt.PlotId).Value</c> with the Agronomic
///     BC's <c>PlotId</c> VO at the receiving call site &mdash; the two
///     BCs own different <c>PlotId</c> types and the receiving
///     type-safe call expects the BC-local one.
/// </remarks>
// TODO AGRONOMIC-EVENTS-CHILLDEFICIT: producer deferred to a future IHostedService
// phase (mirrors os-viora-platform's AgronomicStatisticIngestionScheduler). When the
// ChillDeficitMonitor : BackgroundService lands, the producer will call:
//   Task PublishChillDeficitAsync(
//       long plotId, decimal currentChill, decimal targetChill,
//       decimal temperatureAnomaly, CancellationToken ct);
// and instantiate this event from the snapshot data.
public record AgronomicChillDeficitIntegrationEvent(
    long PlotId,
    decimal CurrentChillAccumulation,
    decimal TargetChill,
    decimal TemperatureAnomaly,
    DateTimeOffset DetectedAt
) : IEvent;
