namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     REST response DTO for an expense record.
/// </summary>
public sealed record ExpenseResource(
    long Id,
    long GrowerId,
    long PlotId,
    string Type,
    string Category,
    string? LinkedActionCode,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    string PaymentStatus,
    string? Note,
    string Status);
