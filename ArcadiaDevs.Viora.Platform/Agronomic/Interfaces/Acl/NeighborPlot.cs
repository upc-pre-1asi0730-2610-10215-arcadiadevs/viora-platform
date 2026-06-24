namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;

/// <summary>
///     Anonymized neighbor-plot reference exposed across bounded contexts.
/// </summary>
/// <remarks>
///     Carries only the plot identifier and its great-circle distance (in kilometers)
///     from a reference plot, intentionally excluding any owner or naming information so
///     consumers (e.g. community-risk diffusion) cannot de-anonymize the neighbor.
/// </remarks>
/// <param name="PlotId">The neighbor plot identifier.</param>
/// <param name="DistanceKm">Distance from the reference plot centroid, in kilometers.</param>
public record NeighborPlot(long PlotId, double DistanceKm);
