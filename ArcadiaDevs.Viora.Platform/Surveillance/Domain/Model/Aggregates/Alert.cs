using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;

public partial class Alert
{
    public Alert()
    {
        Type = EThreatType.UNKNOWN;
        Severity = EAlertSeverity.LOW;
        Title = string.Empty;
        RiskExplanation = string.Empty;
        Status = "ACTIVE";
    }

    public Alert(CreateAlertCommand command) : this()
    {
        PlotId = new PlotId(command.PlotId);
        Type = Enum.Parse<EThreatType>(command.AlertType, true);
        Severity = Enum.Parse<EAlertSeverity>(command.Severity, true);
        Title = command.Title;
        RiskExplanation = command.RiskExplanation;
        Status = "ACTIVE";
    }

    public long Id { get; }
    public PlotId PlotId { get; private set; }
    public EThreatType Type { get; private set; }
    public EAlertSeverity Severity { get; private set; }
    public string Title { get; private set; }
    public string RiskExplanation { get; private set; }
    public string Status { get; private set; }
}
