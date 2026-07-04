namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object representing a <see cref="Aggregates.ServiceProposal" />'s
///     estimated cost (REQ-SP-1). <see cref="Amount" /> must be
///     non-negative and <see cref="Currency" /> must be non-blank.
/// </summary>
public record CostEstimate
{
    public decimal Amount { get; }

    public string Currency { get; }

    public CostEstimate(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Cost estimate amount cannot be negative.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Cost estimate currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }
}
