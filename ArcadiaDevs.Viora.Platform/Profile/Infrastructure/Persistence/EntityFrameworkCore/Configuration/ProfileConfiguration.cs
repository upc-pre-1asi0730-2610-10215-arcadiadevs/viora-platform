using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Profile aggregate root.
/// </summary>
/// <remarks>
///     Follows UserConfiguration's explicit-naming style. The Role enum is
///     stored as a string (max 20 chars) matching the WeatherStatus precedent
///     in the Agronomic BC.
/// </remarks>
public class ProfileConfiguration : IEntityTypeConfiguration<ProfileAggregate>
{
    /// <summary>
    ///     Configures the Profile entity type.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ProfileAggregate> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Phone)
            .HasColumnName("phone")
            .HasMaxLength(50);

        builder.Property(p => p.JobTitle)
            .HasColumnName("job_title")
            .HasMaxLength(100);

        builder.Property(p => p.Language)
            .HasColumnName("language")
            .HasMaxLength(10);

        builder.Property(p => p.Location)
            .HasColumnName("location")
            .HasMaxLength(100);

        builder.Property(p => p.SpecialtyArea)
            .HasColumnName("specialty_area")
            .HasMaxLength(100);
    }
}
