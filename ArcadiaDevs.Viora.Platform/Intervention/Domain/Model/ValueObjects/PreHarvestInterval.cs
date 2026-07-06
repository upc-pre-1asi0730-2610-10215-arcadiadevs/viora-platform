namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The pre-harvest safety interval for an <see cref="AgrochemicalPrescription" />
///     (REQ-TP-3).
/// </summary>
public record PreHarvestInterval
{
    public int Days { get; }

    public PreHarvestInterval(int days)
    {
        if (days < 0)
        {
            throw new ArgumentException("Pre-harvest interval days must be non-negative.", nameof(days));
        }

        Days = days;
    }
}
