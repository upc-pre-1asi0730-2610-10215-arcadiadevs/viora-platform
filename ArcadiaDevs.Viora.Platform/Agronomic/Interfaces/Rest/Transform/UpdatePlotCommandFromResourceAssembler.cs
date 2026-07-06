using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

public static class UpdatePlotCommandFromResourceAssembler
{
    public static UpdatePlotCommand ToCommandFromResource(this UpdatePlotResource resource, int plotId, int userId)
    {
        var geoPoints = resource.PolygonCoordinates.Select(p =>
        {
            var coords = p.ToList();
            return new GeoPoint { Latitude = (decimal)coords[1], Longitude = (decimal)coords[0] };
        }).ToList();

        return new UpdatePlotCommand(
            plotId,
            userId,
            resource.Name,
            resource.CropType,
            resource.Variety,
            resource.Location,
            resource.Campaign,
            resource.Notes,
            geoPoints
        );
    }
}
