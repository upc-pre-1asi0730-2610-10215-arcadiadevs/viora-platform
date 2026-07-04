using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the InterventionOutcome aggregate root.
///     Explicit snake_case naming mirrors <c>InterventionExecutionConfiguration</c>.
/// </summary>
public class InterventionOutcomeConfiguration : IEntityTypeConfiguration<InterventionOutcome>
{
    public void Configure(EntityTypeBuilder<InterventionOutcome> builder)
    {
        builder.ToTable("intervention_outcomes");

        builder.HasKey(io => io.Id);

        builder.Property(io => io.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(io => io.InterventionExecutionId)
            .HasColumnName("intervention_execution_id")
            .IsRequired();

        // Unique: one outcome per execution (REQ-IO-3 idempotency).
        builder.HasIndex(io => io.InterventionExecutionId)
            .IsUnique();

        builder.Property(io => io.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // ImpactReport is set at creation time (REQ-IO-1) — owned, required
        // (unlike TreatmentPrescription.FieldInspectionRecord, which is
        // optional until logged).
        builder.OwnsOne(io => io.ImpactReport, reportBuilder =>
        {
            reportBuilder.Property<int>("InterventionOutcomeId").HasColumnName("id");

            reportBuilder.Property(r => r.GracePeriod)
                .HasColumnName("grace_period");

            reportBuilder.Property(r => r.ObservedResult)
                .HasColumnName("observed_result");

            reportBuilder.Property(r => r.ImpactLevel)
                .HasColumnName("impact_level");

            reportBuilder.Property(r => r.ProducerAssessment)
                .HasColumnName("producer_assessment");
        });

        builder.Navigation(io => io.ImpactReport).IsRequired();

        // ServiceEvaluation is only present from CLOSED onward — owned,
        // optional (mirrors TreatmentPrescription.AgrochemicalPrescription's
        // owned-and-optional precedent).
        builder.OwnsOne(io => io.ServiceEvaluation, evaluationBuilder =>
        {
            evaluationBuilder.Property<int>("InterventionOutcomeId").HasColumnName("id");

            evaluationBuilder.Property(e => e.ServiceResult)
                .HasColumnName("service_result");

            evaluationBuilder.Property(e => e.HireAgain)
                .HasColumnName("hire_again");

            evaluationBuilder.Property(e => e.PrivateFeedback)
                .HasColumnName("private_feedback");
        });
    }
}
