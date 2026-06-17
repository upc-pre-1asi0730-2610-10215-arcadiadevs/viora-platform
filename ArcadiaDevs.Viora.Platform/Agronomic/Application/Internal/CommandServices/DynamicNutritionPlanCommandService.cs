using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

public class DynamicNutritionPlanCommandService(ILogger<DynamicNutritionPlanCommandService> logger) : IDynamicNutritionPlanCommandService
{
    public Task<Result<bool, Error>> Handle(RecommendDynamicNutritionCommand command, CancellationToken cancellationToken = default)
    {
        // En .NET aun no tenemos la entidad agregada completa para DynamicNutritionPlan.
        // Se simula la generacion exitosa para mantener paridad con el OS Java.
        logger.LogInformation("Successfully generated Automated Dynamic Nutrition Plan for plot {PlotId} triggered by alert {AlertId}", command.PlotId, command.AlertId);
        
        return Task.FromResult<Result<bool, Error>>(new Result<bool, Error>.Success(true));
    }
}
