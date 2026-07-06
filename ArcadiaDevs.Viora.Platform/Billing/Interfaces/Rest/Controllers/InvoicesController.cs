using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for invoices (REQ-INV-3). List-only — invoices are
///     created exclusively via WU6's internal webhook reconciliation
///     (REQ-INV-1, REQ-INV-2), matching OS. Only the list endpoint is
///     exposed: spec's REQ-CC-1 route table and REQ-INV-3 both only require
///     <c>GET /invoices?userId=</c>, so no <c>GET /invoices/{id}</c> endpoint
///     is added in this slice (same reasoning as WU1's Plan deviation —
///     followed the gate-passed spec/tasks over a broader design REST-table
///     mention).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InvoicesController(
    IInvoiceQueryService invoiceQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Lists the invoices belonging to a user (REQ-INV-3).
    /// </summary>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Invoices for the user (possibly empty).</response>
    /// <response code="404">Unknown user (REQ-CC-2).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InvoiceResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoicesByUserId(
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await invoiceQueryService.Handle(new GetInvoicesByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            invoices => Ok(invoices.Select(InvoiceResourceFromEntityAssembler.ToResourceFromEntity).ToList()));
    }
}
