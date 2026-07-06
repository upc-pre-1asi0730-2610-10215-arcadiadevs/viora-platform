using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the ReferralCode aggregate root.
///     Explicit snake_case naming, table <c>referral_codes</c>, unique index
///     on <c>user_id</c> (REQ-REF-1) and unique index on <c>code</c>
///     (REQ-REF-2). <see cref="ReferralCode.RewardPercent" /> is a compile-time
///     constant, not an instance property, so it is not mapped here.
/// </summary>
public class ReferralCodeConfiguration : IEntityTypeConfiguration<ReferralCode>
{
    public void Configure(EntityTypeBuilder<ReferralCode> builder)
    {
        builder.ToTable("referral_codes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // REQ-REF-1: one referral code per user.
        builder.HasIndex(r => r.UserId)
            .IsUnique();

        builder.Property(r => r.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        // REQ-REF-2: codes are globally unique (loop-until-unique generation).
        builder.HasIndex(r => r.Code)
            .IsUnique();
    }
}