using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/expenses")]
[Produces("application/json")]
[Authorize]
public class ExpensesController(
    IExpenseCommandService expenseCommandService,
    IExpenseQueryService expenseQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExpenseResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpenses(
        [FromToken] long growerId,
        [FromQuery] long? plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetGrowerExpensesQuery(growerId, plotId);
        var expenses = await expenseQueryService.Handle(query, cancellationToken);

        var resources = expenses.Select(ExpenseResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ExpenseResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpenseById(
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var expense = await expenseQueryService.FindByIdAsync(id, cancellationToken);

        if (expense == null)
            return NotFound(new ProblemDetails { Title = "Expense not found" });

        return Ok(ExpenseResourceFromEntityAssembler.ToResourceFromEntity(expense));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateExpense(
        [FromBody] CreateExpenseResource resource,
        CancellationToken cancellationToken)
    {
        var command = CreateExpenseCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await expenseCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            expense => Created($"/api/v1/expenses/{expense.Id}",
                ExpenseResourceFromEntityAssembler.ToResourceFromEntity(expense)));
    }
}
