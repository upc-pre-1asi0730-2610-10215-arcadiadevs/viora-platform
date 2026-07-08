namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record AlertTimelineRecordResource(
    string Tag,
    string Title,
    string Description,
    DateTimeOffset CreatedAt
);
