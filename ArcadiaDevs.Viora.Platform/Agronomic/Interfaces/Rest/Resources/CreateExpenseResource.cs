namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     REST request DTO for creating an expense record.
/// </summary>
public sealed record CreateExpenseResource(
    long GrowerId,
    long PlotId,
    string Type,
    string Category,
    string? LinkedActionCode,
    decimal Amount,
    string? Currency,
    DateOnly ExpenseDate,
    string? PaymentStatus,
    string? Note,
    string? Status);
