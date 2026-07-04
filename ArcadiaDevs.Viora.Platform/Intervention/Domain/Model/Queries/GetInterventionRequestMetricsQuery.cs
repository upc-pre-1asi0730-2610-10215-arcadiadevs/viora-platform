namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for aggregate <c>InterventionRequest</c> metrics (REQ-OV-3),
///     scoped by either <see cref="GrowerId" /> or <see cref="SpecialistId" />.
///     Exactly one is expected to be supplied — the controller validates this
///     before dispatching the query.
/// </summary>
public record GetInterventionRequestMetricsQuery(int? GrowerId, int? SpecialistId);
