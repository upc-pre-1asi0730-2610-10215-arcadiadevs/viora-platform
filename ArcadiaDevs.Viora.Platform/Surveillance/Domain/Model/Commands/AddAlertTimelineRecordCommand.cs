using System;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

/// <summary>
///     Command to append a timeline record to an existing alert.
///     Used by cross-BC event handlers to track automated actions
///     (e.g., dynamic nutrition plan generation) on an alert's timeline.
/// </summary>
public sealed record AddAlertTimelineRecordCommand(
    long AlertId,
    string Tag,
    string Title,
    string Description)
{
    /// <summary>
    ///     Factory method that validates inputs before constructing the command.
    /// </summary>
    public static AddAlertTimelineRecordCommand Create(
        long alertId, string tag, string title, string description)
    {
        if (alertId <= 0) throw new ArgumentException("Alert ID must be a positive number.");
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be null or empty.");
        return new AddAlertTimelineRecordCommand(alertId, tag, title, description);
    }
}
