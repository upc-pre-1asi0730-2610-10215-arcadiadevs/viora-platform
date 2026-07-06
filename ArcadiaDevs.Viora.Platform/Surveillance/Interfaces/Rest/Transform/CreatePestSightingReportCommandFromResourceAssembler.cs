using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

/// <summary>
/// Assembler to map a <see cref="CreatePestSightingReportResource"/> to a <see cref="CreatePestSightingReportCommand"/>.
/// </summary>
public static class CreatePestSightingReportCommandFromResourceAssembler
{
    /// <summary>
    /// Transforms the resource into a command.
    /// </summary>
    /// <param name="resource">The incoming REST resource.</param>
    /// <param name="reporterUserId">
    ///     The authenticated caller's id, derived from the token — takes precedence
    ///     over <see cref="CreatePestSightingReportResource.ReporterUserId"/>, which
    ///     is client-supplied and therefore not trusted for identity.
    /// </param>
    /// <returns>The generated command.</returns>
    public static CreatePestSightingReportCommand ToCommandFromResource(
        CreatePestSightingReportResource resource,
        long reporterUserId)
    {
        return new CreatePestSightingReportCommand(
            resource.PlotId,
            reporterUserId,
            resource.RiskZone,
            resource.Symptoms,
            resource.ObservedSeverity,
            resource.Notes
        );
    }
}
