using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="InterventionOutcome" /> commands (REQ-IO-1..3).
/// </summary>
public class InterventionOutcomeCommandService(
    IInterventionOutcomeRepository interventionOutcomeRepository,
    IInterventionExecutionRepository interventionExecutionRepository,
    IUnitOfWork unitOfWork)
    : IInterventionOutcomeCommandService
{
    public async Task<Result<InterventionOutcome, Error>> Handle(
        ReportImpactCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var execution = await interventionExecutionRepository.FindByIdAsync(
                command.InterventionExecutionId, cancellationToken);
            if (execution is null)
            {
                return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.NotFound);
            }

            // REQ-IO-3 idempotency — one outcome per execution.
            var existing = await interventionOutcomeRepository.FindByInterventionExecutionIdAsync(
                command.InterventionExecutionId, cancellationToken);
            if (existing is not null)
            {
                return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.ConflictError);
            }

            var impactReport = new ImpactReport(
                command.GracePeriod ?? string.Empty,
                command.ObservedResult ?? string.Empty,
                command.ImpactLevel ?? string.Empty,
                command.ProducerAssessment ?? string.Empty);

            var outcome = new InterventionOutcome(command.InterventionExecutionId, impactReport);

            await interventionOutcomeRepository.AddAsync(outcome, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<InterventionOutcome, Error>.Success(outcome);
        }
        catch (ArgumentException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<InterventionOutcome, Error>> Handle(
        CloseOutcomeWithEvaluationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await interventionOutcomeRepository.FindByIdAsync(command.Id, cancellationToken);
            if (outcome is null)
            {
                return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.NotFound);
            }

            var evaluation = new ServiceEvaluation(
                command.ServiceResult ?? string.Empty,
                command.HireAgain,
                command.PrivateFeedback ?? string.Empty);

            var closeResult = outcome.CloseWithEvaluation(evaluation);
            if (closeResult is Result<Unit, Error>.Failure closeFailure)
            {
                return new Result<InterventionOutcome, Error>.Failure(closeFailure.Error);
            }

            interventionOutcomeRepository.Update(outcome);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<InterventionOutcome, Error>.Success(outcome);
        }
        catch (ArgumentException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<InterventionOutcome, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }
}
