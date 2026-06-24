namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

/// <summary>
///     Query for the anonymized community-risk snapshot around a reference plot.
/// </summary>
public record GetCommunityRiskByPlotQuery
{
    /// <summary>The reference plot identifier.</summary>
    public long PlotId { get; }

    /// <summary>The monitoring radius in kilometers.</summary>
    public double RadiusKm { get; }

    /// <summary>
    ///     Creates the query, validating that the plot id and radius are positive.
    /// </summary>
    public GetCommunityRiskByPlotQuery(long plotId, double radiusKm)
    {
        if (plotId <= 0)
        {
            throw new ArgumentException("Plot ID must be a positive number.", nameof(plotId));
        }

        if (radiusKm <= 0)
        {
            throw new ArgumentException("Radius must be a positive number.", nameof(radiusKm));
        }

        PlotId = plotId;
        RadiusKm = radiusKm;
    }
}
