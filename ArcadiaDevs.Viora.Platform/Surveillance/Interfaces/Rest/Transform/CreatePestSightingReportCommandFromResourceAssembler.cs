using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for creating checkout sessions (REQ-GATE-3). Ships
///     with WU5 alongside the <c>IPaymentGateway</c> port/adapter — despite
///     WU6's branch name "checkout-and-webhook", the checkout endpoint
///     itself lives here (design's WU5/WU6 sequencing note, tasks obs #319).
///     Returns 503 (<c>BillingErrors.PaymentGatewayNotConfigured</c>) until
///     the adapter is configured.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class CheckoutsController(
    ICheckoutCommandService checkoutCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Creates a checkout session for a user and target plan (REQ-GATE-3).
    /// </summary>
    /// <param name="resource">The checkout payload.</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Checkout session created.</response>
    /// <response code="400">Missing/unsupported <c>interval</c> value.</response>
    /// <response code="404">Unknown <c>planCode</c>.</response>
    /// <response code="503">The payment gateway is not configured (REQ-GATE-2).</response>
    [HttpPost]
    [ProducesResponseType(typeof(CheckoutSessionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateCheckout(
        [FromBody] CreateCheckoutResource resource,
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PlanInterval>(resource.Interval, ignoreCase: true, out var interval))
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                BillingErrors.ValidationError.Code,
                errorLocalizer[BillingErrors.ValidationError.Code].Value ?? BillingErrors.ValidationError.Message);
            return BadRequest(problemDetails);
        }

        var command = new CreateCheckoutCommand(userId, resource.PlanCode, interval);

        var result = await checkoutCommandService.Handle(command, cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            session => StatusCode(
                StatusCodes.Status201Created,
                CheckoutSessionResourceFromEntityAssembler.ToResourceFromEntity(session)));
    }
}
