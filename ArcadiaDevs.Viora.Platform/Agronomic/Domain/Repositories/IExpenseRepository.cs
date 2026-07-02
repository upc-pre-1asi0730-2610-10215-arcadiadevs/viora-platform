using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

/// <summary>
///     Repository contract for the Expense aggregate.
///     Standalone interface (does NOT extend IBaseRepository&lt;T&gt;) per R15.
/// </summary>
public interface IExpenseRepository
{
    Task<Expense?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

    Task SaveAsync(Expense expense, CancellationToken cancellationToken = default);

    Task<IEnumerable<Expense>> FindByGrowerIdAsync(long growerId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Expense>> FindByPlotIdAsync(long plotId, CancellationToken cancellationToken = default);
}
