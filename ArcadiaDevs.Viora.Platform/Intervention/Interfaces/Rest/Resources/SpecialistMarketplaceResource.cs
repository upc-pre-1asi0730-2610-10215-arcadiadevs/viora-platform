namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record SpecialistMarketplaceResource(
    int NewCasesCount,
    double? AcceptanceRatePercent,
    int ActiveCasesCount,
    IReadOnlyList<SpecialistMarketplaceResource.MarketplaceCase> Cases,
    string UpdatedAt)
{
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
