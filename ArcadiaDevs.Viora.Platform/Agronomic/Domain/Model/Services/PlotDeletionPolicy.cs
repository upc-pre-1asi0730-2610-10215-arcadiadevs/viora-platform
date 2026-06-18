using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Domain service that defines whether a plot can be deleted
///     and whether it should be physically removed or logically deactivated.
/// </summary>
public class PlotDeletionPolicy
{
    /// <summary>
    ///     Checks whether the plot can be deleted.
    /// </summary>
    public bool CanDelete(Plot? plot)
    {
        return plot is { IsActive: true };
    }

    /// <summary>
    ///     Determines whether the plot should be logically deleted instead of physically removed.
    /// </summary>
    /// <remarks>
    ///     If related records exist, the plot should be deactivated to preserve traceability.
    /// </remarks>
    public bool RequiresLogicalDeletion(bool hasRelatedOperationalRecords)
    {
        return hasRelatedOperationalRecords;
    }

    /// <summary>
    ///     Explains why a plot cannot be deleted.
    /// </summary>
    public string ExplainDeletionRejection(Plot? plot)
    {
        if (plot == null)
            return "Plot is required.";

        if (!plot.IsActive)
            return "Only active plots can be deleted.";

        return "Plot cannot be deleted.";
    }
}
