using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record UpdatePlotResource(
    string Name,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    string CropType,
    string Variety,
    string Location,
    string Campaign,
    string Notes
);
