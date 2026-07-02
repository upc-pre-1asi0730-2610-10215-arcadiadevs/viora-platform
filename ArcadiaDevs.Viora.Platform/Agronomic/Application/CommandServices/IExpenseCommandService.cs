using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;

/// <summary>
///     Application contract for expense commands.
/// </summary>
public interface IExpenseCommandService
{
    Task<Result<Expense, Error>> Handle(
        CreateExpenseCommand command,
        CancellationToken cancellationToken = default);
}
