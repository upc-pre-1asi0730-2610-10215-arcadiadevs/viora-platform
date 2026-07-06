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

/// <summary>
///     REST controller for subscriptions (REQ-SUB-2, REQ-SUB-4).
/// </summary>
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
    /// <summary>
    ///     Gets a user's subscription (REQ-SUB-4).
    /// </summary>
    /// <remarks>
    ///     404 either when the user is unknown to IAM (REQ-CC-2) or when the
    ///     user has no subscription — both surface identically as 404.
    /// </remarks>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Subscription found.</response>
    /// <response code="404">Unknown user, or the user has no subscription.</response>
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

    /// <summary>
    ///     Cancels a user's subscription (REQ-SUB-2).
    /// </summary>
    /// <remarks>
    ///     The only supported target <c>status</c> value is <c>CANCELED</c>
    ///     — plan-switch (REQ-SUB-3) stays internal-only, no public
    ///     endpoint. Self-guarded on the aggregate: only succeeds from
    ///     <c>ACTIVE</c> (409 otherwise, including an already-<c>CANCELED</c>
    ///     subscription).
    /// </remarks>
    /// <param name="userId">The user id.</param>
    /// <param name="resource">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Subscription canceled.</response>
    /// <response code="400">Missing/unsupported <c>status</c> value.</response>
    /// <response code="404">Unknown user, or the user has no subscription.</response>
    /// <response code="409">The subscription is not currently <c>ACTIVE</c>.</response>
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
