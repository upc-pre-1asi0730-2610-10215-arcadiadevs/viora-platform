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
    /// <returns>The generated command.</returns>
    public static CreatePestSightingReportCommand ToCommandFromResource(CreatePestSightingReportResource resource)
    {
        return new CreatePestSightingReportCommand(
            resource.PlotId,
            resource.ReporterUserId,
            resource.RiskZone,
            resource.Symptoms,
            resource.ObservedSeverity,
            resource.Notes
        );
    }
}
