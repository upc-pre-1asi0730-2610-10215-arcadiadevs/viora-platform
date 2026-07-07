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
    IServiceProposalRepository serviceProposalRepository,
    ITreatmentPrescriptionRepository treatmentPrescriptionRepository,
    IInterventionExecutionRepository interventionExecutionRepository,
    IInterventionOutcomeRepository interventionOutcomeRepository,
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
            var successRate = await SuccessRatePercentAsync(query.Id, cancellationToken);

            return new Result<SpecialistPublicProfile, Error>.Success(
                ToPublicProfile(profile, caseCount, distanceKm: null, successRate));
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
            var specialistProfile = await profileContextFacade.GetSpecialistProfileAsync(query.SpecialistId, cancellationToken);

            return new Result<SpecialistContact, Error>.Success(
                new SpecialistContact(
                    query.SpecialistId,
                    profileSummary.Email,
                    profileSummary.Phone,
                    specialist?.Whatsapp,
                    nameof(ProfileRole.Specialist),
                    specialistProfile?.PhotoUrl));
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
            var successRate = await SuccessRatePercentAsync(candidate.Profile.UserId, cancellationToken);
            results.Add(ToPublicProfile(candidate.Profile, caseCount, candidate.DistanceKm, successRate));
        }

        return results.AsReadOnly();
    }

    private async Task<int> CountAcceptedCasesAsync(int profileUserId, CancellationToken cancellationToken)
    {
        var accepted = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            profileUserId, InterventionStatus.ACCEPTED, cancellationToken);
        return accepted.Count;
    }

    /// <summary>
    ///     Service success rate for the specialist: the share of their closed
    ///     cases whose outcome resolved the threat, walking the real
    ///     accepted-proposal → prescription → execution → closed-outcome
    ///     chain (mirrors OS's <c>successRatePercent</c>). Returns
    ///     <c>null</c> when the specialist has no closed cases yet — never a
    ///     fabricated <c>0</c>.
    /// </summary>
    private async Task<double?> SuccessRatePercentAsync(int profileUserId, CancellationToken cancellationToken)
    {
        var proposals = await serviceProposalRepository.FindBySpecialistIdAsync(profileUserId, cancellationToken);

        var closed = 0;
        var resolved = 0;

        foreach (var proposal in proposals)
        {
            if (proposal.Status != ServiceProposalStatus.ACCEPTED)
            {
                continue;
            }

            var prescription = await treatmentPrescriptionRepository.FindByServiceProposalIdAsync(proposal.Id, cancellationToken);
            if (prescription is null)
            {
                continue;
            }

            var execution = await interventionExecutionRepository.FindByTreatmentPrescriptionIdAsync(prescription.Id, cancellationToken);
            if (execution is null)
            {
                continue;
            }

            var outcome = await interventionOutcomeRepository.FindByInterventionExecutionIdAsync(execution.Id, cancellationToken);
            if (outcome is null || outcome.Status != InterventionOutcomeStatus.CLOSED)
            {
                continue;
            }

            closed++;
            if (string.Equals(outcome.ServiceEvaluation?.ServiceResult, "RESOLVED", StringComparison.OrdinalIgnoreCase))
            {
                resolved++;
            }
        }

        if (closed == 0)
        {
            return null;
        }

        return Math.Round(resolved * 100.0 / closed, 1);
    }

    private static SpecialistPublicProfile ToPublicProfile(
        SpecialistProfileSummary profile,
        int caseCount,
        double? distanceKm,
        double? successRate)
    {
        var tags = string.IsNullOrWhiteSpace(profile.ServiceTags)
            ? Array.Empty<string>()
            : profile.ServiceTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new SpecialistPublicProfile(
            profile.UserId,
            profile.DisplayName,
            nameof(ProfileRole.Specialist),
            successRate,
            caseCount,
            distanceKm,
            tags,
            (profile.Availability ?? ESpecialistAvailability.AvailableThisWeek).ToString(),
            profile.PhotoUrl);
    }
}
