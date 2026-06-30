using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     AgronomicStatistic aggregate root (AGRO-002 hardened).
///     Use the <see cref="Create"/> factory to enforce invariants; mutate
///     measurement state exclusively through the <c>RecordReading</c> domain
///     method, which validates inputs and returns a <see cref="Result{TValue, TError}"/>.
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
    public ChillModelState ChillModelState { get; private set; } = null!;

    // Parameterless constructor for EF Core materialization.
    private AgronomicStatistic() { }

    /// <summary>
    ///     Creates a new <see cref="AgronomicStatistic"/> with validated inputs.
    /// </summary>
    /// <returns>
    ///     A <see cref="Result{TValue, TError}"/> wrapping the statistic on
    ///     success, or an <see cref="Error"/> on validation failure.
    /// </returns>
    public static Result<AgronomicStatistic, Error> Create(
        long userId,
        long plotId,
        DateTimeOffset measurementDate,
        double ndviValue,
        double chillPortions,
        double chillHours,
        ChillModelState chillModelState)
    {
        if (userId <= 0)
        {
            return new Result<AgronomicStatistic, Error>.Failure(
                new Error("USER_ID_REQUIRED", "AgronomicStatistic requires a positive UserId."));
        }

        if (plotId <= 0)
        {
            return new Result<AgronomicStatistic, Error>.Failure(
                new Error("PLOT_ID_REQUIRED", "AgronomicStatistic requires a positive PlotId."));
        }

        if (ndviValue is < -1.0 or > 1.0)
        {
            return new Result<AgronomicStatistic, Error>.Failure(
                new Error("NDVI_OUT_OF_RANGE", "NDVI value must be between -1 and 1 (inclusive)."));
        }

        if (chillPortions < 0)
        {
            return new Result<AgronomicStatistic, Error>.Failure(
                new Error("CHILL_PORTIONS_NEGATIVE", "Chill portions cannot be negative."));
        }

        if (chillHours < 0)
        {
            return new Result<AgronomicStatistic, Error>.Failure(
                new Error("CHILL_HOURS_NEGATIVE", "Chill hours cannot be negative."));
        }

        var statistic = new AgronomicStatistic
        {
            UserId = userId,
            PlotId = plotId,
            MeasurementDate = measurementDate,
            NdviValue = ndviValue,
            ChillPortions = chillPortions,
            ChillHours = chillHours,
            ChillModelState = chillModelState ?? ChillModelState.Empty()
        };
        return new Result<AgronomicStatistic, Error>.Success(statistic);
    }

    /// <summary>
    ///     Records a new measurement on this statistic. Validates the same
    ///     invariants as <see cref="Create"/>; on failure returns a
    ///     <see cref="Result{TValue, TError}.Failure"/> and leaves state
    ///     unchanged.
    /// </summary>
    public Result<Unit, Error> RecordReading(
        double ndviValue,
        double chillPortions,
        double chillHours,
        ChillModelState chillModelState)
    {
        if (ndviValue is < -1.0 or > 1.0)
        {
            return new Result<Unit, Error>.Failure(
                new Error("NDVI_OUT_OF_RANGE", "NDVI value must be between -1 and 1 (inclusive)."));
        }

        if (chillPortions < 0)
        {
            return new Result<Unit, Error>.Failure(
                new Error("CHILL_PORTIONS_NEGATIVE", "Chill portions cannot be negative."));
        }

        if (chillHours < 0)
        {
            return new Result<Unit, Error>.Failure(
                new Error("CHILL_HOURS_NEGATIVE", "Chill hours cannot be negative."));
        }

        NdviValue = ndviValue;
        ChillPortions = chillPortions;
        ChillHours = chillHours;
        ChillModelState = chillModelState ?? ChillModelState.Empty();
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
