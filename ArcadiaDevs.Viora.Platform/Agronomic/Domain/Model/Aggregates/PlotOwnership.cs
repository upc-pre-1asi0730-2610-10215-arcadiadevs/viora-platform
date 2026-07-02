namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

public partial class Plot
{
    public bool BelongsTo(int userId) => OwnerUserId == userId;
}
