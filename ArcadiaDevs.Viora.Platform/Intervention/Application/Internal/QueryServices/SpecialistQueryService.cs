using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Handles Specialist read queries (REQ-SPEC-1..3). Rebuilt for real
///     specialist matching: the public profile and candidates lists now
///     compose entirely from <c>Profile</c> (Role=Specialist) via
///     <see cref="IProfileContextFacade" />, keyed by <c>ProfileUserId</c> —
///     not from a stored <c>Specialist</c> business-fields catalog. The
///     <c>Specialist</c> aggregate/repository is only still consulted for
///     the Intervention-local <c>Whatsapp</c> contact channel, which has no
///     home on <c>Profile</c>.
/// </summary>
public class SpecialistQueryService(
    ISpecialistRepository specialistRepository,
    IInterventionRequestRepository interventionRequestRepository,
    IProfileContextFacade profileContextFacade,
    SpecialistMatchingPolicy matchingPolicy,
    ILogger<SpecialistQueryService> logger)
    : ISpecialistQueryService
{
    /// <inheritdoc />
    public async Task<Result<SpecialistPublicProfile, Error>> Handle(
        GetSpecialistByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await profileContextFacade.GetSpecialistProfileAsync(query.Id, cancellationToken);
            if (profile is null)
            {
                return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.NotFound);
            }

            var caseCount = await CountAcceptedCasesAsync(query.Id, cancellationToken);

            return new Result<SpecialistPublicProfile, Error>.Success(ToPublicProfile(profile, caseCount, distanceKm: null));
        }
        catch (OperationCanceledException)
        {
            return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<SpecialistPublicProfile, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     REQ-SPEC-2 requires <c>InterventionRequest(requestId).status ==
    ///     ACCEPTED &amp;&amp; InterventionRequest(requestId).specialistId ==
    ///     id</c>, where <c>id</c>/<c>specialistId</c> are now both the
    ///     specialist's Profile UserId (see the specialist-live-matching
    ///     change — <c>InterventionRequest.SpecialistId</c> and this query's
    ///     <c>SpecialistId</c> used to live in two different id spaces: the
    ///     former was always the Profile UserId, the latter used to be the
    ///     Specialist aggregate's own row id; unified onto Profile UserId
    ///     everywhere). The gating ALSO verifies that
    ///     <c>query.CallerUserId</c> owns the referenced request
    ///     (<c>request.GrowerId == CallerUserId</c>) — status+specialist-id
    ///     matching alone is not sufficient to authorize the caller.
    /// </remarks>
    public async Task<Result<SpecialistContact, Error>> Handle(
        GetSpecialistContactQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await interventionRequestRepository.FindByIdAsync(query.RequestId, cancellationToken);
            if (request is null ||
                request.Status != InterventionStatus.ACCEPTED ||
                request.SpecialistId != query.SpecialistId ||
                request.GrowerId != query.CallerUserId)
            {
                return new Result<SpecialistContact, Error>.Failure(InterventionErrors.ContactNotUnlocked);
            }

            var profileSummary = await profileContextFacade.GetProfileSummaryAsync(query.SpecialistId, cancellationToken);
            if (profileSummary is null)
            {
                logger.LogWarning(
                    "Specialist contact requested for ProfileUserId {ProfileUserId}, but no matching Profile was found.",
                    query.SpecialistId);
                return new Result<SpecialistContact, Error>.Failure(InterventionErrors.NotFound);
            }

            var specialist = await specialistRepository.FindByProfileUserIdAsync(query.SpecialistId, cancellationToken);

            return new Result<SpecialistContact, Error>.Success(
                new SpecialistContact(query.SpecialistId, profileSummary.Email, profileSummary.Phone, specialist?.Whatsapp));
        }
        catch (OperationCanceledException)
        {
            return new Result<SpecialistContact, Error>.Failure(InterventionErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<SpecialistContact, Error>.Failure(InterventionErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<SpecialistContact, Error>.Failure(InterventionErrors.InternalServerError);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Ranks every real specialist Profile via
    ///     <see cref="SpecialistMatchingPolicy" /> (geo/tag/availability-aware
    ///     when <c>alertId</c> is given), then trims to
    ///     <see cref="GetSpecialistCandidatesQuery.Limit" />. Unlike the
    ///     previous fixed-catalog design, every ranked entry is already
    ///     Profile-backed by construction — there is no "candidate whose
    ///     Profile can't be resolved" case to filter out anymore.
    /// </remarks>
    public async Task<IReadOnlyList<SpecialistPublicProfile>> Handle(
        GetSpecialistCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var ranked = await matchingPolicy.MatchSpecialistsForAlertAsync(query.AlertId, cancellationToken);

        var results = new List<SpecialistPublicProfile>(Math.Min(ranked.Count, query.Limit));
        foreach (var candidate in ranked.Take(query.Limit))
        {
            var caseCount = await CountAcceptedCasesAsync(candidate.Profile.UserId, cancellationToken);
            results.Add(ToPublicProfile(candidate.Profile, caseCount, candidate.DistanceKm));
        }

        return results.AsReadOnly();
    }

    private async Task<int> CountAcceptedCasesAsync(int profileUserId, CancellationToken cancellationToken)
    {
        var accepted = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            profileUserId, InterventionStatus.ACCEPTED, cancellationToken);
        return accepted.Count;
    }

    private static SpecialistPublicProfile ToPublicProfile(
        SpecialistProfileSummary profile,
        int caseCount,
        double? distanceKm)
    {
        var tags = string.IsNullOrWhiteSpace(profile.ServiceTags)
            ? Array.Empty<string>()
            : profile.ServiceTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new SpecialistPublicProfile(
            profile.UserId,
            profile.DisplayName,
            nameof(ProfileRole.Specialist),
            null, // SuccessRate: no closed-case derivation yet — deliberately null, not fabricated.
            caseCount,
            distanceKm,
            tags,
            (profile.Availability ?? ESpecialistAvailability.AvailableThisWeek).ToString());
    }
}
