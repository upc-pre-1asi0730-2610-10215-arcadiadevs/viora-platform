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

    public static readonly Error PlanNotCertifiable =
        new("Agronomic.PlanNotCertifiable", "The dynamic nutrition plan cannot be certified in its current state.");

    public static readonly Error InvalidInput =
        new("Agronomic.InvalidInput", "The provided input was invalid.");

    public static readonly Error CertificationError =
        new("Agronomic.CertificationError", "Failed to certify the dynamic nutrition plan.");

    public static readonly Error GenerationError =
        new("Agronomic.GenerationError", "Failed to generate the dynamic nutrition plan.");

    // A2 part 2: the generator throws DynamicNutritionPlanUnavailableException
    // when no triggering risk is observed (CC-7: early throw, no silent
    // default). The command service boundary catches the exception and
    // converts it to this error so the REST surface sees a normal 4xx.
    public static readonly Error NoTriggeringRisk =
        new("Agronomic.NoTriggeringRisk", "No triggering risk was observed for the plot; a dynamic nutrition plan cannot be generated.");

    public static readonly Error AgronomicStatisticsAccess =
        new("Agronomic.AgronomicStatisticsAccess", "The authenticated user cannot access statistics from another user.");

    public static readonly Error PlotOwnership =
        new("Agronomic.PlotOwnership", "The authenticated user does not own the requested plot.");

    public static readonly Error QueryError =
        new("Agronomic.QueryError", "An error occurred while executing the agronomic query.");

    public static readonly Error TileNotFound =
        new("Agronomic.TileNotFound", "The requested NDVI tile could not be fetched.");

    public static readonly Error PlotNotLinked =
        new("Agronomic.PlotNotLinked", "The plot is not linked to AgroMonitoring.");

    public static readonly Error WeatherUnavailable =
        new("Agronomic.WeatherUnavailable", "Live weather data is currently unavailable; the platform does not provide a fabricated fallback.");

    // A4 part 2: activation-code claim failures. The codes are stable
    // identifiers; the messages are surfaced via AgronomicMessages.resx.
    public static readonly Error InvalidActivationCodeFormat =
        new("Agronomic.InvalidActivationCodeFormat", "The activation code format is invalid. Expected VIORA-<SP|LW|WS><NN>-<XXXX>.");

    public static readonly Error ActivationCodeNotRecognized =
        new("Agronomic.ActivationCodeNotRecognized", "The activation code is not in the issued-code catalog.");

    public static readonly Error ActivationCodeAlreadyClaimed =
        new("Agronomic.ActivationCodeAlreadyClaimed", "The activation code has already been claimed by another device.");

    public static readonly Error InternalServerError =
        new("Agronomic.InternalServerError", "An unexpected internal error occurred in Agronomic.");
}
