using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;

public interface IDynamicNutritionPlanCommandService
{
    Task<Result<bool, Error>> Handle(RecommendDynamicNutritionCommand command, CancellationToken cancellationToken = default);
}
