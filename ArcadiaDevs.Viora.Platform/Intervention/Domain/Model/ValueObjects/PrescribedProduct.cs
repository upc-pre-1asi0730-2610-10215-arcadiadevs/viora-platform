namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     A single product prescribed within an <see cref="AgrochemicalPrescription" />
///     (REQ-TP-3).
/// </summary>
public record PrescribedProduct
{
    public string ProductName { get; }

    public Dosage Dosage { get; }

    public ApplicationSessions Sessions { get; }

    public string TechnicalRecommendation { get; }

    public PrescribedProduct(
        string productName,
        Dosage dosage,
        ApplicationSessions sessions,
        string technicalRecommendation)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name cannot be blank.", nameof(productName));
        }

        ArgumentNullException.ThrowIfNull(dosage);
        ArgumentNullException.ThrowIfNull(sessions);

        ProductName = productName;
        Dosage = dosage;
        Sessions = sessions;
        TechnicalRecommendation = technicalRecommendation ?? string.Empty;
    }
}
