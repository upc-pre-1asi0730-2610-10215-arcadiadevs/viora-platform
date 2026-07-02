using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

/// <summary>
///     Application contract for expense queries.
/// </summary>
public interface IExpenseQueryService
{
    Task<IEnumerable<Expense>> Handle(
        GetGrowerExpensesQuery query,
        CancellationToken cancellationToken = default);

    Task<Expense?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default);
}
