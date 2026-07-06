using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     The verification-token repository.
/// </summary>
public class VerificationTokenRepository(AppDbContext context)
    : BaseRepository<VerificationToken>(context), IVerificationTokenRepository
{
    /// <inheritdoc />
    public async Task<VerificationToken?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return await Context.Set<VerificationToken>()
            .FirstOrDefaultAsync(vt => vt.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VerificationToken>> FindByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await Context.Set<VerificationToken>()
            .Where(vt => vt.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
