namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record CommunityRiskResource(
    long PlotId,
    string PlotName,
    double RadiusKm,
    List<NearbyRiskSignalResource> Signals,
    List<string> PreventiveRecommendations
);
