namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to close an <see cref="Aggregates.InterventionOutcome" />
///     with the grower's service evaluation (REQ-IO-2). Self-guarded on the
///     aggregate — only succeeds if not already <c>CLOSED</c> (409
///     otherwise).
/// </summary>
public record CloseOutcomeWithEvaluationCommand(
    int Id,
    string? ServiceResult,
    bool HireAgain,
    string? PrivateFeedback);
