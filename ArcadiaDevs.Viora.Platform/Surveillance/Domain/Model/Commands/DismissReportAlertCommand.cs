namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

public sealed record DismissReportAlertCommand(
    long ReportId,
    string DismissalReason);
