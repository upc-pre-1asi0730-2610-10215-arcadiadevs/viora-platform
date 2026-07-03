using System.ComponentModel.DataAnnotations.Schema;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Exceptions;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

/// <summary>
///     Alert aggregate root. SURV-001 exposes a state machine
///     (<c>ConfirmFromInspection</c> / <c>Dismiss</c> / <c>Escalate</c> /
///     <c>LinkReport</c>) that validates the current state, mutates the
///     aggregate, appends a timeline record, and raises an
///     <see cref="AlertUpdatedEvent"/> via the <see cref="IHasDomainEvents"/>
///     collection. The existing <see cref="MarkAsReviewed"/> is preserved
///     and is still the path used by <c>PATCH /api/v1/alerts/{id}</c>.
/// </summary>
public partial class Alert : IHasDomainEvents
{
    private readonly IClock _clock;

    public Alert() : this(new SystemClock())
    {
    }

    public Alert(IClock clock)
    {
        _clock = clock;
        Type = EThreatType.UNKNOWN;
        Severity = EAlertSeverity.LOW;
        Title = string.Empty;
        RiskExplanation = string.Empty;
        Status = "ACTIVE";
        Sources = new List<string>();
        DataProviders = new List<string>();
        SupportingData = new Dictionary<string, string>();
        _timeline = new List<AlertTimelineRecord>();
        _domainEvents = new List<IEvent>();
        PlotId = null!;
    }

    public Alert(CreateAlertCommand command) : this(command, new SystemClock())
    {
    }

    public Alert(CreateAlertCommand command, IClock clock) : this(clock)
    {
        PlotId = new PlotId(command.PlotId);
        Type = Enum.Parse<EThreatType>(command.AlertType, true);
        Severity = Enum.Parse<EAlertSeverity>(command.Severity, true);
        Title = command.Title;
        RiskExplanation = command.RiskExplanation;
        Status = "ACTIVE";
        Sources = command.Sources ?? new List<string>();
        DataProviders = command.DataProviders ?? new List<string>();
        SupportingData = command.SupportingData != null
            ? new Dictionary<string, string>(command.SupportingData)
            : new Dictionary<string, string>();

        _timeline = new List<AlertTimelineRecord>();
        AddTimelineRecord("CREATED", "Alert Generated", "The alert was automatically or manually generated.");
    }

    public long Id { get; }
    public PlotId PlotId { get; private set; }
    public EThreatType Type { get; private set; }
    public EAlertSeverity Severity { get; private set; }
    public string Title { get; private set; }
    public string RiskExplanation { get; private set; }
    public string Status { get; private set; }
    public IList<string> Sources { get; private set; }
    public IList<string> DataProviders { get; private set; }
    public IDictionary<string, string> SupportingData { get; private set; }

    private readonly List<AlertTimelineRecord> _timeline;
    public IReadOnlyCollection<AlertTimelineRecord> Timeline => _timeline.AsReadOnly();

    /// <summary>
    ///     Pest Sighting Report currently linked to this alert. Set via
    ///     <see cref="LinkReport"/>; null until a report has been attached.
    ///     Not mapped to a database column in Phase 1; persistence is added
    ///     when a future migration introduces a FK from
    ///     <c>pest_sighting_reports</c> to <c>alerts</c>.
    /// </summary>
    [NotMapped]
    public PestSightingReportId? LinkedReportId { get; private set; }

    private readonly List<IEvent> _domainEvents;
    public IReadOnlyCollection<IEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    ///     Clears the <see cref="DomainEvents"/> collection. Invoked by the
    ///     <c>PostCommitDomainEventDispatcher</c> (SHARED-011) AFTER each
    ///     <see cref="IEvent"/> has been (attempted to be) dispatched on
    ///     the in-process bus, so the next <c>SaveChanges</c> does not
    ///     re-dispatch the same events. The <c>IHasDomainEvents</c>
    ///     contract stays read-only; the clear method is a public member
    ///     of the concrete aggregate.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    public void AddTimelineRecord(string tag, string title, string description)
    {
        _timeline.Add(new AlertTimelineRecord(tag, title, description, _clock));
    }

