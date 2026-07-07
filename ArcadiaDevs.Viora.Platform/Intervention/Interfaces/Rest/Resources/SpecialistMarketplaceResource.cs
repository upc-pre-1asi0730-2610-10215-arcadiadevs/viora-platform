namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Read model for the specialist Intervention Marketplace (Overview):
///     the inbox of incoming producer cases routed to the signed-in
///     specialist and still awaiting a response, plus the headline
///     counters.
/// </summary>
/// <remarks>
///     Each case is enriched across bounded contexts (Surveillance for the
///     alert severity/problem, Agronomic for the plot and NDVI, Profile for
///     the producer's name) via ACL facades. Fields with no real source for
///     a given case are returned <c>null</c> — never fabricated — so the
///     client renders empty states. <c>DistanceKm</c> is <c>null</c> when
///     the specialist has no geolocation set on their profile.
/// </remarks>
public record SpecialistMarketplaceResource(
    int NewCasesCount,
    double? AcceptanceRatePercent,
    int ActiveCasesCount,
    IReadOnlyList<SpecialistMarketplaceResource.MarketplaceCase> Cases,
    string UpdatedAt)
{
    /// <summary>A single incoming producer case awaiting the specialist's response.</summary>
    public record MarketplaceCase(
        int Id,
        string ReferenceCode,
        int SpecialistId,
        string? Severity,
        string? Problem,
        double? Ndvi,
        string? ProducerName,
        string? ProducerPhotoUrl,
        string? ProductionType,
        string? PlotName,
        string? Location,
        decimal? AreaHectares,
        int? PlotCount,
        string? CreatedAt,
        double? DistanceKm);
}
