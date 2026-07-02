using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

/// <summary>
/// Assembler to map a <see cref="ReviewPestSightingReportResource"/>, along with the
/// route-supplied report id and the query-supplied reporter id, to a
/// <see cref="ReviewPestSightingReportCommand"/>.
/// </summary>
public static class ReviewPestSightingReportCommandFromResourceAssembler
{
    /// <summary>
    /// Transforms the resource into a command.
    /// </summary>
    /// <param name="reportId">The report id, supplied via the route.</param>
    /// <param name="reporterUserId">The reporter id, supplied via the query string.</param>
    /// <param name="resource">The incoming REST resource body.</param>
    /// <returns>The generated command.</returns>
    public static ReviewPestSightingReportCommand ToCommandFromResource(
        long reportId,
        long reporterUserId,
        ReviewPestSightingReportResource resource)
    {
        return new ReviewPestSightingReportCommand(
            reportId,
            reporterUserId,
            resource.Outcome
        );
    }
}
