using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Handles expense creation commands (R12: catches ArgumentException from aggregate constructor).
/// </summary>
public class ExpenseCommandService : IExpenseCommandService
{
    private readonly IExpenseRepository _repository;

    public ExpenseCommandService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Expense, Error>> Handle(
        CreateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = new Expense(command);
            await _repository.SaveAsync(expense, cancellationToken);
            return new Result<Expense, Error>.Success(expense);
        }
        catch (ArgumentException ex)
        {
            return new Result<Expense, Error>.Failure(
                new Error("VALIDATION_ERROR", ex.Message));
        }
    }
}
