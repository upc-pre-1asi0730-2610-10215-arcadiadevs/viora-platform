using System.Linq;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The ServiceProposal aggregate root — a specialist's response to an
///     <see cref="InterventionRequest" /> (REQ-SP-1..4). Second link in the
///     FK chain <c>InterventionRequest ← ServiceProposal ← ...</c>
///     (REQ-CC-3).
/// </summary>
/// <remarks>
///     Ctor-set fields are immutable post-creation (REQ-CC-3). Only
///     <see cref="Status" /> mutates, via the self-guarded
///     <see cref="Accept" />/<see cref="Reject" /> transitions (REQ-SP-2,
///     REQ-SP-3) — both only succeed from <c>PENDING</c>, mirroring
///     <c>Alert.ConfirmFromInspection</c>/<c>Alert.Dismiss</c>'s
///     <see cref="Result{TValue, TError}" />-returning self-guard style
///     rather than an exception-throwing one, since the failure here maps
///     directly to <see cref="InterventionErrors.ConflictError" /> (409)
///     without any intermediate translation.
/// </remarks>
public class ServiceProposal
{
    public int Id { get; }

    public int InterventionRequestId { get; }

    public int SpecialistId { get; }

    public string ServiceTitle { get; }

    public string DurationLabel { get; }

    public IReadOnlyList<string> Scope { get; }

    public DateOnly ProposedDate { get; }

    public CostEstimate CostEstimate { get; }

    public string ProposalDetails { get; }

    public ServiceProposalStatus Status { get; private set; }

    private ServiceProposal()
    {
        ServiceTitle = string.Empty;
        DurationLabel = string.Empty;
        Scope = Array.Empty<string>();
        ProposalDetails = string.Empty;
        CostEstimate = null!;
    }

    public ServiceProposal(
        int interventionRequestId,
        int specialistId,
        string serviceTitle,
        string durationLabel,
        IReadOnlyList<string> scope,
        DateOnly proposedDate,
        CostEstimate costEstimate,
        string proposalDetails)
    {
        if (interventionRequestId <= 0)
        {
            throw new ArgumentException("Intervention request ID must be positive.", nameof(interventionRequestId));
        }

        if (specialistId <= 0)
        {
            throw new ArgumentException("Specialist ID must be positive.", nameof(specialistId));
        }

        if (string.IsNullOrWhiteSpace(serviceTitle))
        {
            throw new ArgumentException("Service title is required.", nameof(serviceTitle));
        }

        if (string.IsNullOrWhiteSpace(durationLabel))
        {
            throw new ArgumentException("Duration label is required.", nameof(durationLabel));
        }

        if (scope is null || scope.Count == 0 || scope.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Scope is required and must not contain blank items.", nameof(scope));
        }

        if (costEstimate is null)
        {
            throw new ArgumentException("Cost estimate is required.", nameof(costEstimate));
        }

        if (string.IsNullOrWhiteSpace(proposalDetails))
        {
            throw new ArgumentException("Proposal details are required.", nameof(proposalDetails));
        }

        InterventionRequestId = interventionRequestId;
        SpecialistId = specialistId;
        ServiceTitle = serviceTitle;
        DurationLabel = durationLabel;
        Scope = scope;
        ProposedDate = proposedDate;
        CostEstimate = costEstimate;
        ProposalDetails = proposalDetails;
        Status = ServiceProposalStatus.PENDING;
    }

    /// <summary>
    ///     Accepts the proposal (REQ-SP-2). Self-guarded — only succeeds
    ///     from <c>PENDING</c>.
    /// </summary>
    public Result<Unit, Error> Accept()
    {
        if (Status != ServiceProposalStatus.PENDING)
        {
            return new Result<Unit, Error>.Failure(InterventionErrors.ConflictError);
        }

        Status = ServiceProposalStatus.ACCEPTED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Rejects the proposal (REQ-SP-3, terminal — no re-routing). Self-guarded —
    ///     only succeeds from <c>PENDING</c>.
    /// </summary>
    public Result<Unit, Error> Reject()
    {
        if (Status != ServiceProposalStatus.PENDING)
        {
            return new Result<Unit, Error>.Failure(InterventionErrors.ConflictError);
        }

        Status = ServiceProposalStatus.REJECTED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
