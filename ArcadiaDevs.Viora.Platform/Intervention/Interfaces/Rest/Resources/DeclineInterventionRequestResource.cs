namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for partially updating an InterventionRequest
///     (REQ-IREQ-3). Currently the only supported target
///     <paramref name="Status" /> is <c>DECLINED</c> — matches the exposed
///     REST surface (REQ-CC-1); other status transitions are owned by
///     downstream aggregates (<c>ServiceProposal</c>, etc.), not this
///     resource.
/// </summary>
/// <param name="Status">The target status. Only <c>DECLINED</c> is supported.</param>
/// <param name="DeclineReason">Required when <see cref="Status" /> is <c>DECLINED</c>.</param>
public record DeclineInterventionRequestResource(string? Status, string? DeclineReason);
