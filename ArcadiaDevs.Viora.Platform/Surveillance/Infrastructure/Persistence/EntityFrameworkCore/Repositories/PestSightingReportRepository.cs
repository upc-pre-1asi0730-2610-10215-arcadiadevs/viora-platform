using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="IPestSightingReportRepository"/>.
/// </summary>
public class PestSightingReportRepository(AppDbContext context)
    : BaseRepository<PestSightingReport>(context), IPestSightingReportRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<PestSightingReport>> FindByReporterUserIdAsync(long reporterUserId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<PestSightingReport>()
            .Where(r => r.ReporterUserId.Value == reporterUserId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
