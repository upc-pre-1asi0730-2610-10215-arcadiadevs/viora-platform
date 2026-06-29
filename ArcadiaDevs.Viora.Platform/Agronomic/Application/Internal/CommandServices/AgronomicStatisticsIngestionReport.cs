namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Outcome of an agronomic statistic snapshot ingestion run.
/// </summary>
/// <param name="Ingested">Number of new daily snapshots persisted.</param>
/// <param name="Skipped">Number of plots skipped (already snapshotted today or no real signal).</param>
public record AgronomicStatisticsIngestionReport(int Ingested, int Skipped)
{
    /// <summary>
    ///     Creates an empty report.
    /// </summary>
    public static AgronomicStatisticsIngestionReport Empty()
    {
        return new AgronomicStatisticsIngestionReport(0, 0);
    }

    /// <summary>
    ///     Returns a new report with the ingested count incremented.
    /// </summary>
    public AgronomicStatisticsIngestionReport WithIngested()
    {
        return new AgronomicStatisticsIngestionReport(Ingested + 1, Skipped);
    }

    /// <summary>
    ///     Returns a new report with the skipped count incremented.
    /// </summary>
    public AgronomicStatisticsIngestionReport WithSkipped()
    {
        return new AgronomicStatisticsIngestionReport(Ingested, Skipped + 1);
    }
}
