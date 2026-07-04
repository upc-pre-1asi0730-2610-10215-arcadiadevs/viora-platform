using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="InterventionOutcome" />.
/// </summary>
public interface IInterventionOutcomeCommandService
{
    /// <summary>
    ///     Reports the impact of an execution, creating a new outcome
    ///     (REQ-IO-1), validating <c>InterventionExecutionId</c> exists and
    ///     that no outcome already exists for it (REQ-IO-3 idempotency).
    /// </summary>
    Task<Result<InterventionOutcome, Error>> Handle(
        ReportImpactCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Closes the outcome with the grower's service evaluation
    ///     (REQ-IO-2, self-guarded).
    /// </summary>
    Task<Result<InterventionOutcome, Error>> Handle(
        CloseOutcomeWithEvaluationCommand command,
        CancellationToken cancellationToken = default);
}
