using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Date range value object.
/// </summary>
public record DateRange
{
    public DateRange(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("Start date cannot be after end date.");
        }

        StartDate = startDate.Date;
        EndDate = endDate.Date;
    }

    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
}
