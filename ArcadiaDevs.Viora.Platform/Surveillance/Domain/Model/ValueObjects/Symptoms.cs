namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents an immutable collection of observed symptoms.
/// </summary>
public record Symptoms
{
    public IReadOnlyList<Symptom> Items { get; }

    public Symptoms(IEnumerable<Symptom> items)
    {
        var symptomList = items?.ToList() ?? new List<Symptom>();
        if (symptomList.Count == 0)
        {
            throw new ArgumentException("Symptoms list cannot be null or empty", nameof(items));
        }

        Items = symptomList.AsReadOnly();
    }

    public static Symptoms FromDescriptions(IEnumerable<string> descriptions)
    {
        if (descriptions == null || !descriptions.Any())
        {
            throw new ArgumentException("Symptom descriptions list cannot be null or empty", nameof(descriptions));
        }

        return new Symptoms(descriptions.Select(d => new Symptom(d)));
    }

    public IReadOnlyList<string> GetDescriptions()
    {
        return Items.Select(i => i.Description).ToList().AsReadOnly();
    }
}
