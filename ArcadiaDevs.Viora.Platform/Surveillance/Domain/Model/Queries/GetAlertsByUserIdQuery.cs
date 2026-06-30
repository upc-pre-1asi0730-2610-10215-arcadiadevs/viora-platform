namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

/// <summary>
///     Query to fetch the alerts of a user, sorted by the requested key
///     (SURV-003 sort-placeholder fix). Replaces the previous behaviour
///     where any sort other than <c>"recent"</c> returned an empty list.
/// </summary>
/// <param name="UserId">The id of the user whose plots' alerts are returned.</param>
/// <param name="Sort">One of <c>"recent"</c>, <c>"severity"</c>, <c>"type"</c>. Defaults to <c>"recent"</c>.</param>
/// <param name="Limit">Maximum number of alerts to return.</param>
public record GetAlertsByUserIdQuery(
    long UserId,
    string? Sort = "recent",
    int Limit = 3);
