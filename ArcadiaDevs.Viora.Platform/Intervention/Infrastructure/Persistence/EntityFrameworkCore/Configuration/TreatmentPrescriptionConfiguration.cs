using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the TreatmentPrescription aggregate root.
///     Explicit snake_case naming mirrors <c>ServiceProposalConfiguration</c>.
/// </summary>
public class TreatmentPrescriptionConfiguration : IEntityTypeConfiguration<TreatmentPrescription>
{
    public void Configure(EntityTypeBuilder<TreatmentPrescription> builder)
    {
        builder.ToTable("treatment_prescriptions");

        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(tp => tp.ServiceProposalId)
            .HasColumnName("service_proposal_id")
            .IsRequired();

        // Unique: one prescription per proposal (REQ-TP-4 idempotency).
        builder.HasIndex(tp => tp.ServiceProposalId)
            .IsUnique();

        builder.Property(tp => tp.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // FieldInspectionRecord is only present from INSPECTED onward — owned,
        // optional (unlike ServiceProposal.CostEstimate, no .IsRequired() call
        // here, so the owned nav stays nullable until LogFieldInspection runs).
        builder.OwnsOne(tp => tp.FieldInspectionRecord, inspectionBuilder =>
        {
            inspectionBuilder.Property<int>("TreatmentPrescriptionId").HasColumnName("id");

            inspectionBuilder.Property(r => r.FindingType)
                .HasColumnName("finding_type");

            inspectionBuilder.Property(r => r.IncidenceLevel)
                .HasColumnName("incidence_level");

            inspectionBuilder.Property(r => r.TechnicalDescription)
                .HasColumnName("technical_description");

            inspectionBuilder.Property(r => r.RecordDate)
                .HasColumnName("record_date");
        });

        // AgrochemicalPrescription is only present from PRESCRIBED onward —
        // owned, optional (same shape as FieldInspectionRecord above).
        builder.OwnsOne(tp => tp.AgrochemicalPrescription, prescriptionBuilder =>
        {
            prescriptionBuilder.Property<int>("TreatmentPrescriptionId").HasColumnName("id");

            prescriptionBuilder.Property(p => p.ApplicationMethod)
                .HasColumnName("application_method");

            prescriptionBuilder.Property(p => p.SprayVolume)
                .HasColumnName("spray_volume");

            prescriptionBuilder.Property(p => p.PreHarvestInterval)
                .HasColumnName("pre_harvest_interval");

            prescriptionBuilder.Property(p => p.AgronomistRecommendations)
                .HasColumnName("agronomist_recommendations");

            prescriptionBuilder.Property(p => p.RequiredPPE)
                .HasColumnName("required_ppe");

            // Compares by product content, not by IReadOnlyList<string>
            // reference — mirrors SpecialistConfiguration.Tags' ValueComparer
            // precedent (WU1) to keep EF's change tracker from always seeing
            // the property as "modified".
            var productsComparer = new ValueComparer<IReadOnlyList<string>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            prescriptionBuilder.Property(p => p.Products)
                .HasColumnName("products")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                        ?? new List<string>(),
                    productsComparer);
        });
    }
}
