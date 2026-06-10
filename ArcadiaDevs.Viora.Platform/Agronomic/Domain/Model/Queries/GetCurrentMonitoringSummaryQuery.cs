namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetCurrentMonitoringSummaryQuery
{
    public int UserId { get; init; }

    public GetCurrentMonitoringSummaryQuery(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("GetCurrentMonitoringSummaryQuery requires a valid UserId.", nameof(userId));

        UserId = userId;
    }
}