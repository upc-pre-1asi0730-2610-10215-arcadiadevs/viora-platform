using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

public static class DynamicNutritionPlanResourceFromEntityAssembler
{
    public static DynamicNutritionPlanResource ToResourceFromEntity(DynamicNutritionPlan entity)
    {
        return new DynamicNutritionPlanResource
        {
            DynamicNutritionPlanId = entity.Id,
            UserId = entity.UserId,
            PlotId = entity.PlotId,
            Status = entity.Status.ToString(),
            CertificationStatus = entity.IsCertified() ? "CERTIFIED" : "PENDING",
            GeneratedDate = entity.GeneratedDate.ToString("yyyy-MM-dd"),
            InputRecommendations = entity.InputRecommendations.Select(r => new NutritionInputRecommendationResource(r.Value, r.Purpose, r.Dosage, r.DosageUnit, r.Status.ToString())).ToList(),
            ApplicationWindow = new NutritionApplicationWindowResource(entity.ApplicationWindow.StartDate.ToString("yyyy-MM-dd"), entity.ApplicationWindow.EndDate.ToString("yyyy-MM-dd")),
            Rationale = new PlanRationaleResource(entity.Rationale.Summary, entity.Rationale.TriggeringRiskLevel.ToString(), entity.Rationale.NdviValue.Value, entity.Rationale.TemperatureAnomaly),
            Application = entity.Application == null ? null : new NutritionApplicationResource(
                entity.Application.AppliedInputs, 
                entity.Application.ApplicationDate, 
                entity.Application.ApplicationTime, 
                entity.Application.DoseConfirmation.ToString(), 
                entity.Application.FieldOperator, 
                entity.Application.FieldNotes)
        };
    }
}
