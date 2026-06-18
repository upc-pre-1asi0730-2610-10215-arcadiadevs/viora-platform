using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record MonitoringLinksResource(
    string ClimateMonitoring,
    string SatelliteNdvi,
    DateTimeOffset ClimateLastSyncAt,
    DateTimeOffset SatelliteLastSyncAt
);

public record IoTDetailResource(
    string Status,
    int LinkedDeviceCount,
    int OnlineDeviceCount,
    DateTimeOffset LastActivityAt
);

public record PlotDeviceResource(
    long Id,
    string Name,
    string Status,
    DateTimeOffset LinkedAt,
    DateTimeOffset LastActivityAt
);

public record RecentConfigurationActivityResource(
    string Type,
    string Description,
    DateTimeOffset OccurredAt
);

public record PlotDetailResource(
    long Id,
    long UserId,
    string Name,
    string Location,
    string Campaign,
    string CropType,
    string Variety,
    string Notes,
    IEnumerable<IEnumerable<double>> PolygonCoordinates,
    decimal AreaSizeHectares,
    int BoundaryPointCount,
    string BoundaryStatus,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastConfigurationUpdateAt,
    MonitoringLinksResource MonitoringLinks,
    IoTDetailResource Iot,
    IEnumerable<PlotDeviceResource> Devices,
    IEnumerable<RecentConfigurationActivityResource> RecentConfigurationActivity
);
