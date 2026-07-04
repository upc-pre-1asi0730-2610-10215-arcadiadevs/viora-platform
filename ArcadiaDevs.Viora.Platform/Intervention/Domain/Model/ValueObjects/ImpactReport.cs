namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object recording the observed impact of an
///     <see cref="Aggregates.InterventionExecution" /> (REQ-IO-1). Supplied
///     at outcome-creation time, transitioning the outcome to
///     <c>IMPACT_REPORTED</c>.
/// </summary>
public record ImpactReport
{
    public string GracePeriod { get; }

    public string ObservedResult { get; }

    public string ImpactLevel { get; }

    public string ProducerAssessment { get; }

    public ImpactReport(
        string gracePeriod,
        string observedResult,
        string impactLevel,
        string producerAssessment)
    {
        if (string.IsNullOrWhiteSpace(gracePeriod))
        {
            throw new ArgumentException("Grace period is required.", nameof(gracePeriod));
        }

        if (string.IsNullOrWhiteSpace(observedResult))
        {
            throw new ArgumentException("Observed result is required.", nameof(observedResult));
        }

        if (string.IsNullOrWhiteSpace(impactLevel))
        {
            throw new ArgumentException("Impact level is required.", nameof(impactLevel));
        }

        if (string.IsNullOrWhiteSpace(producerAssessment))
        {
            throw new ArgumentException("Producer assessment is required.", nameof(producerAssessment));
        }

        GracePeriod = gracePeriod;
        ObservedResult = observedResult;
        ImpactLevel = impactLevel;
        ProducerAssessment = producerAssessment;
    }
}
