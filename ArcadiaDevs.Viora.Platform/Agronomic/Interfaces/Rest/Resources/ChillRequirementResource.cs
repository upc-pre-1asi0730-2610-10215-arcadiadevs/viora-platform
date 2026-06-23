namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a plot's effective chill requirement.
/// </summary>
public record ChillRequirementResource(
    double ChillRequirementPortions,
    string Source,
    string Model,
    string Unit
);
