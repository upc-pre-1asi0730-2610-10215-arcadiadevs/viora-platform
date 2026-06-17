using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/symptom-dictionary-items")]
[Produces(MediaTypeNames.Application.Json)]
public class SymptomDictionaryItemsController(ISymptomQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSymptoms([FromHeader(Name = "Accept-Language")] string language = "en")
    {
        var query = new GetAllSymptomsQuery();
        var symptoms = await queryService.Handle(query);

        var resources = symptoms.Select(entity => SymptomResourceFromEntityAssembler.ToResourceFromEntity(entity, language)).ToList();

        return Ok(resources);
    }
}
