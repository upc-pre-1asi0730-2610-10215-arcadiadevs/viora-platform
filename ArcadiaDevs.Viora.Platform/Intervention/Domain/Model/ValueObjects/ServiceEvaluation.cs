namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object recording the grower's evaluation of the
///     specialist's service upon closing an
///     <see cref="Aggregates.InterventionOutcome" /> (REQ-IO-2). Supplied
///     exactly once, transitioning the outcome from
///     <c>IMPACT_REPORTED</c> to <c>CLOSED</c>.
/// </summary>
public record ServiceEvaluation
{
    public string ServiceResult { get; }

    public bool HireAgain { get; }

    public string PrivateFeedback { get; }

    public ServiceEvaluation(
        string serviceResult,
        bool hireAgain,
        string privateFeedback)
    {
        if (string.IsNullOrWhiteSpace(serviceResult))
        {
            throw new ArgumentException("Service result is required.", nameof(serviceResult));
        }

        if (string.IsNullOrWhiteSpace(privateFeedback))
        {
            throw new ArgumentException("Private feedback is required.", nameof(privateFeedback));
        }

        ServiceResult = serviceResult;
        HireAgain = hireAgain;
        PrivateFeedback = privateFeedback;
    }
}
