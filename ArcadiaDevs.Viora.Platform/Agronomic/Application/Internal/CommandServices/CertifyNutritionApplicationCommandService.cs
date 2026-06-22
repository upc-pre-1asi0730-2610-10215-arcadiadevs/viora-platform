using System;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
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

public class CertifyNutritionApplicationCommandService(
    IDynamicNutritionPlanRepository dynamicNutritionPlanRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<CertifyNutritionApplicationCommandService> logger) : ICertifyNutritionApplicationCommandService
{
    public async Task<Result<DynamicNutritionPlan, Error>> Handle(CertifyNutritionApplicationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await dynamicNutritionPlanRepository.FindByIdAsync((int)command.PlanId, cancellationToken);
            if (plan == null)
            {
                return new Result<DynamicNutritionPlan, Error>.Failure(new Error("PLAN_NOT_FOUND", $"Plan {command.PlanId} not found."));
            }

            var application = new NutritionApplication(
                command.ApplicationDate,
                command.ApplicationTime,
                command.AppliedInputs,
                EDoseConfirmationExtensions.FromString(command.DoseConfirmation),
                command.FieldOperator,
                command.FieldNotes);

            plan.CertifyApplication(application);
            dynamicNutritionPlanRepository.Update(plan);
            await unitOfWork.CompleteAsync(cancellationToken);

            var domainEvent = new NutritionApplicationCertifiedEvent(
                plan.Id,
                plan.PlotId,
                command.UserId,
                application.ApplicationDate
            );

            await mediator.PublishAsync(domainEvent, cancellationToken);

            logger.LogInformation("Successfully certified Dynamic Nutrition Plan {PlanId} by user {UserId}", command.PlanId, command.UserId);
            
            return new Result<DynamicNutritionPlan, Error>.Success(plan);
        }
        catch (InvalidOperationException ex)
        {
            return new Result<DynamicNutritionPlan, Error>.Failure(new Error("INVALID_STATE", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return new Result<DynamicNutritionPlan, Error>.Failure(new Error("INVALID_INPUT", ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error certifying Dynamic Nutrition Plan {PlanId}", command.PlanId);
            return new Result<DynamicNutritionPlan, Error>.Failure(new Error("CERTIFICATION_ERROR", "Failed to certify dynamic nutrition plan."));
        }
    }
}
