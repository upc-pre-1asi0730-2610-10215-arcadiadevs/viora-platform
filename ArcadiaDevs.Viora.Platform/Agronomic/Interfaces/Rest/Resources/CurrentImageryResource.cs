using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record CurrentImageryResource(
    string Id,
    long PlotId,
    string TileUrl,
    DateTimeOffset CaptureDate,
    double NdviMean,
    double CloudPercentage
);
