using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the PaymentMethod aggregate root.
///     Explicit snake_case naming, table <c>payment_methods</c>, unique
///     index on <c>user_id</c> (REQ-PM-2 — a single reused row per user).
/// </summary>
public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // REQ-PM-2: single reused row per user (upsert, not insert-per-payment).
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Brand)
            .HasColumnName("brand")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Last4)
            .HasColumnName("last4")
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(p => p.ExpMonth)
            .HasColumnName("exp_month")
            .IsRequired();

        builder.Property(p => p.ExpYear)
            .HasColumnName("exp_year")
            .IsRequired();

        builder.Property(p => p.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();
    }
}