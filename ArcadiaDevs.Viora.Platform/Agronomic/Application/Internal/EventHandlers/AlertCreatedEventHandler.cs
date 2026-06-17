using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;

public class AlertCreatedEventHandler(
    IDynamicNutritionPlanCommandService dynamicNutritionPlanCommandService,
    IPlotRepository plotRepository,
    ILogger<AlertCreatedEventHandler> logger)
    : IEventHandler<AlertCreatedEvent>
{
    public async Task Handle(AlertCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.AlertType == "PHENOLOGICAL_RISK")
        {
            logger.LogInformation("Received AlertCreatedEvent for Phenological Risk on plot {PlotId}. Triggering Dynamic Nutrition Plan...", domainEvent.PlotId);

            var plot = await plotRepository.FindByIdAsync(Convert.ToInt32(domainEvent.PlotId), cancellationToken);

            if (plot != null)
            {
                var command = new RecommendDynamicNutritionCommand(plot.OwnerUserId, plot.Id, domainEvent.AlertId);
                var result = await dynamicNutritionPlanCommandService.Handle(command, cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("Successfully dispatched RecommendDynamicNutritionCommand for plot {PlotId}", domainEvent.PlotId);
                }
                else
                {
                    logger.LogError("Failed to generate automated Dynamic Nutrition Plan");
                }
            }
            else
            {
                logger.LogError("Could not find Plot {PlotId} to generate automated Dynamic Nutrition Plan", domainEvent.PlotId);
            }
        }
    }
}
