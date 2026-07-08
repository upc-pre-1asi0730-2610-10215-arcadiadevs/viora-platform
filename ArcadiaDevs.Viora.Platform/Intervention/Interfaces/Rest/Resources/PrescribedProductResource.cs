namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record PrescribedProductResource(
    string ProductName,
    double DosageAmount,
    string DosageUnit,
    int SessionsCount,
    string? TechnicalRecommendation);
