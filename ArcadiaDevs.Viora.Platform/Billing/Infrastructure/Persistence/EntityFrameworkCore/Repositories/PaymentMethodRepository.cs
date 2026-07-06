using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IPaymentMethodRepository" />.
/// </summary>
public class PaymentMethodRepository(AppDbContext context)
    : BaseRepository<PaymentMethod>(context), IPaymentMethodRepository
{
    public async Task<PaymentMethod?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}