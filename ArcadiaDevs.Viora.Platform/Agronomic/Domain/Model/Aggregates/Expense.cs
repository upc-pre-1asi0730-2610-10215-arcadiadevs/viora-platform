using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     Represents a single expense record (OS parity: data holder, no business methods).
/// </summary>
public class Expense
{
    public long Id { get; private set; }

    public long GrowerId { get; private set; }

    public long PlotId { get; private set; }

    public ExpenseType Type { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public string? LinkedActionCode { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "PEN";

    public DateOnly ExpenseDate { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Paid;

    public string? Note { get; private set; }

    public ExpenseStatus Status { get; private set; } = ExpenseStatus.Registered;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    private Expense() { }

    public Expense(CreateExpenseCommand command)
    {
        if (command.GrowerId <= 0)
            throw new ArgumentException("Grower ID must be positive.");
        if (command.PlotId <= 0)
            throw new ArgumentException("Plot ID must be positive.");
        if (command.Amount <= 0)
            throw new ArgumentException("Amount must be positive.");

        GrowerId = command.GrowerId;
        PlotId = command.PlotId;
        Type = command.Type;
        Category = command.Category;
        LinkedActionCode = command.LinkedActionCode;
        Amount = command.Amount;
        Currency = !string.IsNullOrWhiteSpace(command.Currency) ? command.Currency : "PEN";
        ExpenseDate = command.ExpenseDate;
        PaymentStatus = command.PaymentStatus ?? PaymentStatus.Paid;
        Note = command.Note;
        Status = command.Status ?? ExpenseStatus.Registered;
    }
}
