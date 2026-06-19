using System;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

public class RecommendDynamicNutritionPlanCommandService(
    IDynamicNutritionPlanRepository dynamicNutritionPlanRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<RecommendDynamicNutritionPlanCommandService> logger) : IRecommendDynamicNutritionPlanCommandService
{
    public async Task<Result<DynamicNutritionPlan, Error>> Handle(RecommendDynamicNutritionCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var rationale = new PlanRationale($"Automated recommendation based on current plot conditions.", EClimateRiskLevel.Moderate, new NdviValue(0.5), 1.2);
            var applicationWindow = new NutritionApplicationWindow(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));
            var recommendations = new[]
            {
                new NutritionInputRecommendation("Nitrogen 120kg/ha", "Promote vegetative growth", 120, "kg/ha", ENutritionInputStatus.Recommended),
                new NutritionInputRecommendation("Phosphorus 60kg/ha", "Root development", 60, "kg/ha", ENutritionInputStatus.Recommended),
                new NutritionInputRecommendation("Potassium 90kg/ha", "Stress resistance", 90, "kg/ha", ENutritionInputStatus.Recommended)
            };

            var plan = DynamicNutritionPlan.Recommend(
                command.UserId,
                command.PlotId,
                recommendations,
                applicationWindow,
                rationale,
                DateTimeOffset.UtcNow);

            await dynamicNutritionPlanRepository.AddAsync(plan);
            await unitOfWork.CompleteAsync(cancellationToken);

            var domainEvent = new DynamicNutritionRecommendedEvent(
                plan.Id,
                plan.PlotId,
                plan.UserId,
                plan.Rationale.TriggeringRiskLevel.ToString()
            );

            await mediator.PublishAsync(domainEvent, cancellationToken);

            logger.LogInformation("Successfully generated Automated Dynamic Nutrition Plan {PlanId} for plot {PlotId}", plan.Id, command.PlotId);
            
            return new Result<DynamicNutritionPlan, Error>.Success(plan);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating automated Dynamic Nutrition Plan for plot {PlotId}", command.PlotId);
            return new Result<DynamicNutritionPlan, Error>.Failure(new Error("GENERATION_ERROR", "Failed to generate dynamic nutrition plan."));
        }
    }
}
