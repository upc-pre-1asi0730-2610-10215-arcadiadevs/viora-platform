using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Handles expense query operations.
/// </summary>
public class ExpenseQueryService : IExpenseQueryService
{
    private readonly IExpenseRepository _repository;

    public ExpenseQueryService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Expense>> Handle(
        GetGrowerExpensesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.PlotId.HasValue)
            return await _repository.FindByPlotIdAsync(query.PlotId.Value, cancellationToken);

        return await _repository.FindByGrowerIdAsync(query.GrowerId, cancellationToken);
    }

    public async Task<Expense?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await _repository.FindByIdAsync(id, cancellationToken);
    }
}
