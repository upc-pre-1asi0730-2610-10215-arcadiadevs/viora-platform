namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Hosting;

/// <summary>
/// Strongly-typed options for the agronomic statistics scheduled ingestion pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Bound from the <c>Agronomic:Statistics</c> configuration section.
/// </para>
/// <para>
/// The <see cref="ScheduledIngestionEnabled"/> flag controls whether the background
/// scheduler is active. It defaults to <c>false</c> (opt-in only) to avoid
/// unintended ingestion in development and testing environments.
/// </para>
/// <para>
/// The <see cref="IngestionCron"/> property is an informational label logged on
/// startup; the actual scheduling uses a 24-hour <c>PeriodicTimer</c>.
/// </para>
/// </remarks>
public sealed class AgronomicStatisticsOptions
{
    /// <summary>
    /// The configuration section path used for binding.
    /// </summary>
    public const string SectionName = "Agronomic:Statistics";

    /// <summary>
    /// Gets or sets a value indicating whether the scheduled ingestion is enabled.
    /// Defaults to <c>false</c> (opt-in only).
    /// </summary>
    public bool ScheduledIngestionEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the cron expression for the ingestion schedule.
    /// This is an informational label; the actual scheduling uses a 24-hour PeriodicTimer.
    /// </summary>
    public string IngestionCron { get; set; } = "0 0 2 * * *";
}