    public Alert MarkAsReviewed()
    {
        if (Status is "UNDER_REVIEW" or "RESOLVED" or "DISMISSED")
        {
            throw new AlertAlreadyReviewedException(Id);
        }

        Status = "UNDER_REVIEW";
        AddTimelineRecord("Info", "Alert marked as reviewed", "A specialist has acknowledged and is reviewing this alert.");
        return this;
    }

    /// <summary>
    ///     Transitions the alert from any non-terminal state (anything other
    ///     than <c>DISMISSED</c> / <c>RESOLVED</c>) to <c>UNDER_REVIEW</c>
    ///     and raises the severity by one level
    ///     (<c>LOW → MEDIUM → HIGH → CRITICAL</c>, capped at
    ///     <c>CRITICAL</c>). Raises an <see cref="AlertUpdatedEvent"/> on
    ///     success; returns a <see cref="Result{TValue, TError}.Failure"/>
    ///     and leaves state unchanged on a terminal source state.
    /// </summary>
    public Result<Unit, Error> ConfirmFromInspection()
    {
        if (Status is "DISMISSED" or "RESOLVED")
        {
            return new Result<Unit, Error>.Failure(
                new Error("ALERT_TERMINAL", "Cannot confirm a terminal alert."));
        }

        Status = "UNDER_REVIEW";
        Severity = Severity.RaiseOne();
        AddTimelineRecord(
            "CONFIRMED",
            "Alert confirmed from inspection",
            "A specialist has confirmed the alert after on-site inspection.");
        _domainEvents.Add(new AlertUpdatedEvent(Id, PlotId.Value, "CONFIRMED"));
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Transitions the alert from any non-<c>DISMISSED</c> state to
    ///     <c>DISMISSED</c> (terminal). Raises an
    ///     <see cref="AlertUpdatedEvent"/> on success; returns a
    ///     <see cref="Result{TValue, TError}.Failure"/> and leaves state
    ///     unchanged when the alert is already <c>DISMISSED</c>.
    /// </summary>
    /// <param name="reason">
    ///     Optional caller-supplied dismissal reason, recorded as the
    ///     timeline entry's description. When omitted or blank, a default
    ///     description is used instead.
    /// </param>
    public Result<Unit, Error> Dismiss(string? reason = null)
    {
        if (Status is "DISMISSED")
        {
            return new Result<Unit, Error>.Failure(
                new Error("ALERT_TERMINAL", "Cannot dismiss an already-dismissed alert."));
        }

        Status = "DISMISSED";
        var description = string.IsNullOrWhiteSpace(reason)
            ? "The alert was dismissed without further action."
            : reason;
        AddTimelineRecord("DISMISSED", "Alert dismissed", description);
        _domainEvents.Add(new AlertUpdatedEvent(Id, PlotId.Value, "DISMISSED"));
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Raises the severity by one level (capped at
    ///     <see cref="EAlertSeverity.CRITICAL"/>) without changing the
    ///     status. Raises an <see cref="AlertUpdatedEvent"/> on success;
    ///     always succeeds — there is no invalid source state for an
    ///     escalation.
    /// </summary>
    public Result<Unit, Error> Escalate()
    {
        Severity = Severity.RaiseOne();
        AddTimelineRecord(
            "ESCALATED",
            "Alert severity escalated",
            $"Severity raised to {Severity}.");
        _domainEvents.Add(new AlertUpdatedEvent(Id, PlotId.Value, "ESCALATED"));
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Attaches a <see cref="PestSightingReportId"/> to the alert
    ///     without changing its status or severity. Raises an
    ///     <see cref="AlertUpdatedEvent"/> on success; always succeeds.
    ///     Calling <see cref="LinkReport"/> with a different report id
    ///     overwrites the previous link.
    /// </summary>
    public Result<Unit, Error> LinkReport(PestSightingReportId reportId)
    {
        LinkedReportId = reportId;
        AddTimelineRecord(
            "LINKED_REPORT",
            "Pest sighting report linked",
            $"Pest sighting report {reportId.Value} linked to this alert.");
        _domainEvents.Add(new AlertUpdatedEvent(Id, PlotId.Value, "LINKED_REPORT"));
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
