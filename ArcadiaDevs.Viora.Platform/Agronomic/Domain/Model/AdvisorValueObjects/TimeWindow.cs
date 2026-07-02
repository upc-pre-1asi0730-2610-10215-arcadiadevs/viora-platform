using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.AdvisorValueObjects;

/// <summary>
///     A time window for recommended application.
/// </summary>
public record TimeWindow
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    public TimeWindow(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date must be on or before end date.", nameof(startDate));

        StartDate = startDate;
        EndDate = endDate;
    }
}
