using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

public static class CreatePlotCommandFromResourceAssembler
{
    private const double EarthRadiusMeters = 6378137;

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
            CalculateAreaHectares(geoPoints),
            resource.CropType,
            resource.Variety,
            resource.Location,
            resource.Campaign,
            resource.Notes
        );
    }

    private static decimal CalculateAreaHectares(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count < 4) return 0m;

        var total = 0d;

        for (var index = 0; index < points.Count - 1; index++)
        {
            var current = points[index];
            var next = points[index + 1];

            total += ToRadians((double)(next.Longitude - current.Longitude)) *
                     (2 + Math.Sin(ToRadians((double)current.Latitude)) +
                      Math.Sin(ToRadians((double)next.Latitude)));
        }

        var squareMeters = Math.Abs(total * EarthRadiusMeters * EarthRadiusMeters / 2);
        return (decimal)(squareMeters / 10000);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
