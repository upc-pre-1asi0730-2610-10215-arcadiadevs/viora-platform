using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

/// <summary>
/// Assembler to map a <see cref="PestSightingReport"/> entity to a <see cref="PestSightingReportResource"/>.
/// </summary>
public static class PestSightingReportResourceFromEntityAssembler
{
    /// <summary>
    /// Transforms the entity into a resource.
    /// </summary>
    /// <param name="entity">The aggregate root entity.</param>
    /// <returns>The mapped resource.</returns>
    public static PestSightingReportResource ToResourceFromEntity(PestSightingReport entity)
    {
        return new PestSightingReportResource(
            entity.Id,
            entity.PlotId.Value,
            entity.ReporterUserId.Value,
            entity.RiskZone.ToString(),
            entity.Symptoms.GetDescriptions().ToList(),
            entity.ObservedSeverity.ToString(),
            entity.Notes,
            entity.Evaluated,
            entity.CalculatedRisk.ToString(),
            entity.ProbableThreat.ToString(),
            entity.Status.ToString(),
            entity.AlertConfirmed
        );
    }
}
