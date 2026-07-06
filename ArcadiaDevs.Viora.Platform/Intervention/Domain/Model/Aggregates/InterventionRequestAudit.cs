using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

public partial class InterventionRequest : IAuditableEntity
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
