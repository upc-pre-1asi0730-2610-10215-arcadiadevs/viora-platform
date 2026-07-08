using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class CheckoutsController(
    ICheckoutCommandService checkoutCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
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
            return BadRequest(new
            {
                error = $"Invalid interval '{resource.Interval}'. Valid values: {string.Join(", ", Enum.GetNames<PlanInterval>())}."
            });
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
