using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the VerificationToken aggregate root.
/// </summary>
public class VerificationTokenConfiguration : IEntityTypeConfiguration<VerificationToken>
{
    public void Configure(EntityTypeBuilder<VerificationToken> builder)
    {
        builder.ToTable("verification_tokens");

        builder.HasKey(vt => vt.Id);

        builder.Property(vt => vt.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(vt => vt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(vt => vt.Token)
            .HasColumnName("token")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(vt => vt.Token)
            .IsUnique();

        builder.Property(vt => vt.Purpose)
            .HasColumnName("purpose")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(vt => vt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(vt => vt.Consumed)
            .HasColumnName("consumed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(vt => vt.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(vt => vt.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
