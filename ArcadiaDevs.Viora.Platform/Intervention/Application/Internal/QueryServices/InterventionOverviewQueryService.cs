using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles the composed producer-facing overview read query (REQ-OV-1,
///     REQ-OV-2), delegating FK-chain traversal and status derivation to
///     <see cref="InterventionOverviewComposer" />.
/// </summary>
public class InterventionOverviewQueryService(
    IInterventionRequestRepository interventionRequestRepository,
    InterventionOverviewComposer composer)
    : IInterventionOverviewQueryService
{
    /// <remarks>
    ///     Mirrors <c>SpecialistQueryService.Handle(GetSpecialistCandidatesQuery)</c>'s
    ///     bare-list return shape — this handler does not participate in the
    ///     Result/ProblemDetails error contract (REQ-CC-2 is for
    ///     command-driven state changes and single-resource reads, not this
    ///     list read model); an empty list for a grower with no requests is
    ///     a valid, non-error outcome.
    /// </remarks>
    public async Task<IReadOnlyList<InterventionOverviewItem>> Handle(
        GetInterventionOverviewByGrowerIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var requests = await interventionRequestRepository.ListByGrowerIdAsync(
            query.GrowerId, null, cancellationToken);

        var items = new List<InterventionOverviewItem>(requests.Count);
        foreach (var request in requests)
        {
            var chain = await composer.ComposeAsync(request, cancellationToken);
            items.Add(ToOverviewItem(request, chain));
        }

        return items.AsReadOnly();
    }

    private static InterventionOverviewItem ToOverviewItem(InterventionRequest request, InterventionChainSnapshot chain)
    {
        return new InterventionOverviewItem(
            request.Id,
            request.GrowerId,
            request.PlotId,
            request.SpecialistId,
            request.Status.ToString(),
            chain.Proposal?.Id,
            chain.Proposal?.Status.ToString(),
            chain.Prescription?.Id,
            chain.Prescription?.Status.ToString(),
            chain.Execution?.Id,
            chain.Execution?.ExecutionStatus.ToString(),
            chain.Outcome?.Id,
            chain.Outcome?.Status.ToString(),
            chain.Status);
    }
}
