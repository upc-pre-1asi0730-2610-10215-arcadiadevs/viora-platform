using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetAgronomicStatisticSeriesQuery(
    long UserId,
    long AuthenticatedUserId,
    long? PlotId,
    ETimeRange TimeRange
);
