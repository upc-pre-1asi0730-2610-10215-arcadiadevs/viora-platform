namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO representing an alert timeline record.
/// </summary>
/// <param name="Tag">The tag of the event.</param>
/// <param name="Title">The title of the event.</param>
/// <param name="Description">The description of the event.</param>
/// <param name="CreatedAt">The timestamp of the event.</param>
public record AlertTimelineRecordResource(
    string Tag,
    string Title,
    string Description,
    DateTimeOffset CreatedAt
);
