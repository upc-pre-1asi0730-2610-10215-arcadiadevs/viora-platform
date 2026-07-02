using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to create a new expense record.
/// </summary>
public sealed record CreateExpenseCommand(
    long GrowerId,
    long PlotId,
    ExpenseType Type,
    ExpenseCategory Category,
    string? LinkedActionCode,
    decimal Amount,
    string? Currency,
    DateOnly ExpenseDate,
    PaymentStatus? PaymentStatus,
    string? Note,
    ExpenseStatus? Status);
