using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

public static class AlertResourceFromEntityAssembler
{
    public static AlertResource ToResourceFromEntity(Alert entity)
    {
        var timeline = entity.Timeline.Select(t => new AlertTimelineRecordResource(
            t.Tag,
            t.Title,
            t.Description,
            t.CreatedAt
        )).ToList();

        return new AlertResource(
            entity.Id,
            entity.PlotId.Value,
            entity.Type.ToString(),
            entity.Severity.ToString(),
            entity.Status,
            entity.Title,
            entity.RiskExplanation,
            entity.Sources.ToList(),
            entity.DataProviders.ToList(),
            entity.SupportingData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            timeline
        );
    }
}
