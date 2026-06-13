using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

/// <summary>
/// Repository contract for agronomic statistics.
/// </summary>
public interface IAgronomicStatisticRepository
{
    /// <summary>
    /// Finds the most recent agronomic statistics for a given user within a specified date range.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of the most recent agronomic statistics.</returns>
    Task<IReadOnlyList<AgronomicStatistic>> FindLatestByUserIdAsync(
        int userId, 
        DateTimeOffset startDate, 
        DateTimeOffset endDate, 
        CancellationToken cancellationToken = default);
}