using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Coupon aggregate root. Explicit
///     snake_case naming, table <c>coupons</c>, composite unique index on
///     <c>(user_id, code)</c> — per-user idempotency, not global (REQ-COUP-2).
/// </summary>
public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        // REQ-COUP-2: per-user idempotency — a different user CAN redeem the
        // same code, so the guard is composite, not a plain unique on Code.
        builder.HasIndex(c => new { c.UserId, c.Code })
            .IsUnique();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.DiscountPercent)
            .HasColumnName("discount_percent")
            .IsRequired();

        builder.Property(c => c.ValidUntil)
            .HasColumnName("valid_until");

        builder.Property(c => c.Conditions)
            .HasColumnName("conditions")
            .HasMaxLength(500)
            .IsRequired();
    }
}