using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record PlanRationale
{
    private const int SummaryMaxLength = 500;

    public string Summary { get; }
    public EClimateRiskLevel TriggeringRiskLevel { get; }
    public NdviValue NdviValue { get; }
    public double TemperatureAnomaly { get; }

    protected PlanRationale() { }

    public PlanRationale(
        string summary,
        EClimateRiskLevel triggeringRiskLevel,
        NdviValue ndviValue,
        double temperatureAnomaly)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Plan rationale summary is required.");
        
        if (summary.Trim().Length > SummaryMaxLength)
            throw new ArgumentException($"Plan rationale summary cannot exceed {SummaryMaxLength} characters.");

        Summary = summary.Trim();
        TriggeringRiskLevel = triggeringRiskLevel;
        NdviValue = ndviValue ?? throw new ArgumentNullException(nameof(ndviValue), "NDVI value is required.");
        TemperatureAnomaly = temperatureAnomaly;
    }
}
