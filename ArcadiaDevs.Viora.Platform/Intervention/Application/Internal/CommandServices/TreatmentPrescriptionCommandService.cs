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
///     Handles <see cref="TreatmentPrescription" /> commands (REQ-TP-1..4).
/// </summary>
public class TreatmentPrescriptionCommandService(
    ITreatmentPrescriptionRepository treatmentPrescriptionRepository,
    IServiceProposalRepository serviceProposalRepository,
    IUnitOfWork unitOfWork)
    : ITreatmentPrescriptionCommandService
{
    public async Task<Result<TreatmentPrescription, Error>> Handle(
        CreateTreatmentPrescriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposal = await serviceProposalRepository.FindByIdAsync(
                command.ServiceProposalId, cancellationToken);
            if (proposal is null)
            {
                return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.NotFound);
            }

            // REQ-TP-4 idempotency — one prescription per proposal.
            var existing = await treatmentPrescriptionRepository.FindByServiceProposalIdAsync(
                command.ServiceProposalId, cancellationToken);
            if (existing is not null)
            {
                return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.ConflictError);
            }

            // REQ-TP-1: no guard against the proposal's status — OS parity,
            // intentional inherited absence, not hardened here.
            var prescription = new TreatmentPrescription(command.ServiceProposalId);

            await treatmentPrescriptionRepository.AddAsync(prescription, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<TreatmentPrescription, Error>.Success(prescription);
        }
        catch (ArgumentException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<TreatmentPrescription, Error>> Handle(
        LogFieldInspectionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prescription = await treatmentPrescriptionRepository.FindByIdAsync(command.Id, cancellationToken);
            if (prescription is null)
            {
                return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.NotFound);
            }

            var record = new FieldInspectionRecord(
                command.FindingType ?? string.Empty,
                command.IncidenceLevel ?? string.Empty,
                command.TechnicalDescription ?? string.Empty,
                command.RecordDate ?? default);

            var logResult = prescription.LogFieldInspection(record);
            if (logResult is Result<Unit, Error>.Failure logFailure)
            {
                return new Result<TreatmentPrescription, Error>.Failure(logFailure.Error);
            }

            treatmentPrescriptionRepository.Update(prescription);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<TreatmentPrescription, Error>.Success(prescription);
        }
        catch (ArgumentException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<TreatmentPrescription, Error>> Handle(
        PrescribeAgrochemicalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prescription = await treatmentPrescriptionRepository.FindByIdAsync(command.Id, cancellationToken);
            if (prescription is null)
            {
                return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.NotFound);
            }

            var agrochemicalPrescription = new AgrochemicalPrescription(
                command.ApplicationMethod ?? string.Empty,
                command.SprayVolume ?? string.Empty,
                command.PreHarvestInterval ?? string.Empty,
                command.AgronomistRecommendations ?? string.Empty,
                command.RequiredPPE ?? string.Empty,
                command.Products ?? Array.Empty<string>());

            var prescribeResult = prescription.PrescribeAgrochemical(agrochemicalPrescription);
            if (prescribeResult is Result<Unit, Error>.Failure prescribeFailure)
            {
                return new Result<TreatmentPrescription, Error>.Failure(prescribeFailure.Error);
            }

            treatmentPrescriptionRepository.Update(prescription);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<TreatmentPrescription, Error>.Success(prescription);
        }
        catch (ArgumentException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<TreatmentPrescription, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }
}
