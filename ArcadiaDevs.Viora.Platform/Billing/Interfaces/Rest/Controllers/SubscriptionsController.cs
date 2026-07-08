using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
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
public class SubscriptionsController(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionQueryService subscriptionQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(SubscriptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionByUserId(
        [FromRoute] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await subscriptionQueryService.Handle(new GetSubscriptionByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            subscription => Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription)));
    }

    [HttpPatch("{userId:int}")]
    [ProducesResponseType(typeof(SubscriptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSubscription(
        [FromRoute] int userId,
        [FromBody] UpdateSubscriptionResource resource,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(resource.Status, "CANCELED", StringComparison.OrdinalIgnoreCase))
        {
            var result = await subscriptionCommandService.Handle(new CancelSubscriptionCommand(userId), cancellationToken);

            return BillingActionResultAssembler.ToActionResult(
                this,
                result,
                errorLocalizer,
                problemDetailsFactory,
                subscription => Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription)));
        }

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            HttpContext,
            StatusCodes.Status400BadRequest,
            BillingErrors.ValidationError.Code,
            errorLocalizer[BillingErrors.ValidationError.Code].Value ?? BillingErrors.ValidationError.Message);
        return BadRequest(problemDetails);
    }
}
