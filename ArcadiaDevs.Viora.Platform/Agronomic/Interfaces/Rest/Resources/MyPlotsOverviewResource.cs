using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record MyPlotsOverviewResource(
    int RegisteredPlotCount,
    decimal MonitoredAreaHectares,
    int ClimateLinkedPlotCount,
    int OnlineDeviceCount,
    IEnumerable<OverviewPlotResource> Plots
);
