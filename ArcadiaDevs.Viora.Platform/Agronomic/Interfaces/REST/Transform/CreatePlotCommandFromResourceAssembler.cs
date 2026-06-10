using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a create plot command from its REST resource.
/// </summary>
public static class CreatePlotCommandFromResourceAssembler
{
    /// <summary>
    ///     Maps a CreatePlotResource to a CreatePlotCommand.
    /// </summary>
    /// <param name="resource">The request resource.</param>
    /// <returns>The mapped command.</returns>
    public static CreatePlotCommand ToCommand(this CreatePlotResource resource)
    {
        var geoPoints = resource.PolygonCoordinates
            .Select(point => new GeoPoint { Latitude = point.Latitude, Longitude = point.Longitude })
            .ToList();

        return new CreatePlotCommand(
            resource.OwnerUserId,
            resource.PlotName,
            geoPoints,
            resource.AreaSize);
    }
}
