using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Cross-BC integration event published when a significant NDVI drop
///     is detected for a plot. Consumed by the Surveillance BC to create
///     a PHENOLOGICAL_RISK alert (CC-1 primitive transport).
/// </summary>
public sealed record NdviDroppedIntegrationEvent(
    long PlotId,
    double CurrentNdvi,
    double HistoricalAverage
) : IEvent;
