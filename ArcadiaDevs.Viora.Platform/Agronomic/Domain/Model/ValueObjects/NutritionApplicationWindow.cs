using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record NutritionApplicationWindow
{
    public DateTimeOffset StartDate { get; }
    public DateTimeOffset EndDate { get; }

    protected NutritionApplicationWindow() { }

    public NutritionApplicationWindow(
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsExpiredOn(DateTimeOffset date)
    {
        return EndDate < date;
    }
}
