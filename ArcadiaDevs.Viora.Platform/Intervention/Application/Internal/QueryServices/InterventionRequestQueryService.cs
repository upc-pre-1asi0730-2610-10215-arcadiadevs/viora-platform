using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles <see cref="InterventionRequest" /> read queries (REQ-IREQ-2).
/// </summary>
public class InterventionRequestQueryService(IInterventionRequestRepository interventionRequestRepository)
    : IInterventionRequestQueryService
{
    public async Task<Result<InterventionRequest, Error>> Handle(
        GetInterventionRequestByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await interventionRequestRepository.FindByIdAsync(query.Id, cancellationToken);
            if (request is null || request.GrowerId != query.GrowerId)
            {
                // A non-owner sees the same NotFound as a genuinely missing request —
                // existence isn't leaked to callers who don't own it.
                return new Result<InterventionRequest, Error>.Failure(InterventionErrors.NotFound);
            }

            return new Result<InterventionRequest, Error>.Success(request);
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

    public async Task<IReadOnlyList<InterventionRequest>> Handle(
        ListInterventionRequestsByGrowerQuery query,
        CancellationToken cancellationToken = default)
    {
        return await interventionRequestRepository.ListByGrowerIdAsync(
            query.GrowerId, query.PlotId, cancellationToken);
    }
}
