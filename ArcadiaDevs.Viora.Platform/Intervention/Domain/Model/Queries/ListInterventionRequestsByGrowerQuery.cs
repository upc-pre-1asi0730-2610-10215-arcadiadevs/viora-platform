namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to list a grower's intervention requests, optionally narrowed
///     to a single plot (REQ-IREQ-2).
/// </summary>
public record ListInterventionRequestsByGrowerQuery(int GrowerId, long? PlotId);
