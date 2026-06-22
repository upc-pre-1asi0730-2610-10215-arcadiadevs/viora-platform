using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record OverviewPlotResource(
    long Id,
    long UserId,
    string Name,
    string Location,
    string Campaign,
    string CropType,
    string Variety,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    decimal AreaSizeHectares,
    double CurrentNdvi,
    double ChillPortions,
    string HealthStatus,
    string PhenologicalRisk,
    int OnlineDeviceCount,
    int ActiveAlertCount,
    DateTimeOffset LastUpdatedAt,
    string ClimateMonitoring,
    string SatelliteNdvi
);
