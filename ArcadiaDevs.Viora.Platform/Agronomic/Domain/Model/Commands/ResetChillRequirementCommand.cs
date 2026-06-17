namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

/// <summary>
///     Command to clear a plot's grower-declared chill requirement, reverting it to the system default.
/// </summary>
public record ResetChillRequirementCommand(
    int PlotId,
    int UserId);
