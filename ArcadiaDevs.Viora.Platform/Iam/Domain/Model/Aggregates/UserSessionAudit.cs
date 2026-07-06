using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

public partial class UserSession : IAuditableEntity
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
