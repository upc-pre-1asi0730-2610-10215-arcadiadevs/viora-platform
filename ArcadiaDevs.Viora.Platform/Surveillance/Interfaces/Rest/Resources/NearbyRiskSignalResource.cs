namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Anonymized nearby risk signal shown on the Community Risk section.
/// </summary>
/// <param name="Id">Synthetic identifier (does not reveal the source alert).</param>
/// <param name="Title">Anonymized, human-readable headline for the signal.</param>
/// <param name="ProbableThreat">The probable threat behind the signal.</param>
/// <param name="Severity">Severity of the signal (LOW | MEDIUM | HIGH | CRITICAL).</param>
/// <param name="DistanceKm">Approximate distance from the reference plot, in kilometers.</param>
public record NearbyRiskSignalResource(
    string Id,
    string Title,
    string ProbableThreat,
    string Severity,
    double DistanceKm
);
