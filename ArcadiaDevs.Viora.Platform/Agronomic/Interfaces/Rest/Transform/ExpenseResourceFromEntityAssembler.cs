using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Converts Expense aggregate to ExpenseResource DTO.
/// </summary>
public static class ExpenseResourceFromEntityAssembler
{
    public static ExpenseResource ToResourceFromEntity(Expense entity)
    {
        return new ExpenseResource(
            entity.Id,
            entity.GrowerId,
            entity.PlotId,
            entity.Type.ToString(),
            entity.Category.ToString(),
            entity.LinkedActionCode,
            entity.Amount,
            entity.Currency,
            entity.ExpenseDate,
            entity.PaymentStatus.ToString(),
            entity.Note,
            entity.Status.ToString());
    }
}
