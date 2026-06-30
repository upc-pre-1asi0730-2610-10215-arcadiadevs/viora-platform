using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Domain.Model.Aggregates;

/// <summary>
///     SURV-001 hardening tests for the <see cref="Alert"/> aggregate.
///     The aggregate must expose a state machine
///     (<c>ConfirmFromInspection</c> / <c>Dismiss</c> / <c>Escalate</c> /
///     <c>LinkReport</c>) that returns <see cref="Result{TValue, TError}"/>,
///     leaves state unchanged on failure, and raises an
///     <see cref="AlertUpdatedEvent"/> on every successful transition.
/// </summary>
public class AlertTests
{
    private const long PlotIdValue = 42L;
    private const string Title = "Test Alert";
    private const string RiskExplanation = "Some risk";

    private static Alert NewActiveAlert(EAlertSeverity severity = EAlertSeverity.LOW)
    {
        var command = new CreateAlertCommand(
            PlotId: PlotIdValue,
            AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: severity.ToString(),
            Title: Title,
            RiskExplanation: RiskExplanation,
            Sources: new List<string>(),
            DataProviders: new List<string>(),
            SupportingData: new Dictionary<string, string>()
        );
        return new Alert(command);
    }

    [Fact]
    public void ConfirmFromInspection_FromActive_TransitionsAndRaisesEvent()
    {
        // Arrange — fresh ACTIVE alert with severity LOW
        var alert = NewActiveAlert(EAlertSeverity.LOW);
        Assert.Equal("ACTIVE", alert.Status);
        Assert.Equal(EAlertSeverity.LOW, alert.Severity);

        // Act
        var result = alert.ConfirmFromInspection();

        // Assert — state machine
        Assert.True(result.IsSuccess);
        Assert.Equal("UNDER_REVIEW", alert.Status);
        Assert.Equal(EAlertSeverity.MEDIUM, alert.Severity);

        // Assert — domain event
        var events = alert.DomainEvents;
        Assert.Contains(events, e => e is AlertUpdatedEvent u && u.Transition == "CONFIRMED");

        // Assert — timeline record
        Assert.Contains(alert.Timeline, t => t.Tag == "CONFIRMED");
    }

    [Fact]
    public void Dismiss_FromAnyNonDismissed_TransitionsToDismissed()
    {
        // Arrange — start from ACTIVE
        var alert = NewActiveAlert();

        // Act
        var result = alert.Dismiss();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);
        Assert.Contains(alert.DomainEvents, e => e is AlertUpdatedEvent u && u.Transition == "DISMISSED");
        Assert.Contains(alert.Timeline, t => t.Tag == "DISMISSED");
    }

    [Fact]
    public void Dismiss_FromUnderReview_TransitionsToDismissed()
    {
        // Arrange — move to UNDER_REVIEW first via ConfirmFromInspection
        var alert = NewActiveAlert();
        var confirm = alert.ConfirmFromInspection();
        Assert.True(confirm.IsSuccess);
        Assert.Equal("UNDER_REVIEW", alert.Status);

        // Act
        var result = alert.Dismiss();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);
    }

    [Fact]
    public void Escalate_RaisesSeverity()
    {
        // Arrange
        var alert = NewActiveAlert(EAlertSeverity.LOW);
        var initialEventCount = alert.DomainEvents.Count;

        // Act
        var result = alert.Escalate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EAlertSeverity.MEDIUM, alert.Severity);
        // Status unchanged
        Assert.Equal("ACTIVE", alert.Status);
        // Domain event raised
        Assert.Equal(initialEventCount + 1, alert.DomainEvents.Count);
        Assert.Contains(alert.DomainEvents, e => e is AlertUpdatedEvent u && u.Transition == "ESCALATED");
    }

    [Fact]
    public void Escalate_FromCritical_StaysAtCritical()
    {
        // Arrange — severity already at the top
        var alert = NewActiveAlert(EAlertSeverity.CRITICAL);

        // Act
        var result = alert.Escalate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EAlertSeverity.CRITICAL, alert.Severity);
    }

    [Fact]
    public void LinkReport_AttachesWithoutStateChange()
    {
        // Arrange
        var alert = NewActiveAlert();
        var initialStatus = alert.Status;
        var initialSeverity = alert.Severity;
        var reportId = new PestSightingReportId(123L);
        var initialEventCount = alert.DomainEvents.Count;

        // Act
        var result = alert.LinkReport(reportId);

        // Assert
        Assert.True(result.IsSuccess);
        // State unchanged
        Assert.Equal(initialStatus, alert.Status);
        Assert.Equal(initialSeverity, alert.Severity);
        Assert.Equal(reportId, alert.LinkedReportId);
        // Domain event raised
        Assert.Equal(initialEventCount + 1, alert.DomainEvents.Count);
        Assert.Contains(alert.DomainEvents, e => e is AlertUpdatedEvent u && u.Transition == "LINKED_REPORT");
    }

    [Fact]
    public void ConfirmFromInspection_OnDismissed_ReturnsFailureAndUnchangedState()
    {
        // Arrange — dismiss first
        var alert = NewActiveAlert();
        var dismiss = alert.Dismiss();
        Assert.True(dismiss.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);
        var severityAfterDismiss = alert.Severity;
        var eventCountAfterDismiss = alert.DomainEvents.Count;

        // Act — try to confirm a dismissed alert
        var result = alert.ConfirmFromInspection();

        // Assert
        Assert.True(result.IsFailure);
        var error = ((Result<Unit, Error>.Failure)result).Error;
        Assert.Equal("ALERT_TERMINAL", error.Code);
        // State unchanged
        Assert.Equal("DISMISSED", alert.Status);
        Assert.Equal(severityAfterDismiss, alert.Severity);
        // No new domain event
        Assert.Equal(eventCountAfterDismiss, alert.DomainEvents.Count);
    }

    [Fact]
    public void Dismiss_OnDismissed_ReturnsFailureAndUnchangedState()
    {
        // Arrange
        var alert = NewActiveAlert();
        var firstDismiss = alert.Dismiss();
        Assert.True(firstDismiss.IsSuccess);
        var eventCountAfterFirstDismiss = alert.DomainEvents.Count;

        // Act — second dismiss
        var secondDismiss = alert.Dismiss();

        // Assert
        Assert.True(secondDismiss.IsFailure);
        var error = ((Result<Unit, Error>.Failure)secondDismiss).Error;
        Assert.Equal("ALERT_TERMINAL", error.Code);
        // State unchanged
        Assert.Equal("DISMISSED", alert.Status);
        // No new domain event
        Assert.Equal(eventCountAfterFirstDismiss, alert.DomainEvents.Count);
    }

    [Fact]
    public void MarkAsReviewed_IsPreserved_AndStillTransitionsToUnderReview()
    {
        // Arrange — the existing MarkAsReviewed() must still work (still called by PATCH /api/v1/alerts/{id})
        var alert = NewActiveAlert();

        // Act
        var result = alert.MarkAsReviewed();

        // Assert
        Assert.Equal("UNDER_REVIEW", result.Status);
    }

    [Fact]
    public void DomainEvents_AreEmptyForNewlyConstructedAlert()
    {
        var alert = NewActiveAlert();
        Assert.Empty(alert.DomainEvents);
    }

    [Fact]
    public void Alert_ImplementsIHasDomainEvents()
    {
        var alert = NewActiveAlert();
        Assert.IsAssignableFrom<IHasDomainEvents>(alert);
    }
}
