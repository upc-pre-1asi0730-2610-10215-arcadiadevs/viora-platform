using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record NutritionInputRecommendation
{
    public string Value { get; }
    public string Purpose { get; }
    public double Dosage { get; }
    public string DosageUnit { get; }
    public ENutritionInputStatus Status { get; }

    protected NutritionInputRecommendation() { }

    public NutritionInputRecommendation(
        string value,
        string purpose,
        double dosage,
        string dosageUnit,
        ENutritionInputStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Input recommendation value is required.");
            
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("Input recommendation purpose is required.");

        if (string.IsNullOrWhiteSpace(dosageUnit))
            throw new ArgumentException("Input recommendation dosage unit is required.");

        if (dosage <= 0)
            throw new ArgumentException("Input recommendation dosage must be greater than zero.");

        Value = value.Trim();
        Purpose = purpose.Trim();
        Dosage = dosage;
        DosageUnit = dosageUnit.Trim();
        Status = status;
    }
}
