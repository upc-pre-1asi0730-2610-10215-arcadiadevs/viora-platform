namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetAgronomicStatisticsQuery
{
    public int UserId { get; init; }
    public int? PlotId { get; init; }
    public string TimeRange { get; init; }

    public GetAgronomicStatisticsQuery(int userId, int? plotId, string timeRange)
    {
        if (userId <= 0)
            throw new ArgumentException("GetAgronomicStatisticsQuery requires a valid UserId.", nameof(userId));

        var validTimeRanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "week", "month", "quarter", "year" };
        if (string.IsNullOrWhiteSpace(timeRange) || !validTimeRanges.Contains(timeRange))
            throw new ArgumentException("GetAgronomicStatisticsQuery requires a valid TimeRange (week, month, quarter, year).", nameof(timeRange));

        UserId = userId;
        PlotId = plotId;
        TimeRange = timeRange.ToLowerInvariant();
    }
}