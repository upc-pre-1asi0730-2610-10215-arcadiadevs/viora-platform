using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     AgronomicStatistic aggregate root.
///     Represents the consolidated agronomic measurements of a plot for a specific date.
/// </summary>
public partial class AgronomicStatistic
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long PlotId { get; private set; }
    public DateTimeOffset MeasurementDate { get; private set; }
    public double NdviValue { get; private set; }
    public double ChillPortions { get; private set; }
    public double ChillHours { get; private set; }
    public ChillModelState ChillModelState { get; private set; }

    protected AgronomicStatistic()
    {
    }

    public AgronomicStatistic(
        long userId,
        long plotId,
        DateTimeOffset measurementDate,
        double ndviValue,
        double chillPortions,
        double chillHours,
        ChillModelState chillModelState)
    {
        UserId = userId;
        PlotId = plotId;
        MeasurementDate = measurementDate;
        NdviValue = ndviValue;
        ChillPortions = chillPortions;
        ChillHours = chillHours;
        ChillModelState = chillModelState ?? ChillModelState.Empty();
    }
}
