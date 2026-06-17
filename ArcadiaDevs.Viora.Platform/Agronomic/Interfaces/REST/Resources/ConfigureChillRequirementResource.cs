namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for configuring a plot's chill requirement.
/// </summary>
public record ConfigureChillRequirementResource(
    double ChillRequirementPortions
);
