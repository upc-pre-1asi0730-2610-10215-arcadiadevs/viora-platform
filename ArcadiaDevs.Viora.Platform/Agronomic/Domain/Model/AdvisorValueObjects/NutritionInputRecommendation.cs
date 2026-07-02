using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.AdvisorValueObjects;

/// <summary>
///     A nutrition input recommendation for mitigation.
/// </summary>
public record NutritionInputRecommendation
{
    public string Description { get; init; }

    public NutritionInputRecommendation(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty.", nameof(description));

        Description = description;
    }
}
