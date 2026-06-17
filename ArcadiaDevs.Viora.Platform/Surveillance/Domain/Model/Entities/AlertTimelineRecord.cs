namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;

public class AlertTimelineRecord
{
    public AlertTimelineRecord(string tag, string title, string description)
    {
        Tag = tag;
        Title = title;
        Description = description;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private AlertTimelineRecord() { } // EF Core

    public long Id { get; private set; }
    public long AlertId { get; private set; }
    public string Tag { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
