using ArcadiaDevs.Viora.Platform.Profile.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/profiles")]
[Produces("application/json")]
public class ProfilesController(
    IProfileCommandService profileCommandService,
    IProfileQueryService profileQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var profile = await profileQueryService.Handle(
            new GetProfileByUserIdQuery(userId), cancellationToken);

        if (profile is null)
            return NotFound();

        return Ok(profile.ToResource());
    }

    [HttpPut("{userId:int}")]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProfileResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdateProfile(
        [FromRoute] int userId,
        [FromBody] CreateOrUpdateProfileResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand(userId);
        var result = await profileCommandService.Handle(command, cancellationToken);

        return ProfileActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            tuple =>
            {
                var profileResource = tuple.Profile.ToResource();
                return tuple.Created
                    ? Created($"/api/v1/profiles/{userId}", profileResource)
                    : Ok(profileResource);
            });
    }
}
