namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

/// <summary>
///     Query to fetch every pest sighting report submitted by a given reporter,
///     newest first.
/// </summary>
/// <param name="ReporterUserId">The id of the reporting user.</param>
public record GetPestSightingReportsByUserQuery(long ReporterUserId);
