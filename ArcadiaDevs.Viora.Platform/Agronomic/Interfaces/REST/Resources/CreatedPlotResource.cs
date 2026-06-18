using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record CreatedPlotResource(
    long Id,
    long UserId,
    string Name,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    decimal AreaSizeHectares,
    string CropType,
    string Variety,
    string Location,
    string Campaign,
    string Notes,
    string State,
    string ClimateMonitoring,
    string SatelliteNdvi,
    string IotDevices
);
