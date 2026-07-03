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

/// <summary>
///     REST controller for profile operations.
/// </summary>
[ApiController]
[Route("api/v1/profiles")]
[Produces("application/json")]
public class ProfilesController(
    IProfileCommandService profileCommandService,
    IProfileQueryService profileQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets a profile by user id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>200 with the profile, or 404 if not found.</returns>
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

    /// <summary>
    ///     Creates or updates a profile (PUT upsert).
    /// </summary>
    /// <remarks>
    ///     Returns 201 when a new profile is created, 200 when an existing
    ///     profile is updated. Role is immutable after creation.
    /// </remarks>
    /// <param name="userId">The user id from the route.</param>
    /// <param name="resource">The upsert resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201/200 with the profile, or error details.</returns>
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
