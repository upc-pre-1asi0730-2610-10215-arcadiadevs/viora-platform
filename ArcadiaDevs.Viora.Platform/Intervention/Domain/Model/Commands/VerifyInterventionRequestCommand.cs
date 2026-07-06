namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command for the assigned specialist to verify (take on) an incoming
///     intervention request, moving it out of their pending inbox.
/// </summary>
/// <param name="Id">The intervention request id.</param>
/// <param name="SpecialistId">The authenticated caller's id, derived from the token — enforced as the assigned specialist.</param>
public record VerifyInterventionRequestCommand(int Id, int SpecialistId);
