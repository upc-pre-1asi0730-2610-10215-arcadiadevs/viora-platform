using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="ICouponRepository" />.
/// </summary>
public class CouponRepository(AppDbContext context)
    : BaseRepository<Coupon>(context), ICouponRepository
{
    public async Task<bool> ExistsByUserIdAndCodeAsync(int userId, string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Coupon>()
            .AnyAsync(c => c.UserId == userId && c.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Coupon>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Coupon>()
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}