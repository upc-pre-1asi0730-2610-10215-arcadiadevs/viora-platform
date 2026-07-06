using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="ServiceProposal" /> commands (REQ-SP-1..3). Every
///     accept/reject transition side-effects the parent
///     <see cref="InterventionRequest" /> in the same unit of work.
/// </summary>
public class ServiceProposalCommandService(
    IServiceProposalRepository serviceProposalRepository,
    IInterventionRequestRepository interventionRequestRepository,
    IProfileContextFacade profileContextFacade,
    IUnitOfWork unitOfWork)
    : IServiceProposalCommandService
{
    private const string ProposalRejectedReason = "Service proposal rejected.";

    public async Task<Result<ServiceProposal, Error>> Handle(
        SubmitServiceProposalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await interventionRequestRepository.FindByIdAsync(
                command.InterventionRequestId, cancellationToken);
            if (request is null)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var specialistProfile = await profileContextFacade.GetSpecialistProfileAsync(command.SpecialistId, cancellationToken);
            if (specialistProfile is null)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var costEstimate = new CostEstimate(command.CostAmount, command.CostCurrency);

            var proposal = new ServiceProposal(
                command.InterventionRequestId,
                command.SpecialistId,
                command.ServiceTitle,
                command.DurationLabel,
                command.Scope,
                command.ProposedDate,
                costEstimate,
                command.ProposalDetails);

            await serviceProposalRepository.AddAsync(proposal, cancellationToken);

            request.MarkProposalReceived();
            interventionRequestRepository.Update(request);

            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<ServiceProposal, Error>.Success(proposal);
        }
        catch (ArgumentException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<ServiceProposal, Error>> Handle(
        AcceptServiceProposalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposal = await serviceProposalRepository.FindByIdAsync(command.Id, cancellationToken);
            if (proposal is null)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var request = await interventionRequestRepository.FindByIdAsync(
                proposal.InterventionRequestId, cancellationToken);
            if (request is null || request.GrowerId != command.GrowerId)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var acceptResult = proposal.Accept();
            if (acceptResult is Result<Unit, Error>.Failure acceptFailure)
            {
                return new Result<ServiceProposal, Error>.Failure(acceptFailure.Error);
            }

            request.MarkAccepted();

            serviceProposalRepository.Update(proposal);
            interventionRequestRepository.Update(request);

            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<ServiceProposal, Error>.Success(proposal);
        }
        catch (ArgumentException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<ServiceProposal, Error>> Handle(
        RejectServiceProposalCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposal = await serviceProposalRepository.FindByIdAsync(command.Id, cancellationToken);
            if (proposal is null)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var request = await interventionRequestRepository.FindByIdAsync(
                proposal.InterventionRequestId, cancellationToken);
            if (request is null || request.GrowerId != command.GrowerId)
            {
                return new Result<ServiceProposal, Error>.Failure(InterventionErrors.NotFound);
            }

            var rejectResult = proposal.Reject();
            if (rejectResult is Result<Unit, Error>.Failure rejectFailure)
            {
                return new Result<ServiceProposal, Error>.Failure(rejectFailure.Error);
            }

            // REQ-SP-3: terminal decline, no re-routing — reuses InterventionRequest's
            // existing (no-self-guard) Decline transition rather than adding a new one.
            request.Decline(ProposalRejectedReason);

            serviceProposalRepository.Update(proposal);
            interventionRequestRepository.Update(request);

            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<ServiceProposal, Error>.Success(proposal);
        }
        catch (ArgumentException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<ServiceProposal, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }
}
