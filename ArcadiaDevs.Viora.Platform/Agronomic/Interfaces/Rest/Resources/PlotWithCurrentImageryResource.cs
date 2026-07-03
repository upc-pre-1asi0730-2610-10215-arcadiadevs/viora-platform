using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Composite resource returned by <c>GET /?includeCurrentImagery=true</c>.
///     Combines the trimmed <see cref="PlotResource"/> with imagery data.
/// </summary>
public record PlotWithCurrentImageryResource(
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
    CurrentImageryResource CurrentImagery
);
