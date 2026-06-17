namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
/// Resource used to represent a symptom catalog item in HTTP responses.
/// </summary>
public record SymptomResource(
    string Id,
    string Description
);
