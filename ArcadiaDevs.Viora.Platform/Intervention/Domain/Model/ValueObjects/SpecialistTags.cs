namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Represents an immutable collection of specialty tags for a
///     <see cref="Aggregates.Specialist" /> (e.g. "Xylella", "Pest Control").
/// </summary>
/// <remarks>
///     Mirrors the Surveillance BC's <c>Symptoms</c> VO shape, but tags are
///     allowed to be empty (a specialist may have no declared specialties
///     yet), unlike <c>Symptoms</c> which requires at least one item.
/// </remarks>
public record SpecialistTags
{
    public IReadOnlyList<string> Items { get; }

    public SpecialistTags(IEnumerable<string> items)
    {
        Items = (items ?? Enumerable.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList()
            .AsReadOnly();
    }

    public static SpecialistTags Empty => new(Enumerable.Empty<string>());
}
