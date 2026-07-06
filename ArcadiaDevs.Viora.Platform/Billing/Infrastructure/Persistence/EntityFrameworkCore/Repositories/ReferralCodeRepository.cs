using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IReferralCodeRepository" />.
/// </summary>
public class ReferralCodeRepository(AppDbContext context)
    : BaseRepository<ReferralCode>(context), IReferralCodeRepository
{
    public async Task<ReferralCode?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<ReferralCode>()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<ReferralCode>()
            .AnyAsync(r => r.Code == code, cancellationToken);
    }

    public async Task<ReferralCode?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<ReferralCode>()
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
    }
}