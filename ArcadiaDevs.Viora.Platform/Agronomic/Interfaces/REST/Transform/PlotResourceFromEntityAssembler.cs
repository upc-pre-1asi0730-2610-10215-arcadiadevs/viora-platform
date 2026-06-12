using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a REST resource from a plot aggregate.
/// </summary>
public static class PlotResourceFromEntityAssembler
{
    /// <summary>
    ///     Maps a Plot aggregate to a PlotResource.
    /// </summary>
    /// <param name="plot">The plot aggregate.</param>
    /// <returns>The mapped response DTO.</returns>
    public static PlotResource ToResource(this Plot plot)
    {
        var geoPointDtos = plot.PolygonCoordinates.Points
            .Select(p => new GeoPointResource(p.Latitude, p.Longitude))
            .ToList();

        return new PlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            geoPointDtos,
            plot.AreaSize,
            plot.CreatedAt ?? DateTimeOffset.UtcNow,
            plot.AgroMonitoringPolygonId,
            plot.AgroMonitoringCenter);
    }
}
