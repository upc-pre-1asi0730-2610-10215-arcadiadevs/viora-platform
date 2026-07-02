using System;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.EventHandlers;

/// <summary>
///     Surveillance-side handler for the cross-BC
///     <see cref="DynamicNutritionPlanGeneratedIntegrationEvent"/>.
///     <para>
///         When the event carries a non-null <see cref="DynamicNutritionPlanGeneratedIntegrationEvent.AlertId"/>,
///         appends a timeline record to the triggering alert. When null,
///         the event is a no-op (manual plan generation without alert context).
///     </para>
///     <para>
///         Handler failures are logged at <c>Error</c> and swallowed so the
///         originating producer is never rolled back (CC-2 best-effort).
///     </para>
/// </summary>
public class DynamicNutritionPlanGeneratedEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<DynamicNutritionPlanGeneratedEventHandler> logger)
    : IEventHandler<DynamicNutritionPlanGeneratedIntegrationEvent>
{
    public async Task Handle(DynamicNutritionPlanGeneratedIntegrationEvent evt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(alertCommandService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(evt);

        try
        {
            if (evt.AlertId is null)
            {
                logger.LogDebug(
                    "DynamicNutritionPlanGeneratedIntegrationEvent for plan {PlanId} has no AlertId; skipping timeline record.",
                    evt.PlanId);
                return;
            }

            var command = AddAlertTimelineRecordCommand.Create(
                evt.AlertId.Value,
                "Plan",
                "Dynamic Nutrition Plan Generated",
                $"An automated Dynamic Nutrition Plan (ID: {evt.PlanId}) has been recommended to mitigate this risk.");

            await alertCommandService.Handle(command, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to handle DynamicNutritionPlanGeneratedIntegrationEvent for plan {PlanId}",
                evt.PlanId);
        }
    }
}
