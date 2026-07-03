using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Interfaces.Rest.Transform;

/// <summary>
///     WU5 tests for <see cref="ReviewPestSightingReportCommandFromResourceAssembler"/>.
///     Template A: pure static function, no DI.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class ReviewPestSightingReportCommandFromResourceAssemblerTests
{
    /// <summary>
    ///     GIVEN a <see cref="ReviewPestSightingReportResource"/> with a valid outcome
    ///     WHEN <see cref="ReviewPestSightingReportCommandFromResourceAssembler.ToCommandFromResource"/> is called
    ///     THEN the returned command contains the correct report id, reporter user id, and outcome.
    /// </summary>
    [Fact]
    public void FromResource_ValidResource_ReturnsCorrectCommand()
    {
        // GIVEN a resource with CONFIRMED outcome
        var resource = new ReviewPestSightingReportResource(Outcome: "CONFIRMED");
        const long reportId = 42L;
        const long reporterUserId = 100L;

        // WHEN the assembler maps the resource to a command
        var command = ReviewPestSightingReportCommandFromResourceAssembler.ToCommandFromResource(
            reportId, reporterUserId, resource);

        // THEN the command fields are correct
        Assert.Equal(reportId, command.ReportId);
        Assert.Equal(reporterUserId, command.ReporterUserId);
        Assert.Equal("CONFIRMED", command.Outcome);
    }

    /// <summary>
    ///     GIVEN a <see cref="ReviewPestSightingReportResource"/> with RULED_OUT outcome
    ///     WHEN <see cref="ReviewPestSightingReportCommandFromResourceAssembler.ToCommandFromResource"/> is called
    ///     THEN the returned command carries the RULED_OUT outcome.
    /// </summary>
    [Fact]
    public void FromResource_RuledOutOutcome_ReturnsCorrectCommand()
    {
        // GIVEN a resource with RULED_OUT outcome
        var resource = new ReviewPestSightingReportResource(Outcome: "RULED_OUT");

        // WHEN the assembler maps the resource to a command
        var command = ReviewPestSightingReportCommandFromResourceAssembler.ToCommandFromResource(
            reportId: 7L, reporterUserId: 200L, resource);

        // THEN the command carries the RULED_OUT outcome
        Assert.Equal(7L, command.ReportId);
        Assert.Equal(200L, command.ReporterUserId);
        Assert.Equal("RULED_OUT", command.Outcome);
    }
}
