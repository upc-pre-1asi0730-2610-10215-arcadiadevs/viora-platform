using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the Billing bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the Billing
///     BC is applied explicitly here so the BC owns its EF Core mapping,
///     mirroring Intervention's <c>ModelBuilderExtensions</c>. WU1 registers
///     <c>PlanConfiguration</c>; WU2 adds <c>SubscriptionConfiguration</c>;
///     WU3 adds <c>PaymentMethodConfiguration</c>; WU4 adds
///     <c>InvoiceConfiguration</c>; WU7 adds <c>CouponConfiguration</c>; WU8
///     adds <c>ReferralCodeConfiguration</c> (per-slice migrations, design's
///     EF Core Design section).
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every Billing <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    public static void ApplyBillingConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new PlanConfiguration());
        builder.ApplyConfiguration(new SubscriptionConfiguration());
    }
}
