using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="IPestSightingReportRepository"/>.
/// </summary>
public class PestSightingReportRepository(AppDbContext context)
    : BaseRepository<PestSightingReport>(context), IPestSightingReportRepository
{
}
