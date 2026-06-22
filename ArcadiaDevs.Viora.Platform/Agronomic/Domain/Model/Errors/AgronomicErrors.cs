using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

/// <summary>
///     Static domain error codes for the Agronomic bounded context.
///     Codes follow the convention <c>Agronomic.{Name}</c> to align with the
///     reference DDD layout (<c>PublishingErrors</c>, <c>IamErrors</c>).
/// </summary>
public static class AgronomicErrors
{
    public static readonly Error PlotNotFound =
        new("Agronomic.PlotNotFound", "The specified plot was not found.");

    public static readonly Error UnauthorizedAccess =
        new("Agronomic.UnauthorizedAccess", "The authenticated user does not own the requested plot.");

    public static readonly Error PlotInactive =
        new("Agronomic.PlotInactive", "Only active plots can be modified.");

    public static readonly Error PlotConflict =
        new("Agronomic.PlotConflict", "A plot with the same name already exists for this user.");

    public static readonly Error DeleteActivePlot =
        new("Agronomic.DeleteActivePlot", "The plot cannot be deleted under the current deletion policy.");

    public static readonly Error DeviceNotFound =
        new("Agronomic.DeviceNotFound", "The specified IoT device was not found.");

    public static readonly Error PlanNotFound =
        new("Agronomic.PlanNotFound", "The specified dynamic nutrition plan was not found.");

    public static readonly Error InvalidState =
        new("Agronomic.InvalidState", "The operation is invalid for the current state of the aggregate.");

    public static readonly Error InvalidInput =
        new("Agronomic.InvalidInput", "The provided input was invalid.");

    public static readonly Error CertificationError =
        new("Agronomic.CertificationError", "Failed to certify the dynamic nutrition plan.");

    public static readonly Error GenerationError =
        new("Agronomic.GenerationError", "Failed to generate the dynamic nutrition plan.");

    public static readonly Error AgronomicStatisticsAccess =
        new("Agronomic.AgronomicStatisticsAccess", "The authenticated user cannot access statistics from another user.");

    public static readonly Error PlotOwnership =
        new("Agronomic.PlotOwnership", "The authenticated user does not own the requested plot.");

    public static readonly Error QueryError =
        new("Agronomic.QueryError", "An error occurred while executing the agronomic query.");
}
