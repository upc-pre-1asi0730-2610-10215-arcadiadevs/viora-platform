namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record ChillRequirementResource(
    double ChillRequirementPortions,
    string Source,
    string Model,
    string Unit
);
