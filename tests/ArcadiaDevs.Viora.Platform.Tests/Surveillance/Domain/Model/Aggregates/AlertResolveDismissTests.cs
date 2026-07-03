using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Domain.Model.Aggregates;

/// <summary>
///     WU4 tests for the <see cref="Alert"/> aggregate's
///     <c>Resolve()</c> and <c>Dismiss(string?)</c> transitions.
///     <c>Resolve()</c> is unconditional — it always transitions to
///     <c>RESOLVED</c> regardless of the current state (REQ-4).
///     <c>Dismiss(string?)</c> stores an optional caller-supplied reason
///     on the timeline record (REQ-5).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class AlertResolveDismissTests
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
    public void Resolve_FromActiveAlert_SetsStatusToResolved()
    {
        // Arrange — fresh ACTIVE alert
        var alert = NewActiveAlert(EAlertSeverity.LOW);
        Assert.Equal("ACTIVE", alert.Status);

        // Act
        var result = alert.Resolve();

        // Assert — state transition
        Assert.True(result.IsSuccess);
        Assert.Equal("RESOLVED", alert.Status);

        // Assert — domain event raised
        Assert.Contains(alert.DomainEvents, e => e is AlertUpdatedEvent u && u.Transition == "RESOLVED");

        // Assert — timeline record appended
        Assert.Contains(alert.Timeline, t => t.Tag == "RESOLVED");
    }

    [Fact]
    public void Resolve_FromDismissedAlert_TransitionsToResolved()
    {
        // Arrange — dismiss first (terminal state)
        var alert = NewActiveAlert();
        var dismiss = alert.Dismiss();
        Assert.True(dismiss.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);

        // Act — Resolve() is unconditional, so it succeeds even from DISMISSED
        var result = alert.Resolve();

        // Assert — transitions to RESOLVED despite DISMISSED source state
        Assert.True(result.IsSuccess);
        Assert.Equal("RESOLVED", alert.Status);
        Assert.Contains(alert.DomainEvents, e => e is AlertUpdatedEvent u && u.Transition == "RESOLVED");
    }

    [Fact]
    public void Resolve_ThenDismiss_Succeeds()
    {
        // Arrange — resolve first
        var alert = NewActiveAlert();
        var resolve = alert.Resolve();
        Assert.True(resolve.IsSuccess);
        Assert.Equal("RESOLVED", alert.Status);

        // Act — dismiss after resolve (Dismiss only rejects DISMISSED, not RESOLVED)
        var result = alert.Dismiss();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);
    }

    [Fact]
    public void Dismiss_WithReason_PersistsReason()
    {
        // Arrange — fresh ACTIVE alert
        var alert = NewActiveAlert();
        var reason = "No further action required after field inspection.";

        // Act
        var result = alert.Dismiss(reason);

        // Assert — status transition
        Assert.True(result.IsSuccess);
        Assert.Equal("DISMISSED", alert.Status);

        // Assert — reason persisted as timeline record description
        var timelineRecord = Assert.Single(alert.Timeline, t => t.Tag == "DISMISSED");
        Assert.Equal(reason, timelineRecord.Description);
    }

    [Fact]
    public void Dismiss_NullReason_DefaultsEmpty()
    {
        // Arrange
        var alert = NewActiveAlert();

        // Act — dismiss without a reason
        var result = alert.Dismiss();

        // Assert — default description is used
        Assert.True(result.IsSuccess);
        var timelineRecord = Assert.Single(alert.Timeline, t => t.Tag == "DISMISSED");
        Assert.Equal("The alert was dismissed without further action.", timelineRecord.Description);
    }
}
