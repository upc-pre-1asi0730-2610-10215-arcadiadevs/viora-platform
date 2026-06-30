using System;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

/// <summary>
///     Cross-BC integration event published by the Surveillance bounded
///     context on the in-process bus (Cortex.Mediator <see cref="IEvent"/>)
///     when a new <c>Alert</c> is created (SURV-002).
///     <para>
///         <b>CC-1 primitive transport</b>: every id is transported as a
///         primitive <see cref="long"/>. The receiving handler MUST wrap
///         <see cref="PlotId"/> / <see cref="AlertId"/> in its own BC-local
///         value object (e.g. <c>Agronomic.Domain.Model.ValueObjects.PlotId</c>,
///         <c>Surveillance.Domain.Model.ValueObjects.PlotId</c>).
///     </para>
/// </summary>
/// <param name="PlotId">
///     The plot id (primitive) of the alert's plot. Recipient wraps in its
///     own <c>PlotId</c> VO (CC-1).
/// </param>
/// <param name="AlertId">
///     The new alert id (primitive). Recipient wraps in its own BC-local
///     <c>AlertId</c> VO if needed (CC-1).
/// </param>
/// <param name="ThreatType">
///     The alert's <c>EThreatType</c> as a string (e.g. <c>"PHENOLOGICAL_RISK"</c>).
///     The Agronomic handler filters on this and acts only when the value
///     equals <c>PHENOLOGICAL_RISK</c>.
/// </param>
/// <param name="GeneratedAt">
///     UTC timestamp the alert was created at. Useful for downstream
///     correlation and idempotency (not yet enforced).
/// </param>
public record AlertGeneratedIntegrationEvent(
    long PlotId,
    long AlertId,
    string ThreatType,
    DateTime GeneratedAt
) : IEvent;
