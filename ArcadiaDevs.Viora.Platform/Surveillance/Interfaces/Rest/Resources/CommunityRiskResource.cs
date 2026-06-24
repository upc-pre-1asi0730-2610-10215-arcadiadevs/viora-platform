namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Community-risk snapshot around a reference plot: anonymized nearby signals detected
///     within a monitoring radius, plus derived preventive recommendations.
/// </summary>
/// <param name="PlotId">The reference plot identifier.</param>
/// <param name="PlotName">The reference plot display name.</param>
/// <param name="RadiusKm">The monitoring radius in kilometers.</param>
/// <param name="Signals">The anonymized nearby risk signals.</param>
/// <param name="PreventiveRecommendations">Derived preventive recommendations.</param>
public record CommunityRiskResource(
    long PlotId,
    string PlotName,
    double RadiusKm,
    List<NearbyRiskSignalResource> Signals,
    List<string> PreventiveRecommendations
);
