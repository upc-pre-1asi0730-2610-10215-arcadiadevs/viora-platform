namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

/// <summary>
///     Query to fetch a single alert by id.
/// </summary>
/// <param name="AlertId">The alert identifier.</param>
/// <param name="RequestingUserId">
///     The authenticated caller's id, derived from the token, when the query is
///     reached from a public REST GET (ownership is enforced against it). Internal
///     call sites that re-fetch an alert right after a state-transition command
///     they already authorized (e.g. <c>LinkReport</c>) pass <c>null</c> to skip
///     the redundant check.
/// </param>
public record GetAlertByIdQuery(long AlertId, long? RequestingUserId = null);
