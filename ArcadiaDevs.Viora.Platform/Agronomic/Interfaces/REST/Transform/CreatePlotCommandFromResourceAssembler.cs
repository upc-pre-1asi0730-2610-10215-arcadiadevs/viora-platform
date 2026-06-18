using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

public static class CreatePlotCommandFromResourceAssembler
{
    public static CreatePlotCommand ToCommand(this CreatePlotResource resource)
    {
        var geoPoints = resource.PolygonCoordinates.Select(p => 
        {
            var coords = p.ToList();
            return new GeoPoint { Latitude = (decimal)coords[1], Longitude = (decimal)coords[0] };
        }).ToList();

        return new CreatePlotCommand(
            (int)resource.UserId,
            resource.Name,
            geoPoints,
            0m
        );
    }
}
