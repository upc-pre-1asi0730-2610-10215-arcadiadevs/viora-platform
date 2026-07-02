using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

/// <summary>
///     Partial extension of the <see cref="Alert"/> aggregate that adds the
///     <c>RESOLVED</c> terminal transition (REQ-4). Exposed via
///     <c>PATCH /api/v1/alerts/{id}</c> with <c>{"status": "RESOLVED"}</c>.
/// </summary>
public partial class Alert
{
    /// <summary>
    ///     Unconditionally transitions the alert to <c>RESOLVED</c> regardless
    ///     of its current status, appends a timeline record, and raises an
    ///     <see cref="AlertUpdatedEvent"/>. Unlike <see cref="ConfirmFromInspection"/>
    ///     and <see cref="Dismiss"/>, there is no invalid source state — this
    ///     always succeeds.
    /// </summary>
    public Result<Unit, Error> Resolve()
    {
        Status = "RESOLVED";
        AddTimelineRecord(
            "RESOLVED",
            "Alert resolved",
            "The alert was resolved.");
        _domainEvents.Add(new AlertUpdatedEvent(Id, PlotId.Value, "RESOLVED"));
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
