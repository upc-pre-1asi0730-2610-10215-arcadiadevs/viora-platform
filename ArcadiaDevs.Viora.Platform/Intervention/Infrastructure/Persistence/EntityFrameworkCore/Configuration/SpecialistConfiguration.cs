using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
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

        var tagsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());

        builder.Property(s => s.Tags)
            .HasColumnName("tags")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v.Items, (System.Text.Json.JsonSerializerOptions?)null),
                v => new SpecialistTags(
                    System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                    ?? new List<string>()));
    }
}
