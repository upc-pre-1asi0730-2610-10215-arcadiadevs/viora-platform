namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to set a plot's grower-declared winter-chill requirement.
/// </summary>
public record ConfigureChillRequirementCommand(
    int PlotId,
    int UserId,
    double ChillRequirementPortions);
