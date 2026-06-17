namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     A value object representing accumulated chill portions in the Dynamic Model.
/// </summary>
public record ChillPortions
{
    public double Value { get; init; }

    public ChillPortions(double value)
    {
        if (value < 0)
        {
            throw new ArgumentException("Chill portions cannot be negative.", nameof(value));
        }

        Value = value;
    }
}
