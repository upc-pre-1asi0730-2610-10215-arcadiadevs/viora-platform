using System;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

public sealed record ReviewPestSightingReportCommand(
    long ReportId,
    long ReporterUserId,
    string Outcome)
{
    public ReviewPestSightingReportCommand() : this(0, 0, string.Empty) { }
}
