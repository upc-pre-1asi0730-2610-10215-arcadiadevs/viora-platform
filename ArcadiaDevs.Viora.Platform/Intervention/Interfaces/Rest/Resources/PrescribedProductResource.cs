namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     A single prescribed product, used both as part of the request body for
///     the PRESCRIPTION stage of <c>PATCH /treatment-prescriptions/{id}</c> and
///     as part of the response. Field names match OS's
///     <c>PrescribedProductResource.java</c> exactly.
/// </summary>
public record PrescribedProductResource(
    string ProductName,
    double DosageAmount,
    string DosageUnit,
    int SessionsCount,
    string? TechnicalRecommendation);
