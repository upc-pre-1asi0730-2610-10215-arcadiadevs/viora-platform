using System.Linq;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
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
        // ApplicationMethod/SprayVolume/PreHarvestInterval are structured value
        // objects (2026-07-05 field-level parity fix — previously opaque
        // strings); SprayVolume/PreHarvestInterval are flattened into scalar
        // columns since they're simple 1-2 field VOs, mirroring OS's own
        // flattened REST DTO shape (PrescribeTreatmentResource.java).
        builder.OwnsOne(tp => tp.AgrochemicalPrescription, prescriptionBuilder =>
        {
            prescriptionBuilder.Property<int>("TreatmentPrescriptionId").HasColumnName("id");

            prescriptionBuilder.Property(p => p.ApplicationMethod)
                .HasColumnName("application_method")
                .HasConversion<string>()
                .HasMaxLength(30);

            prescriptionBuilder.OwnsOne(p => p.SprayVolume, sprayVolumeBuilder =>
            {
                sprayVolumeBuilder.Property<int>("AgrochemicalPrescriptionTreatmentPrescriptionId").HasColumnName("id");
                sprayVolumeBuilder.Property(sv => sv.Amount).HasColumnName("spray_volume_amount");
                sprayVolumeBuilder.Property(sv => sv.Unit).HasColumnName("spray_volume_unit").HasMaxLength(20);
            });

            prescriptionBuilder.OwnsOne(p => p.PreHarvestInterval, preHarvestBuilder =>
            {
                preHarvestBuilder.Property<int>("AgrochemicalPrescriptionTreatmentPrescriptionId").HasColumnName("id");
                preHarvestBuilder.Property(phi => phi.Days).HasColumnName("pre_harvest_interval_days");
            });

            prescriptionBuilder.Property(p => p.AgronomistRecommendations)
                .HasColumnName("agronomist_recommendations");

            // RequiredPPE and Products are JSON-serialized columns (mirrors the
            // pre-existing Products-as-JSON precedent) — the value comparer
            // keeps EF's change tracker from always seeing the property as
            // "modified" since it compares by content, not by list reference.
            var ppeComparer = new ValueComparer<IReadOnlyList<PersonalProtectiveEquipment>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            prescriptionBuilder.Property(p => p.RequiredPPE)
                .HasColumnName("required_ppe")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<PersonalProtectiveEquipment>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                        ?? new List<PersonalProtectiveEquipment>(),
                    ppeComparer);

            var productsComparer = new ValueComparer<IReadOnlyList<PrescribedProduct>>(
                (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            prescriptionBuilder.Property(p => p.Products)
                .HasColumnName("products")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<PrescribedProduct>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                        ?? new List<PrescribedProduct>(),
                    productsComparer);
        });
    }
}
