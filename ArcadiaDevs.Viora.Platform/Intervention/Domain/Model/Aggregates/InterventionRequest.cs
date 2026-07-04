using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The InterventionRequest aggregate root — a grower's request for a
///     specialist's intervention on a plot, optionally originating from an
///     Alert (REQ-IREQ-1). Root of the FK chain
///     <c>InterventionRequest ← ServiceProposal ← TreatmentPrescription ←
///     InterventionExecution ← InterventionOutcome</c> (REQ-CC-3).
/// </summary>
/// <remarks>
///     Ctor-set FK fields (<see cref="GrowerId" />, <see cref="PlotId" />,
///     <see cref="SpecialistId" />, <see cref="AlertId" />) are immutable
///     post-creation (REQ-CC-3). <see cref="Status" /> and
///     <see cref="DeclineReason" /> mutate via <see cref="Decline" /> (WU3),
///     and via <see cref="MarkProposalReceived" />/<see cref="MarkAccepted" />
///     (WU4, <c>ServiceProposal</c> side-effects) — none of these are
///     self-guarded against the current status, per REQ-IREQ-3's
///     established no-guard convention for this aggregate.
/// </remarks>
public class InterventionRequest
{
    public int Id { get; }

    public int GrowerId { get; }

    public long PlotId { get; }

    public int SpecialistId { get; }

    /// <summary>
    ///     Optional originating alert (REQ-IREQ-1) — a request may be
    ///     raised independently of an alert.
    /// </summary>
    public long? AlertId { get; }

    public string Reason { get; }

    public string Message { get; }

    public InterventionStatus Status { get; private set; }

    public string? DeclineReason { get; private set; }

    private InterventionRequest()
    {
        Reason = string.Empty;
        Message = string.Empty;
    }

    public InterventionRequest(
        int growerId,
        long plotId,
        int specialistId,
        long? alertId,
        string reason,
        string message)
    {
        if (growerId <= 0)
        {
            throw new ArgumentException("Grower ID must be positive.", nameof(growerId));
        }

        if (plotId <= 0)
        {
            throw new ArgumentException("Plot ID must be positive.", nameof(plotId));
        }

        if (specialistId <= 0)
        {
            throw new ArgumentException("Specialist ID must be positive.", nameof(specialistId));
        }

        if (alertId is <= 0)
        {
            throw new ArgumentException("Alert ID must be positive when provided.", nameof(alertId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        GrowerId = growerId;
        PlotId = plotId;
        SpecialistId = specialistId;
        AlertId = alertId;
        Reason = reason;
        Message = message;
        Status = InterventionStatus.PENDING;
    }

    /// <summary>
    ///     Declines the request (REQ-IREQ-3). Per OS parity, this is
    ///     intentionally NOT self-guarded against the current status — it
    ///     succeeds from any status, including terminal or downstream
    ///     ones. This is an inherited behavior, not an oversight (see spec
    ///     REQ-IREQ-3's "parity boundary" scenario).
    /// </summary>
    public void Decline(string declineReason)
    {
        if (string.IsNullOrWhiteSpace(declineReason))
        {
            throw new ArgumentException("Decline reason is required.", nameof(declineReason));
        }

        Status = InterventionStatus.DECLINED;
        DeclineReason = declineReason;
    }

    /// <summary>
    ///     Marks the request as having received a service proposal
    ///     (REQ-SP-1 side-effect of <c>SubmitServiceProposalCommand</c>).
    ///     No self-guard, mirrors <see cref="Decline" />'s no-guard
    ///     convention — WU4 (<c>ServiceProposal</c>) owns this transition.
    /// </summary>
    public void MarkProposalReceived()
    {
        Status = InterventionStatus.PROPOSAL_RECEIVED;
    }

    /// <summary>
    ///     Marks the request as accepted (REQ-SP-2 side-effect of
    ///     <c>AcceptServiceProposalCommand</c>). No self-guard, mirrors
    ///     <see cref="Decline" />'s no-guard convention.
    /// </summary>
    public void MarkAccepted()
    {
        Status = InterventionStatus.ACCEPTED;
    }
}
