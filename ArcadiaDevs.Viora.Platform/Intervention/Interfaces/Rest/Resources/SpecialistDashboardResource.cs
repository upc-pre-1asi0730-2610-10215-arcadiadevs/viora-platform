namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Read model for the specialist segment dashboard (Overview). Aggregates
///     the signed-in specialist's headline KPIs, the zonal prospecting radar,
///     their incoming producer requests, and the accepted-cases performance
///     series. Matches OS's <c>SpecialistDashboardResource.java</c> exactly.
/// </summary>
/// <remarks>
///     All fields are derived from real repository data. Metrics with no
///     domain source yet are returned empty (never fabricated):
///     <see cref="AcceptanceRatePercent" />/<see cref="AcceptanceRateDeltaPercent" />
///     and <see cref="PhytosanitaryEfficiencyPercent" />/
///     <see cref="PhytosanitaryStatus" /> may be <c>null</c>, and
///     <see cref="ZonalRisks" /> may be empty, until their outcome/geospatial
///     read models exist — mirrors OS's own "no fabricated data" contract
///     (<c>90bc928</c> refactor).
/// </remarks>
public record SpecialistDashboardResource(
    int ResolvedInterventions,
    double? AcceptanceRatePercent,
    double? AcceptanceRateDeltaPercent,
    double? PhytosanitaryEfficiencyPercent,
    string? PhytosanitaryStatus,
    IReadOnlyList<SpecialistDashboardResource.ZonalRisk> ZonalRisks,
    IReadOnlyList<SpecialistDashboardResource.IncomingRequest> IncomingRequests,
    IReadOnlyList<SpecialistDashboardResource.PerformancePoint> PerformanceMonthly,
    IReadOnlyList<SpecialistDashboardResource.PerformancePoint> PerformanceAnnual,
    string UpdatedAt)
{
    /// <summary>A single "Zonal prospecting radar" entry.</summary>
    public record ZonalRisk(
        long Id,
        string Severity,
        string Title,
        double DistanceKm,
        string Sector);

    /// <summary>An incoming producer request awaiting the specialist's verify/decline.</summary>
    public record IncomingRequest(
        int Id,
        string ReferenceCode,
        string PlotLabel,
        string GrowerLabel,
        string Problem,
        string? CreatedAt);

    /// <summary>A point in the accepted-cases performance series (monthly or annual).</summary>
    public record PerformancePoint(string Label, int Value);
}
