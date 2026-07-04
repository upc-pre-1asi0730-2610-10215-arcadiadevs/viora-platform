using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The InterventionOutcome aggregate root — records the observed impact
///     of an <see cref="InterventionExecution" /> and the grower's closing
///     evaluation of the specialist's service (REQ-IO-1..3). Fifth and last
///     link in the FK chain <c>InterventionRequest ← ServiceProposal ←
///     TreatmentPrescription ← InterventionExecution ← InterventionOutcome</c>
///     (REQ-CC-3).
/// </summary>
/// <remarks>
///     <see cref="InterventionExecutionId" /> is ctor-only immutable
///     (REQ-CC-3). <see cref="ImpactReport" /> is supplied at creation time
///     (REQ-IO-1, status starts at <c>IMPACT_REPORTED</c>).
///     <see cref="Status" /> advances through a single self-guarded
///     transition: <c>IMPACT_REPORTED</c> → <c>CLOSED</c> (
///     <see cref="CloseWithEvaluation" />) — returns
///     <see cref="Result{TValue, TError}" /> rather than throwing, mirroring
///     <c>ServiceProposal.Accept</c>/<c>Reject</c> and
///     <c>TreatmentPrescription.LogFieldInspection</c>/
///     <c>PrescribeAgrochemical</c>'s established self-guarded-transition
///     convention (WU4/WU5, obs #272). Re-closing an already-<c>CLOSED</c>
///     outcome fails with <see cref="InterventionErrors.ConflictError" />
///     (REQ-IO-2).
/// </remarks>
public class InterventionOutcome
{
    public int Id { get; }

    public int InterventionExecutionId { get; }

    public InterventionOutcomeStatus Status { get; private set; }

    public ImpactReport ImpactReport { get; }

    public ServiceEvaluation? ServiceEvaluation { get; private set; }

    private InterventionOutcome()
    {
        ImpactReport = null!;
    }

    public InterventionOutcome(int interventionExecutionId, ImpactReport impactReport)
    {
        if (interventionExecutionId <= 0)
        {
            throw new ArgumentException("Intervention execution ID must be positive.", nameof(interventionExecutionId));
        }

        if (impactReport is null)
        {
            throw new ArgumentException("Impact report is required.", nameof(impactReport));
        }

        InterventionExecutionId = interventionExecutionId;
        ImpactReport = impactReport;
        Status = InterventionOutcomeStatus.IMPACT_REPORTED;
    }

    /// <summary>
    ///     Closes the outcome with the grower's service evaluation (REQ-IO-2).
    ///     Self-guarded — fails if already <c>CLOSED</c>, otherwise
    ///     transitions to <c>CLOSED</c>.
    /// </summary>
    public Result<Unit, Error> CloseWithEvaluation(ServiceEvaluation evaluation)
    {
        if (Status == InterventionOutcomeStatus.CLOSED)
        {
            return new Result<Unit, Error>.Failure(InterventionErrors.ConflictError);
        }

        ServiceEvaluation = evaluation;
        Status = InterventionOutcomeStatus.CLOSED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
