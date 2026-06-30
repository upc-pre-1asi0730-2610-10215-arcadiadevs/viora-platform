using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

/// <summary>
///     Domain event raised by the <see cref="Aggregates.Alert"/> aggregate
///     on every successful state-machine transition (ConfirmFromInspection,
///     Dismiss, Escalate, LinkReport). Captures the transition label so
///     observers can discriminate which method was invoked without sniffing
///     the resulting state.
/// </summary>
/// <remarks>
///     <see cref="AlertId"/> and <see cref="PlotId"/> are transported as
///     primitive <see cref="long"/> values; consumers that need the
///     BC-local value object must wrap them in their own
///     <c>ArcadiaDevs.Viora.Platform.Shared...</c> VO (CC-1).
/// </remarks>
public record AlertUpdatedEvent(
    long AlertId,
    long PlotId,
    string Transition
) : IEvent;
