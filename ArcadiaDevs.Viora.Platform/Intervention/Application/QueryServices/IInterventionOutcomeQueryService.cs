using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles read queries for <see cref="InterventionOutcome" />.
/// </summary>
public interface IInterventionOutcomeQueryService
{
    Task<InterventionOutcome?> Handle(
        GetInterventionOutcomeByIdQuery query,
        CancellationToken cancellationToken = default);

    Task<InterventionOutcome?> Handle(
        GetInterventionOutcomeByInterventionExecutionIdQuery query,
        CancellationToken cancellationToken = default);
}
