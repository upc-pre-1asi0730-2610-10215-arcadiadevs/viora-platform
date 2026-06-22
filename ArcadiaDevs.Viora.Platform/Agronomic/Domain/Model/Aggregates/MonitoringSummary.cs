using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     Represents a monitoring summary for a user's agronomic data.
/// </summary>
/// <remarks>
///     This is the aggregate root for monitoring summaries. Use the <see cref="Create"/> factory method
///     to enforce invariants and validate input.
/// </remarks>
public class MonitoringSummary : IAuditableEntity
{
    /// <summary>
    ///     Gets the unique identifier for the monitoring summary.
    /// </summary>
    public MonitoringSummaryId MonitoringSummaryId { get; init; }

    /// <summary>
    ///     Gets the user identifier associated with this summary.
    /// </summary>
    public UserId UserId { get; init; }

    /// <summary>
    ///     Gets the general health status.
    /// </summary>
    public GeneralHealthStatus GeneralHealthStatus { get; init; }

    /// <summary>
    ///     Gets the average NDVI.
    /// </summary>
    public AverageNdvi AverageNdvi { get; init; }

    /// <summary>
    ///     Gets the accumulated chill hours.
    /// </summary>
    public AccumulatedChillHours AccumulatedChillHours { get; init; }

    /// <summary>
    ///     Gets the yield projection.
    /// </summary>
    public YieldProjection YieldProjection { get; init; }

    /// <summary>
    ///     Gets the weather snapshot.
    /// </summary>
    public required WeatherSnapshot WeatherSnapshot { get; init; }

    /// <summary>
    ///     Gets the mitigation recommendation.
    /// </summary>
    public required MitigationRecommendation MitigationRecommendation { get; init; }

    /// <summary>
    ///     Gets the timestamp of the last data synchronization.
    /// </summary>
    public LastSynchronizationAt LastSynchronizationAt { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    ///     EF Core constructor.
    /// </summary>
    private MonitoringSummary() { }

    /// <summary>
    ///     Creates a new MonitoringSummary with validated invariants.
    /// </summary>
    public static Result<MonitoringSummary, Error> Create(
        int userId,
        string generalHealthStatus,
        decimal averageNdvi,
        decimal accumulatedChillHours,
        decimal yieldProjection,
        WeatherSnapshot weatherSnapshot,
        MitigationRecommendation mitigationRecommendation,
        DateTimeOffset lastSynchronizationAt)
    {
        try
        {
            var userIdVo = new UserId(userId);
            var healthStatusVo = GeneralHealthStatusExtensions.FromString(generalHealthStatus);
            var ndviVo = new AverageNdvi(averageNdvi);
            var chillHoursVo = new AccumulatedChillHours(accumulatedChillHours);
            var yieldProjectionVo = new YieldProjection(yieldProjection);
            var syncAtVo = new LastSynchronizationAt(lastSynchronizationAt);

            var summary = new MonitoringSummary
            {
                UserId = userIdVo,
                GeneralHealthStatus = healthStatusVo,
                AverageNdvi = ndviVo,
                AccumulatedChillHours = chillHoursVo,
                YieldProjection = yieldProjectionVo,
                WeatherSnapshot = weatherSnapshot,
                MitigationRecommendation = mitigationRecommendation,
                LastSynchronizationAt = syncAtVo
            };

            return new Result<MonitoringSummary, Error>.Success(summary);
        }
        catch (ArgumentException ex)
        {
            return new Result<MonitoringSummary, Error>.Failure(
                new Error("INVALID_MONITORING_SUMMARY", ex.Message));
        }
    }
}
