namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for an intervention request by id (REQ-IREQ-2).
/// </summary>
/// <param name="Id">The intervention request id.</param>
/// <param name="GrowerId">The authenticated caller's id, derived from the token — enforced as owner.</param>
public record GetInterventionRequestByIdQuery(int Id, int GrowerId);
