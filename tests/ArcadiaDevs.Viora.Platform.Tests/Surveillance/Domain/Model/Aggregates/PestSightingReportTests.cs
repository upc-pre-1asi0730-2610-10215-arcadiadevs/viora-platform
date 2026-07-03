using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Domain.Model.Aggregates;

/// <summary>
///     WU5 tests for the <see cref="PestSightingReport"/> aggregate.
///     Covers creation from command, review state machine transitions
///     (<c>ConfirmAfterInspection</c>, <c>DismissAfterInspection</c>),
///     and invalid-state rejection.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PestSightingReportTests
{
    private const long PlotIdValue = 42L;
    private const long ReporterUserIdValue = 100L;

    private static CreatePestSightingReportCommand NewCommand(
        string severity = "LOW",
        string riskZone = "FULL_PLOT",
        List<string>? symptoms = null) => new(
        PlotId: PlotIdValue,
        ReporterUserId: ReporterUserIdValue,
        RiskZone: riskZone,
        Symptoms: symptoms ?? new List<string> { "yellowing leaves" },
        ObservedSeverity: severity,
        Notes: "Test notes");

    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // GIVEN valid pest sighting data
        var command = NewCommand(severity: "HIGH", riskZone: "FULL_PLOT",
            symptoms: new List<string> { "fruit rot", "puncture" });

        // WHEN the aggregate is created from the command
        var report = new PestSightingReport(command);

        // THEN properties are correctly set
        Assert.Equal(PlotIdValue, report.PlotId.Value);
        Assert.Equal(ReporterUserIdValue, report.ReporterUserId.Value);
        Assert.Equal(ERiskZone.FULL_PLOT, report.RiskZone);
        Assert.Equal(EAlertSeverity.HIGH, report.ObservedSeverity);
        Assert.Equal("Test notes", report.Notes);
        Assert.Equal(EReportStatus.UNDER_REVIEW, report.Status);
        Assert.False(report.Evaluated);
        Assert.False(report.AlertConfirmed);
    }

    [Fact]
    public void Review_ConfirmAfterInspection_SetsStatusToConfirmed()
    {
        // GIVEN a report in UNDER_REVIEW status
        var report = new PestSightingReport(NewCommand());
        Assert.Equal(EReportStatus.UNDER_REVIEW, report.Status);

        // WHEN ConfirmAfterInspection is called
        report.ConfirmAfterInspection();

        // THEN status transitions to CONFIRMED
        Assert.Equal(EReportStatus.CONFIRMED, report.Status);
        Assert.True(report.AlertConfirmed);
    }

    [Fact]
    public void Review_DismissAfterInspection_SetsStatusToRuledOut()
    {
        // GIVEN a report in UNDER_REVIEW status
        var report = new PestSightingReport(NewCommand());
        Assert.Equal(EReportStatus.UNDER_REVIEW, report.Status);

        // WHEN DismissAfterInspection is called
        report.DismissAfterInspection();

        // THEN status transitions to RULED_OUT
        Assert.Equal(EReportStatus.RULED_OUT, report.Status);
        Assert.False(report.AlertConfirmed);
    }

    [Fact]
    public void Review_ConfirmFromConfirmed_ThrowsInvalidOperationException()
    {
        // GIVEN a report already in CONFIRMED status
        var report = new PestSightingReport(NewCommand());
        report.ConfirmAfterInspection();
        Assert.Equal(EReportStatus.CONFIRMED, report.Status);

        // WHEN ConfirmAfterInspection is called again
        // THEN it throws InvalidOperationException
        var ex = Assert.Throws<InvalidOperationException>(() => report.ConfirmAfterInspection());
        Assert.Contains("Cannot confirm a report with status CONFIRMED", ex.Message);
    }

    /// <summary>
    ///     Regression test for a bug fix: the parameterless constructor (used by EF Core
    ///     for materialization) used to call <c>new Symptoms(new List&lt;Symptom&gt;())</c>,
    ///     which threw <see cref="ArgumentException"/> because <see cref="Symptoms"/>'
    ///     constructor rejects empty lists. That call was removed, so the parameterless
    ///     constructor now leaves <see cref="PestSightingReport.Symptoms"/> at its default
    ///     (null) — EF Core is expected to populate it during materialization.
    /// </summary>
    [Fact]
    public void ParameterlessConstructor_DoesNotThrow_AndLeavesSymptomsNull()
    {
        // WHEN the parameterless constructor is invoked directly (as EF Core would)
        var exception = Record.Exception(() => new PestSightingReport());

        // THEN it does not throw
        Assert.Null(exception);

        // AND Symptoms is left at its default (null) — the EF Core materialization
        // path is responsible for populating it from the persisted state.
        var report = new PestSightingReport();
        Assert.Null(report.Symptoms);
    }
}
