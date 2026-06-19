using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class DynamicNutritionQueryService(
    IDynamicNutritionPlanRepository dynamicNutritionPlanRepository) : IDynamicNutritionQueryService
{
    public async Task<Result<DynamicNutritionPlanResource, Error>> Handle(GetDynamicNutritionPlanQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await dynamicNutritionPlanRepository.FindActiveByPlotIdAsync(query.PlotId, cancellationToken);
            if (plan == null)
            {
                return new Result<DynamicNutritionPlanResource, Error>.Failure(new Error("PLAN_NOT_FOUND", "No active nutrition plan found for this plot."));
            }

            var resource = ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform.DynamicNutritionPlanResourceFromEntityAssembler.ToResourceFromEntity(plan);

            return new Result<DynamicNutritionPlanResource, Error>.Success(resource);
        }
        catch (Exception ex)
        {
            return new Result<DynamicNutritionPlanResource, Error>.Failure(new Error("QUERY_ERROR", $"Failed to fetch dynamic nutrition plan: {ex.Message}"));
        }
    }
}