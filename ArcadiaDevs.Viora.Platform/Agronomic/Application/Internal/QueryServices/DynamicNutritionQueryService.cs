using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of dynamic nutrition plan query service.
/// </summary>
public class DynamicNutritionQueryService : IDynamicNutritionQueryService
{
    private readonly IPlotRepository _plotRepository;

    public DynamicNutritionQueryService(IPlotRepository plotRepository)
    {
        _plotRepository = plotRepository;
    }

    public async Task<Result<DynamicNutritionPlanDto, Error>> Handle(
        GetDynamicNutritionPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        var plot = await _plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot == null)
        {
            return new Result<DynamicNutritionPlanDto, Error>.Failure(
                new Error("PLOT_NOT_FOUND", $"Plot {query.PlotId} not found."));
        }

        // Placeholder implementation: return active plan with NPK nutrients
        var dto = new DynamicNutritionPlanDto
        {
            PlotId = plot.Id,
            PlotName = plot.PlotName,
            PlanId = 1, // placeholder
            PlanName = "Active Nutrition Plan",
            Status = "active",
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
            EndDate = DateTimeOffset.UtcNow.AddDays(30),
            Nutrients = new List<NutrientDto>
            {
                new NutrientDto
                {
                    Name = "Nitrogen",
                    RequiredAmount = 100m,
                    CurrentAmount = 80m,
                    Unit = "kg/ha"
                },
                new NutrientDto
                {
                    Name = "Phosphorus",
                    RequiredAmount = 50m,
                    CurrentAmount = 45m,
                    Unit = "kg/ha"
                },
                new NutrientDto
                {
                    Name = "Potassium",
                    RequiredAmount = 80m,
                    CurrentAmount = 70m,
                    Unit = "kg/ha"
                }
            }
        };

        return new Result<DynamicNutritionPlanDto, Error>.Success(dto);
    }
}