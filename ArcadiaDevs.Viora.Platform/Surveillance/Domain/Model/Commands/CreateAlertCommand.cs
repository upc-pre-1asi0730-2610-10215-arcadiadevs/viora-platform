namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

/// <summary>
/// Command to create a new automated alert.
/// </summary>
/// <param name="PlotId">The ID of the affected plot.</param>
/// <param name="AlertType">The type of threat detected.</param>
/// <param name="Severity">The severity level of the threat.</param>
/// <param name="Title">A descriptive title for the alert.</param>
/// <param name="RiskExplanation">Detailed explanation of the risk.</param>
/// <param name="Sources">Optional list of data sources triggering the alert.</param>
/// <param name="DataProviders">Optional list of external providers (e.g., satellites).</param>
/// <param name="SupportingData">Optional dictionary with supporting metadata.</param>
public record CreateAlertCommand(
    long PlotId,
    string AlertType,
    string Severity,
    string Title,
    string RiskExplanation,
    List<string>? Sources,
    List<string>? DataProviders,
    IDictionary<string, string>? SupportingData
);
