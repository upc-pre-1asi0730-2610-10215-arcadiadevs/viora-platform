using System.Linq;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the ServiceProposal aggregate root.
///     Explicit snake_case naming mirrors <c>InterventionRequestConfiguration</c>.
/// </summary>
public class ServiceProposalConfiguration : IEntityTypeConfiguration<ServiceProposal>
{
    public void Configure(EntityTypeBuilder<ServiceProposal> builder)
    {
        builder.ToTable("service_proposals");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(sp => sp.InterventionRequestId)
            .HasColumnName("intervention_request_id")
            .IsRequired();

        // Non-unique: multiple proposals may be submitted against the same
        // request before one is accepted (design, obs #267).
        builder.HasIndex(sp => sp.InterventionRequestId);

        builder.Property(sp => sp.SpecialistId)
            .HasColumnName("specialist_id")
            .IsRequired();

        builder.Property(sp => sp.ServiceTitle)
            .HasColumnName("service_title")
            .IsRequired();

        builder.Property(sp => sp.DurationLabel)
            .HasColumnName("duration_label")
            .IsRequired();

        // Scope is a structured list of itemized bullets (2026-07-05 field-level
        // parity fix — previously an opaque single string), JSON-serialized
        // mirroring the Products-as-JSON precedent on TreatmentPrescription.
        var scopeComparer = new ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(sp => sp.Scope)
            .HasColumnName("scope")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                    ?? new List<string>(),
                scopeComparer)
            .IsRequired();

        builder.Property(sp => sp.ProposedDate)
            .HasColumnName("proposed_date")
            .IsRequired();

        // CostEstimate is a 2-field VO — mapped as an owned entity type
        // (mirrors PlotConfiguration's OwnsOne precedent), not a single
        // HasConversion column, since it carries Amount + Currency.
        builder.OwnsOne(sp => sp.CostEstimate, costBuilder =>
        {
            costBuilder.Property<int>("ServiceProposalId").HasColumnName("id");

            costBuilder.Property(c => c.Amount)
                .HasColumnName("cost_estimate_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            costBuilder.Property(c => c.Currency)
                .HasColumnName("cost_estimate_currency")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Navigation(sp => sp.CostEstimate).IsRequired();

        builder.Property(sp => sp.ProposalDetails)
            .HasColumnName("proposal_details")
            .IsRequired();

        builder.Property(sp => sp.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}
