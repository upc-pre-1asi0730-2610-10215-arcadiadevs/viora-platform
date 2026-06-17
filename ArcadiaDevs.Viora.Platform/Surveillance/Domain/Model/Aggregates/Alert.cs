using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Exceptions;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
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
        Sources = new List<string>();
        DataProviders = new List<string>();
        SupportingData = new Dictionary<string, string>();
        _timeline = new List<AlertTimelineRecord>();
    }

    public Alert(CreateAlertCommand command) : this()
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

    public void AddTimelineRecord(string tag, string title, string description)
    {
        _timeline.Add(new AlertTimelineRecord(tag, title, description));
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
}
