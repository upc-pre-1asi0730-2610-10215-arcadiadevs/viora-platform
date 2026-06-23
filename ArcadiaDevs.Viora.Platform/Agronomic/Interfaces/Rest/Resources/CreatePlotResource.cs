using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record CreatePlotResource(
    long UserId,
    string Name,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    string CropType,
    string Variety,
    string Location,
    string Campaign,
    string Notes
);
