using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;

/// <summary>
///     Handles <see cref="DynamicNutritionRecommendedEvent"/> as a log-and-exit stub.
///     Per design-decisions #28: logs the received event at Information level and returns Task.CompletedTask.
///     Does NOT create AlertTimelineRecord or call any service.
/// </summary>
public class DynamicNutritionRecommendedEventHandler(
    ILogger<DynamicNutritionRecommendedEventHandler> logger)
    : IEventHandler<DynamicNutritionRecommendedEvent>
{
    public Task Handle(DynamicNutritionRecommendedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Received DynamicNutritionRecommendedEvent for PlanId={PlanId}, PlotId={PlotId}, UserId={UserId}, TriggeringRiskLevel={TriggeringRiskLevel}",
            domainEvent.PlanId,
            domainEvent.PlotId,
            domainEvent.UserId,
            domainEvent.TriggeringRiskLevel);

        return Task.CompletedTask;
    }
}
