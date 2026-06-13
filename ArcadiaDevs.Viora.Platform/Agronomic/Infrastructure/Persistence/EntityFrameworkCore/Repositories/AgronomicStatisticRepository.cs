using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// EF Core implementation of the agronomic statistic repository.
/// </summary>
public class AgronomicStatisticRepository : IAgronomicStatisticRepository
{
    private readonly AppDbContext _context;

    public AgronomicStatisticRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AgronomicStatistic>> FindLatestByUserIdAsync(
        int userId, 
        DateTimeOffset startDate, 
        DateTimeOffset endDate, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AgronomicStatistics
            .Where(s => s.UserId == userId && s.MeasurementDate >= startDate && s.MeasurementDate <= endDate)
            .AsNoTracking() // Use AsNoTracking for read-only queries to improve performance
            .ToListAsync(cancellationToken);
    }
}