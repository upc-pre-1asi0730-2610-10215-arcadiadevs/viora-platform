using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record PlotResource(
    long Id,
    long UserId,
    string Name,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    decimal AreaSize,
    DateTimeOffset LastUpdate,
    string CropType,
    string Variety,
    string Location,
    string Campaign,
    string Notes,
    string State,
    string HealthStatus,
    string PhenologicalRisk,
    CurrentImageryResource? CurrentImagery = null
);
