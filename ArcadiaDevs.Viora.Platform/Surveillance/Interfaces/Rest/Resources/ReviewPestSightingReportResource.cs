namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
/// Resource used as the request body for reviewing a pest sighting report.
/// The report id travels in the route and the reporter id in the query string,
/// so the body carries only the review outcome.
/// </summary>
public record ReviewPestSightingReportResource(string Outcome);
