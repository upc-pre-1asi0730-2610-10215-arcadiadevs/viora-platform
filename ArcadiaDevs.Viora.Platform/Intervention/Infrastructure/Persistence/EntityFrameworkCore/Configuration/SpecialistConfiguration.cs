using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Specialist aggregate root.
///     Explicit snake_case naming mirrors <c>ProfileConfiguration</c>.
/// </summary>
public class SpecialistConfiguration : IEntityTypeConfiguration<Specialist>
{
    public void Configure(EntityTypeBuilder<Specialist> builder)
    {
        builder.ToTable("specialists");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.ProfileUserId)
            .HasColumnName("profile_user_id")
            .IsRequired();

        // REQ idempotency (design, obs #267): one Specialist per Profile UserId.
        builder.HasIndex(s => s.ProfileUserId)
            .IsUnique();

        builder.Property(s => s.SuccessRate)
            .HasColumnName("success_rate")
            .IsRequired();

        builder.Property(s => s.CaseCount)
            .HasColumnName("case_count")
            .IsRequired();

        builder.Property(s => s.DistanceKm)
            .HasColumnName("distance_km")
            .IsRequired();

        builder.Property(s => s.Availability)
            .HasColumnName("availability")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.Whatsapp)
            .HasColumnName("whatsapp")
            .HasMaxLength(50);

        // Compares by tag content, not by SpecialistTags record reference — the
        // record's auto-generated equality would otherwise fall back to
        // reference equality on the underlying IReadOnlyList<string>, causing
        // EF's change tracker to always see the property as "modified".
        var tagsComparer = new ValueComparer<SpecialistTags>(
            (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Items.SequenceEqual(c2.Items)),
            c => c.Items.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => new SpecialistTags(c.Items.ToList()));

        builder.Property(s => s.Tags)
            .HasColumnName("tags")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v.Items, (System.Text.Json.JsonSerializerOptions?)null),
                v => new SpecialistTags(
                    System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                    ?? new List<string>()),
                tagsComparer);
    }
}
