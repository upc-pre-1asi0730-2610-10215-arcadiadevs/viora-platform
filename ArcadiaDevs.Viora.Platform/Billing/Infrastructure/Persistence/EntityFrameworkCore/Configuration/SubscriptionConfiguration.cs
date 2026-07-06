using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Subscription aggregate root.
///     Explicit snake_case naming, table <c>subscriptions</c>, unique index
///     on <c>user_id</c> (REQ-SUB-1 — one subscription per user).
/// </summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // REQ-SUB-1: one subscription per user.
        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.PlanCode)
            .HasColumnName("plan_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.Interval)
            .HasColumnName("interval")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnName("current_period_end")
            .IsRequired();
    }
}