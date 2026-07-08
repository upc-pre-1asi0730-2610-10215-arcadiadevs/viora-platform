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

[ApiController]
[Route("api/v1/referrals")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class ReferralCodesController(
    IReferralCodeQueryService referralCodeQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(ReferralCodeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateReferralCode(
        [FromRoute] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await referralCodeQueryService.Handle(
            new GetOrCreateReferralCodeByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            referralCode => Ok(ReferralCodeResourceFromEntityAssembler.ToResourceFromEntity(referralCode)));
    }
}
