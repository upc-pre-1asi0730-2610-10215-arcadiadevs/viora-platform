namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record AlertSummaryResource(
    long Id,
    string Type,
    string Description,
    string Severity,
    string Date,
    string Status,
    List<string> Sources,
    long PlotId,
    PlotSummaryResource Plot
);
