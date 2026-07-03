namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to decline an <see cref="Aggregates.InterventionRequest" />
///     (REQ-IREQ-3). No self-guard against the current status — see
///     <see cref="Aggregates.InterventionRequest.Decline" />.
/// </summary>
public record DeclineInterventionRequestCommand(int Id, string DeclineReason);
