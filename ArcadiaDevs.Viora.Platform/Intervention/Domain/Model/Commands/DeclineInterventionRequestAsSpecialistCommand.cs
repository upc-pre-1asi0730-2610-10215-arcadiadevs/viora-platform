namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command for the assigned specialist to decline an incoming intervention
///     request. Distinct from the grower-side decline
///     (<see cref="DeclineInterventionRequestCommand" />): here the actor is
///     the specialist, ownership is enforced against
///     <see cref="Aggregates.InterventionRequest.SpecialistId" /> instead of
///     <c>GrowerId</c>, and reuses the same
///     <see cref="Aggregates.InterventionRequest.Decline" /> transition.
/// </summary>
/// <param name="Id">The intervention request id.</param>
/// <param name="DeclineReason">The reason for declining.</param>
/// <param name="SpecialistId">The authenticated caller's id, derived from the token — enforced as the assigned specialist.</param>
public record DeclineInterventionRequestAsSpecialistCommand(int Id, string DeclineReason, int SpecialistId);
