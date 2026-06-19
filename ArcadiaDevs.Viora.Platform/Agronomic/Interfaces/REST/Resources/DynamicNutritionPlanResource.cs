using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record NutritionInputRecommendationResource(string Value, string Purpose, double Dosage, string DosageUnit, string Status);
public record NutritionApplicationWindowResource(string StartDate, string EndDate);
public record PlanRationaleResource(string Summary, string TriggeringRiskLevel, double NdviValue, double TemperatureAnomaly);
public record NutritionApplicationResource(IReadOnlyCollection<string> AppliedInputs, DateOnly ApplicationDate, TimeOnly ApplicationTime, string DoseConfirmation, string FieldOperator, string? FieldNotes);

public record DynamicNutritionPlanResource
{
    public int DynamicNutritionPlanId { get; init; }
    public int UserId { get; init; }
    public int PlotId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CertificationStatus { get; init; } = string.Empty;
    public IReadOnlyList<NutritionInputRecommendationResource> InputRecommendations { get; init; } = Array.Empty<NutritionInputRecommendationResource>();
    public NutritionApplicationWindowResource ApplicationWindow { get; init; } = null!;
    public PlanRationaleResource Rationale { get; init; } = null!;
    public string GeneratedDate { get; init; } = string.Empty;
    public NutritionApplicationResource? Application { get; init; }
}
