using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

public record AlertCreatedEvent(
    long AlertId,
    long PlotId,
    string AlertType,
    string Severity
) : IEvent;
