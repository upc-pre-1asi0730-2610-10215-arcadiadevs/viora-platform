using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
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

        builder.Property(s => s.Whatsapp)
            .HasColumnName("whatsapp")
            .HasMaxLength(50);
    }
}
