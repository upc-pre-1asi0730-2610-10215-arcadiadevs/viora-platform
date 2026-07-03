using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles Specialist read queries (REQ-SPEC-1..3), composing the
///     <c>Specialist</c> aggregate's business fields with identity fields
///     resolved live from <see cref="IProfileContextFacade" />.
/// </summary>
public class SpecialistQueryService(
    ISpecialistRepository specialistRepository,
    IProfileContextFacade profileContextFacade,
    SpecialistMatchingPolicy matchingPolicy)
    : ISpecialistQueryService
{
    /// <inheritdoc />
    public async Task<Result<SpecialistPublicProfile, Error>> Handle(
        GetSpecialistByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var specialist = await specialistRepository.FindByIdAsync(query.Id, cancellationToken);
        if (specialist is null)
        {
            return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.NotFound);
        }

        var profileSummary = await profileContextFacade.GetProfileSummaryAsync(specialist.ProfileUserId, cancellationToken);
        if (profileSummary is null)
        {
            return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.NotFound);
        }

        return new Result<SpecialistPublicProfile, Error>.Success(ToPublicProfile(specialist, profileSummary));
    }

    /// <inheritdoc />
    /// <remarks>
    ///     REQ-SPEC-2 requires <c>InterventionRequest(requestId).status ==
    ///     ACCEPTED &amp;&amp; InterventionRequest(requestId).specialistId ==
    ///     id</c>. The <c>InterventionRequest</c> aggregate does not exist
    ///     yet — it ships in WU3 of this 8-work-unit change (design
    ///     sequencing, obs #267). Until then NO request can ever be
    ///     ACCEPTED, so denying contact unconditionally is spec-correct for
    ///     the current codebase state (matches the "Contact denied on
    ///     unaccepted request" scenario), not a shortcut. This method MUST
    ///     be revisited in WU3 to inject the real
    ///     <c>IInterventionRequestRepository</c> lookup once that aggregate
    ///     exists.
    /// </remarks>
    public async Task<Result<SpecialistContact, Error>> Handle(
        GetSpecialistContactQuery query,
        CancellationToken cancellationToken = default)
    {
        var specialist = await specialistRepository.FindByIdAsync(query.SpecialistId, cancellationToken);
        if (specialist is null)
        {
            return new Result<SpecialistContact, Error>.Failure(InterventionErrors.NotFound);
        }

        // TODO(WU3): replace with a real InterventionRequest ACCEPTED + specialist-match check.
        return new Result<SpecialistContact, Error>.Failure(InterventionErrors.ContactNotUnlocked);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpecialistPublicProfile>> Handle(
        GetSpecialistCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var ranked = await matchingPolicy.MatchSpecialistsForAlertAsync(query.AlertId, query.Limit, cancellationToken);

        var results = new List<SpecialistPublicProfile>(ranked.Count);
        foreach (var specialist in ranked)
        {
            var profileSummary = await profileContextFacade.GetProfileSummaryAsync(specialist.ProfileUserId, cancellationToken);
            if (profileSummary is null)
            {
                continue;
            }

            results.Add(ToPublicProfile(specialist, profileSummary));
        }

        return results.AsReadOnly();
    }

    private static SpecialistPublicProfile ToPublicProfile(
        Domain.Model.Aggregates.Specialist specialist,
        ProfileSummary profileSummary)
    {
        return new SpecialistPublicProfile(
            specialist.Id,
            profileSummary.FullName,
            profileSummary.Role.ToString(),
            specialist.SuccessRate,
            specialist.CaseCount,
            specialist.DistanceKm,
            specialist.Tags.Items,
            specialist.Availability.ToString());
    }
}
