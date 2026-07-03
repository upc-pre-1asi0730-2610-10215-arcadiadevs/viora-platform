using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the InterventionRequest aggregate root.
///     Explicit snake_case naming mirrors <c>SpecialistConfiguration</c>.
/// </summary>
public class InterventionRequestConfiguration : IEntityTypeConfiguration<InterventionRequest>
{
    public void Configure(EntityTypeBuilder<InterventionRequest> builder)
    {
        builder.ToTable("intervention_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.GrowerId)
            .HasColumnName("grower_id")
            .IsRequired();

        builder.Property(r => r.PlotId)
            .HasColumnName("plot_id")
            .IsRequired();

        builder.Property(r => r.SpecialistId)
            .HasColumnName("specialist_id")
            .IsRequired();

        // Non-unique: a grower may raise multiple requests against the same
        // specialist over time; also used as the caller-ownership lookup
        // key for the gated specialist-contact check (REQ-SPEC-2).
        builder.HasIndex(r => r.SpecialistId);

        builder.HasIndex(r => r.GrowerId);

        builder.Property(r => r.AlertId)
            .HasColumnName("alert_id");

        builder.Property(r => r.Reason)
            .HasColumnName("reason")
            .IsRequired();

        builder.Property(r => r.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.DeclineReason)
            .HasColumnName("decline_reason");
    }
}
