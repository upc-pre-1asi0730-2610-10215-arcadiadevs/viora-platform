namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The spray volume for an <see cref="AgrochemicalPrescription" /> (REQ-TP-3).
/// </summary>
public record SprayVolume
{
    public int Amount { get; }

    public string Unit { get; }

    public SprayVolume(int amount, string unit)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Spray volume amount must be non-negative.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Spray volume unit cannot be blank.", nameof(unit));
        }

        Amount = amount;
        Unit = unit;
    }
}
