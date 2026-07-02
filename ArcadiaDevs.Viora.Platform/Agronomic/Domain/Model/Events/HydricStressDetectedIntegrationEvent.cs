using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Cross-BC integration event published when soil moisture drops below
///     the critical threshold (20%) on an active IoT device. Consumed by
///     the Surveillance BC to create a WATER_STRESS alert (CC-1 primitive transport).
/// </summary>
public sealed record HydricStressDetectedIntegrationEvent(
    long PlotId,
    string SensorId,
    double CurrentMoisture,
    double Threshold
) : IEvent;
