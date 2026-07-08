using System.ComponentModel.DataAnnotations;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record CreatePestSightingReportResource(
    [Range(1, long.MaxValue)] long PlotId,
    [Required] string RiskZone,
    [Required, MinLength(1)] List<string> Symptoms,
    [Required] string ObservedSeverity,
    string? Notes
);
