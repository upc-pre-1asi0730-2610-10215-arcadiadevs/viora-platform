using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record AgronomicStatisticResource(
    string MeasurementDate,
    double NdviValue,
    double ChillPortions,
    double ChillHours
);
