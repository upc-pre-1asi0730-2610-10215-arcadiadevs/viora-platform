using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IInvoiceRepository" />.
/// </summary>
public class InvoiceRepository(AppDbContext context)
    : BaseRepository<Invoice>(context), IInvoiceRepository
{
    public async Task<Invoice?> FindByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.ExternalPaymentId == externalPaymentId, cancellationToken);
    }

    public async Task<IEnumerable<Invoice>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Invoice>()
            .Where(i => i.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}