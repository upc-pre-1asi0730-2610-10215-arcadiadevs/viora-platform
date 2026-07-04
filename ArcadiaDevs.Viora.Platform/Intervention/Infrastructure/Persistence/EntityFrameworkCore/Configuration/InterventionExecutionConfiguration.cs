using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the InterventionExecution aggregate root.
///     Explicit snake_case naming mirrors <c>TreatmentPrescriptionConfiguration</c>.
/// </summary>
public class InterventionExecutionConfiguration : IEntityTypeConfiguration<InterventionExecution>
{
    public void Configure(EntityTypeBuilder<InterventionExecution> builder)
    {
        builder.ToTable("intervention_executions");

        builder.HasKey(ie => ie.Id);

        builder.Property(ie => ie.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(ie => ie.TreatmentPrescriptionId)
            .HasColumnName("treatment_prescription_id")
            .IsRequired();

        // Unique: one execution per prescription (REQ-IE-2 idempotency).
        builder.HasIndex(ie => ie.TreatmentPrescriptionId)
            .IsUnique();

        builder.Property(ie => ie.ApplicationDate)
            .HasColumnName("application_date")
            .IsRequired();

        builder.Property(ie => ie.AppliedArea)
            .HasColumnName("applied_area")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(ie => ie.ExecutionStatus)
            .HasColumnName("execution_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ie => ie.FieldNote)
            .HasColumnName("field_note");
    }
}
