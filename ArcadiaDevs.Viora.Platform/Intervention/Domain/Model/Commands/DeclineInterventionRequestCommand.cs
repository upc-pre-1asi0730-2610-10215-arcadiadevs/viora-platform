namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to decline an <see cref="Aggregates.InterventionRequest" />
///     (REQ-IREQ-3). No self-guard against the current status — see
///     <see cref="Aggregates.InterventionRequest.Decline" />.
/// </summary>
/// <param name="Id">The intervention request id.</param>
/// <param name="DeclineReason">The reason for declining.</param>
/// <param name="GrowerId">The authenticated caller's id, derived from the token — enforced as owner.</param>
public record DeclineInterventionRequestCommand(int Id, string DeclineReason, int GrowerId);
