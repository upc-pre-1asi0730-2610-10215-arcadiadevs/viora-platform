using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Cross-BC integration event published when a dynamic nutrition plan
///     is generated post-commit. Consumed by the Surveillance BC to append
///     a timeline record to the triggering alert (CC-1 primitive transport).
/// </summary>
public sealed record DynamicNutritionPlanGeneratedIntegrationEvent(
    long PlanId,
    long PlotId,
    long? AlertId
) : IEvent;
