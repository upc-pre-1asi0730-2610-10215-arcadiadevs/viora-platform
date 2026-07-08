namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

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
    public record ZonalRisk(
        long Id,
        string Severity,
        string Title,
        double DistanceKm,
        string Sector);

    public record IncomingRequest(
        int Id,
        string ReferenceCode,
        string PlotLabel,
        string GrowerLabel,
        string Problem,
        string? CreatedAt);

    public record PerformancePoint(string Label, int Value);
}
