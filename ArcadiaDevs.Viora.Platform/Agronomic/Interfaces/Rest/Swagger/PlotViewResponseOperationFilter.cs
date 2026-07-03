using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Swagger;

/// <summary>
///     Emits a `oneOf` 200 response schema for actions with more than one
///     `[ProducesResponseType(200)]` declaration, so the `?view=`-dispatched
///     Plots endpoints document their per-view response shapes instead of
///     collapsing to a single arbitrary type in the generated OpenAPI schema.
/// </summary>
/// <remarks>
///     Reads the raw `[ProducesResponseType]` attributes off the action's
///     <see cref="System.Reflection.MethodInfo"/> rather than
///     <c>context.ApiDescription.SupportedResponseTypes</c> — ASP.NET Core's
///     ApiExplorer collapses multiple attributes sharing the same status code
///     down to the last one declared, so by the time an operation filter sees
///     <c>SupportedResponseTypes</c> the other 200-status types are already gone.
/// </remarks>
public class PlotViewResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var okResponseTypes = context.MethodInfo
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .OfType<ProducesResponseTypeAttribute>()
            .Where(attribute => attribute.StatusCode == StatusCodes.Status200OK && attribute.Type != typeof(void))
            .Select(attribute => attribute.Type)
            .Distinct()
            .ToList();

        if (okResponseTypes.Count < 2)
            return;

        if (!operation.Responses.TryGetValue("200", out var response))
            return;

        if (!response.Content.TryGetValue("application/json", out var mediaType))
            return;

        var schemas = okResponseTypes
            .Select(type => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository))
            .ToList();

        mediaType.Schema = new OpenApiSchema { OneOf = schemas };
    }
}
