namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The dosage of a single <see cref="PrescribedProduct" /> (REQ-TP-3).
/// </summary>
public record Dosage
{
    public double Amount { get; }

    public string Unit { get; }

    public Dosage(double amount, string unit)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Dosage amount must be positive.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Dosage unit cannot be blank.", nameof(unit));
        }

        Amount = amount;
        Unit = unit;
    }
}
