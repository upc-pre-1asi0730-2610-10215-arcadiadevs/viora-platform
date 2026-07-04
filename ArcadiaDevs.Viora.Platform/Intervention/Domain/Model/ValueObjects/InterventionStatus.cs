namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The lifecycle status of an <see cref="Aggregates.InterventionRequest" />
///     (REQ-IREQ-4). <c>AWAITING_RESPONSE</c> is retained though no code
///     path sets it yet — kept for future use, per proposal.
/// </summary>
public enum InterventionStatus
{
    PENDING,
    AWAITING_RESPONSE,
    PROPOSAL_RECEIVED,
    ACCEPTED,
    DECLINED
}
