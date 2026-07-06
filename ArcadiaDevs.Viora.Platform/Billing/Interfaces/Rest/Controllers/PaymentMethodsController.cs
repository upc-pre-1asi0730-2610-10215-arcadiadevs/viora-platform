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
///     REST controller for payment methods (REQ-PM-3). List-only — writes
///     happen exclusively via WU6's internal webhook reconciliation
///     (REQ-PM-2), matching OS.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class PaymentMethodsController(
    IPaymentMethodQueryService paymentMethodQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Lists the payment method(s) belonging to a user (REQ-PM-3).
    /// </summary>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Payment methods for the user (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaymentMethodResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentMethodsByUserId(
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentMethodQueryService.Handle(new GetPaymentMethodsByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            paymentMethods => Ok(paymentMethods.Select(PaymentMethodResourceFromEntityAssembler.ToResourceFromEntity).ToList()));
    }
}
