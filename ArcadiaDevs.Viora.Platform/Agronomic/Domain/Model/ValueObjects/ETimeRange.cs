using System;
using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Time range value object.
///     Represents predefined time ranges used to query agronomic statistics.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ETimeRange
{
    LAST_7_DAYS,
    LAST_30_DAYS,
    LAST_90_DAYS,
    LAST_180_DAYS,
    LAST_365_DAYS,
    /// <summary>Current campaign, modeled as a rolling one-year window until season dates are tracked.</summary>
    CAMPAIGN
}

public static class ETimeRangeExtensions
{
    public static int GetDays(this ETimeRange timeRange)
    {
        return timeRange switch
        {
            ETimeRange.LAST_7_DAYS => 7,
            ETimeRange.LAST_30_DAYS => 30,
            ETimeRange.LAST_90_DAYS => 90,
            ETimeRange.LAST_180_DAYS => 180,
            ETimeRange.LAST_365_DAYS => 365,
            ETimeRange.CAMPAIGN => 365,
            _ => throw new ArgumentOutOfRangeException(nameof(timeRange), timeRange, null)
        };
    }

    public static DateRange ToDateRange(this ETimeRange timeRange, DateTimeOffset referenceDate)
    {
        return new DateRange(
            referenceDate.AddDays(-(timeRange.GetDays() - 1)),
            referenceDate
        );
    }

    public static ETimeRange FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Time range is required.");
        }

        if (Enum.TryParse<ETimeRange>(value.Trim().ToUpperInvariant().Replace("-", "_"), out var timeRange))
        {
            return timeRange;
        }

        throw new ArgumentException("Invalid time range. Allowed values are: LAST_7_DAYS, LAST_30_DAYS, LAST_90_DAYS, LAST_180_DAYS, LAST_365_DAYS, CAMPAIGN.");
    }
}
