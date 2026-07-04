namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for the composed producer-facing overview (REQ-OV-1) of every
///     <c>InterventionRequest</c> belonging to a grower, including the
///     downstream chain (proposal, prescription, execution, outcome) and a
///     derived <c>status</c> (REQ-OV-2).
/// </summary>
public record GetInterventionOverviewByGrowerIdQuery(int GrowerId);
