using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Converts CreateExpenseResource DTO to CreateExpenseCommand.
/// </summary>
public static class CreateExpenseCommandFromResourceAssembler
{
    public static CreateExpenseCommand ToCommandFromResource(CreateExpenseResource resource)
    {
        return new CreateExpenseCommand(
            resource.GrowerId,
            resource.PlotId,
            Enum.Parse<ExpenseType>(resource.Type, true),
            Enum.Parse<ExpenseCategory>(resource.Category, true),
            resource.LinkedActionCode,
            resource.Amount,
            resource.Currency,
            resource.ExpenseDate,
            resource.PaymentStatus != null ? Enum.Parse<PaymentStatus>(resource.PaymentStatus, true) : null,
            resource.Note,
            resource.Status != null ? Enum.Parse<ExpenseStatus>(resource.Status, true) : null);
    }
}
