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
///     Handles <see cref="InterventionExecution" /> commands (REQ-IE-1..2).
/// </summary>
public class InterventionExecutionCommandService(
    IInterventionExecutionRepository interventionExecutionRepository,
    ITreatmentPrescriptionRepository treatmentPrescriptionRepository,
    IUnitOfWork unitOfWork)
    : IInterventionExecutionCommandService
{
    public async Task<Result<InterventionExecution, Error>> Handle(
        CertifyInterventionExecutionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prescription = await treatmentPrescriptionRepository.FindByIdAsync(
                command.TreatmentPrescriptionId, cancellationToken);
            if (prescription is null)
            {
                return new Result<InterventionExecution, Error>.Failure(InterventionErrors.NotFound);
            }

            // REQ-IE-1: deliberate improvement over OS parity — certification
            // requires the parent prescription to be PRESCRIBED. Enforced here
            // at the command-service level (design decision 3, obs #267), NOT
            // as a ctor invariant on InterventionExecution itself, since an
            // aggregate cannot reach into a sibling aggregate to validate
            // itself.
            if (prescription.Status != TreatmentPrescriptionStatus.PRESCRIBED)
            {
                return new Result<InterventionExecution, Error>.Failure(InterventionErrors.ConflictError);
            }

            // REQ-IE-2 idempotency — one execution per prescription.
            var existing = await interventionExecutionRepository.FindByTreatmentPrescriptionIdAsync(
                command.TreatmentPrescriptionId, cancellationToken);
            if (existing is not null)
            {
                return new Result<InterventionExecution, Error>.Failure(InterventionErrors.ConflictError);
            }

            var execution = new InterventionExecution(
                command.TreatmentPrescriptionId,
                command.ApplicationDate,
                command.AppliedArea,
                command.ExecutionStatus,
                command.FieldNote);

            await interventionExecutionRepository.AddAsync(execution, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<InterventionExecution, Error>.Success(execution);
        }
        catch (ArgumentException)
        {
            return new Result<InterventionExecution, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<InterventionExecution, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<InterventionExecution, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<InterventionExecution, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }
}
