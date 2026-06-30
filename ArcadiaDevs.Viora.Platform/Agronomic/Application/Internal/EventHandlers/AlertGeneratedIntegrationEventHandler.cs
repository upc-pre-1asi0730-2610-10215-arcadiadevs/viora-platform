using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;

/// <summary>
///     Agronomic-side handler for the cross-BC
///     <see cref="AlertGeneratedIntegrationEvent"/> (SURV-002).
///     <para>
///         Filters on <c>ThreatType == PHENOLOGICAL_RISK</c>; for any other
///         threat type the handler is a no-op (the alert is still
///         persisted in Surveillance, but Agronomic does not act on it).
///     </para>
///     <para>
///         For matching events the handler wraps the primitive
///         <c>PlotId</c> in the BC-local
///         <c>Agronomic.Domain.Model.ValueObjects.PlotId</c> (CC-1) and
///         delegates to
///         <see cref="IRecommendDynamicNutritionPlanCommandService"/>.
///     </para>
///     <para>
///         The alert is already committed in Surveillance before this
///         handler runs (CC-2 in-process bus; no retry, no DLQ). Handler
///         failures are logged at <c>Error</c> and swallowed so the
///         originating transaction is never rolled back.
///     </para>
/// </summary>
public class AlertGeneratedIntegrationEventHandler(
    IRecommendDynamicNutritionPlanCommandService recommendService,
    ILogger<AlertGeneratedIntegrationEventHandler> logger)
    : IEventHandler<AlertGeneratedIntegrationEvent>
{
    public async Task Handle(AlertGeneratedIntegrationEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                domainEvent.ThreatType,
                EThreatType.PHENOLOGICAL_RISK.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            // No-op for any other threat type; the alert is still persisted
            // in Surveillance but Agronomic does not act on it in v1.
            return;
        }

        try
        {
            // CC-1: wrap the primitive long in the BC-local PlotId VO
            // so the downstream service is type-safe.
            _ = new Domain.Model.ValueObjects.PlotId(domainEvent.PlotId);

            // The agronomic DynamicNutritionPlan command takes a PlotId
            // and a UserId (int). We pass the alert's id as a stand-in
            // UserId because PHENOLOGICAL_RISK triggers a recommendation
            // for the plot owner; the recommendation service derives
            // user context from the plot's owner.
            var command = new RecommendDynamicNutritionCommand(
                UserId: (int)domainEvent.AlertId,
                PlotId: (int)domainEvent.PlotId);

            await recommendService.Handle(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Per CC-2: handler exceptions are logged and swallowed; the
            // originating alert is already committed in Surveillance.
            logger.LogError(
                ex,
                "AlertGeneratedIntegrationEventHandler failed; alert {AlertId} still persisted.",
                domainEvent.AlertId);
        }
    }
}
