using System.Linq;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="TreatmentPrescriptionResource" /> from the
///     <see cref="TreatmentPrescription" /> aggregate — mirrors
///     <c>ServiceProposalResourceFromEntityAssembler</c>'s
///     <c>FromEntity</c> naming.
/// </summary>
public static class TreatmentPrescriptionResourceFromEntityAssembler
{
    public static TreatmentPrescriptionResource ToResourceFromEntity(TreatmentPrescription entity)
    {
        return new TreatmentPrescriptionResource(
            entity.Id,
            entity.ServiceProposalId,
            entity.Status.ToString(),
            entity.FieldInspectionRecord?.FindingType,
            entity.FieldInspectionRecord?.IncidenceLevel,
            entity.FieldInspectionRecord?.TechnicalDescription,
            entity.FieldInspectionRecord?.RecordDate,
            entity.AgrochemicalPrescription?.ApplicationMethod.ToString(),
            entity.AgrochemicalPrescription?.SprayVolume.Amount,
            entity.AgrochemicalPrescription?.SprayVolume.Unit,
            entity.AgrochemicalPrescription?.PreHarvestInterval.Days,
            entity.AgrochemicalPrescription?.AgronomistRecommendations,
            entity.AgrochemicalPrescription?.RequiredPPE.Select(ppe => ppe.ToString()).ToList(),
            entity.AgrochemicalPrescription?.Products.Select(p => new PrescribedProductResource(
                p.ProductName,
                p.Dosage.Amount,
                p.Dosage.Unit,
                p.Sessions.Count,
                p.TechnicalRecommendation)).ToList());
    }
}
