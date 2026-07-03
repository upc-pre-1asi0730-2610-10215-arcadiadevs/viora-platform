using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="InterventionRequest" /> commands (REQ-IREQ-1,
///     REQ-IREQ-3). Every FK on <see cref="CreateInterventionRequestCommand" />
///     is validated through its owning bounded context's ACL before the
///     aggregate is constructed (REQ-CC-2: missing FK maps to 404).
/// </summary>
public class InterventionRequestCommandService(
    IInterventionRequestRepository interventionRequestRepository,
    ISpecialistRepository specialistRepository,
    IIamContextFacade iamContextFacade,
    IExternalAgronomicService externalAgronomicService,
    IExternalSurveillanceService externalSurveillanceService,
    IUnitOfWork unitOfWork)
    : IInterventionRequestCommandService
{
    public async Task<Result<InterventionRequest, Error>> Handle(
        CreateInterventionRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await iamContextFacade.ExistsUserAsync(command.GrowerId, cancellationToken))
            {
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            if (!await externalAgronomicService.PlotExistsAsync(command.PlotId, cancellationToken))
            {
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            var specialist = await specialistRepository.FindByIdAsync(command.SpecialistId, cancellationToken);
            if (specialist is null)
            {
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            if (command.AlertId is { } alertId &&
                !await externalSurveillanceService.AlertExistsAsync(alertId, cancellationToken))
            {
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            var request = new InterventionRequest(
                command.GrowerId,
                command.PlotId,
                command.SpecialistId,
                command.AlertId,
                command.Reason,
                command.Message);

            await interventionRequestRepository.AddAsync(request, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<InterventionRequest, Error>.Success(request);
        }
        catch (ArgumentException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    public async Task<Result<InterventionRequest, Error>> Handle(
        DeclineInterventionRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await interventionRequestRepository.FindByIdAsync(command.Id, cancellationToken);
            if (request is null)
            {
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            request.Decline(command.DeclineReason);

            interventionRequestRepository.Update(request);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<InterventionRequest, Error>.Success(request);
        }
        catch (ArgumentException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<InterventionRequest, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }
}
