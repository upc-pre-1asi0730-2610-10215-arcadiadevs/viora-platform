namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents a single observed symptom.
/// </summary>
public record Symptom
{
    public string Description { get; }

    public Symptom(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Symptom description cannot be null or empty", nameof(description));
        }

        Description = description;
    }
}
