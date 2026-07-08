namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record NearbyRiskSignalResource(
    string Id,
    string Title,
    string ProbableThreat,
    string Severity,
    double DistanceKm
);
