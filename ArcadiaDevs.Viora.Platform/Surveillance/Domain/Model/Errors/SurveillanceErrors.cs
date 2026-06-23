using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;

/// <summary>
///     Static domain error codes for the Surveillance bounded context.
///     Codes follow the convention <c>Surveillance.{Name}</c> to align with the
///     reference DDD layout (<c>PublishingErrors</c>, <c>IamErrors</c>).
/// </summary>
public static class SurveillanceErrors
{
    public static readonly Error OperationCancelled =
        new("Surveillance.OperationCancelled", "The operation was cancelled.");

    public static readonly Error DatabaseError =
        new("Surveillance.DatabaseError", "A database error occurred while processing the surveillance operation.");

    public static readonly Error InternalServerError =
        new("Surveillance.InternalServerError", "An unexpected internal error occurred in Surveillance.");

    public static readonly Error NotFound =
        new("Surveillance.NotFound", "The specified surveillance resource was not found.");

    public static readonly Error AlertAlreadyReviewed =
        new("Surveillance.AlertAlreadyReviewed", "The alert has already been reviewed and cannot be modified.");

    public static readonly Error InvalidRiskZone =
        new("Surveillance.InvalidRiskZone", "The specified risk zone is invalid.");
}
