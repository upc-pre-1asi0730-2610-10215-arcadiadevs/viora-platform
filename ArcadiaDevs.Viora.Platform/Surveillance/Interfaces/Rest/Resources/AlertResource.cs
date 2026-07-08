namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record AlertResource(
    long Id,
    long PlotId,
    string Type,
    string Severity,
    string Status,
    string Title,
    string RiskExplanation,
    List<string> Sources,
    List<string> DataProviders,
    IDictionary<string, string> SupportingData,
    List<AlertTimelineRecordResource> Timeline
);
