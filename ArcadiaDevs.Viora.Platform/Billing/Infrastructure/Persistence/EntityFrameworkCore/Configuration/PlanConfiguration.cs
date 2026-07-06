using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Plan aggregate root. Explicit
///     snake_case naming, table <c>plans</c>, unique index on
///     <c>code</c> (REQ-PLAN-1). <c>Features</c> uses the same
///     JSON-conversion + <see cref="ValueComparer{T}" /> pattern as
///     <c>SpecialistConfiguration.Tags</c>/<c>AgrochemicalPrescription.Products</c>.
/// </summary>
public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        // REQ-PLAN-1: idempotent startup seed relies on this unique index.
        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.PriceAmount)
            .HasColumnName("price_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Interval)
            .HasColumnName("interval")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Tagline)
            .HasColumnName("tagline")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.PlotLimit)
            .HasColumnName("plot_limit")
            .IsRequired();

        builder.Property(p => p.IotLimit)
            .HasColumnName("iot_limit")
            .IsRequired();

        // Compares by feature content, not by list reference — the default
        // reference equality would otherwise leave EF's change tracker
        // permanently seeing the property as "modified" (same rationale as
        // SpecialistConfiguration.Tags).
        var featuresComparer = new ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(p => p.Features)
            .HasColumnName("features")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                    ?? new List<string>(),
                featuresComparer);
    }
}
