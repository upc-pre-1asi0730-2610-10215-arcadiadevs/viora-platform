using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;

/// <summary>
///     Handles <see cref="NutritionApplicationCertifiedEvent"/> as a log-and-exit stub.
///     Per design-decisions #28: logs the received event at Information level and returns Task.CompletedTask.
///     Does NOT call any service or create any record.
/// </summary>
public class NutritionApplicationCertifiedEventHandler(
    ILogger<NutritionApplicationCertifiedEventHandler> logger)
    : IEventHandler<NutritionApplicationCertifiedEvent>
{
    public Task Handle(NutritionApplicationCertifiedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Received NutritionApplicationCertifiedEvent for PlanId={PlanId}, PlotId={PlotId}, UserId={UserId}, ApplicationDate={ApplicationDate}",
            domainEvent.PlanId,
            domainEvent.PlotId,
            domainEvent.UserId,
            domainEvent.ApplicationDate);

        return Task.CompletedTask;
    }
}
