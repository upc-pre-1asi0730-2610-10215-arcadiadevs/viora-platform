using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record AgronomicStatisticResource(
    long UserId,
    long PlotId,
    DateTimeOffset MeasurementDate,
    double NdviValue,
    double ChillPortions,
    double ChillHours
);
