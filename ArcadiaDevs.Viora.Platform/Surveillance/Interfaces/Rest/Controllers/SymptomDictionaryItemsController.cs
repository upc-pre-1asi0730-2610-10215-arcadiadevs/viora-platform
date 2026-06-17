using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/symptom-dictionary-items")]
[Produces(MediaTypeNames.Application.Json)]
public class SymptomDictionaryItemsController(
    ISymptomQueryService queryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets the available symptom dictionary items.
    /// </summary>
    /// <param name="language">The language for symptom translation (e.g., 'en', 'es').</param>
    /// <returns>200 OK with the list of symptoms, or 400 Bad Request with error details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources.SymptomResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSymptoms([FromHeader(Name = "Accept-Language")] string language = "en")
    {
        var query = new GetAllSymptomsQuery();
        var symptoms = await queryService.Handle(query);

        return SurveillanceActionResultAssembler.ToActionResultFromGetAllSymptomsResult(
            this,
            symptoms,
            errorLocalizer,
            problemDetailsFactory,
            entities =>
            {
                var resources = entities.Select(entity => SymptomResourceFromEntityAssembler.ToResourceFromEntity(entity, language)).ToList();
                return Ok(resources);
            }
        );
    }
}
