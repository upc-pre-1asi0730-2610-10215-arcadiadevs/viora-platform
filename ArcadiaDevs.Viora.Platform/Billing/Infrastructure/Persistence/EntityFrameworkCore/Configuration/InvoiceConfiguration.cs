using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Invoice aggregate root. Explicit
///     snake_case naming, table <c>invoices</c>, unique index on
///     <c>external_payment_id</c> — nullable-safe (REQ-INV-1): the column is
///     optional (<see cref="Invoice.ExternalPaymentId" /> is <c>string?</c>),
///     and standard SQL/Postgres unique-index semantics already permit
///     multiple <c>NULL</c> rows (NULL is never considered equal to NULL),
///     so no additional filtered-index configuration is required beyond
///     leaving the column nullable.
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(i => i.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(i => i.IssuedAt)
            .HasColumnName("issued_at")
            .IsRequired();

        builder.Property(i => i.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(i => i.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.ExternalPaymentId)
            .HasColumnName("external_payment_id")
            .HasMaxLength(100)
            .IsRequired(false);

        // REQ-INV-1: unique when present, nullable-safe.
        builder.HasIndex(i => i.ExternalPaymentId)
            .IsUnique();
    }
}
