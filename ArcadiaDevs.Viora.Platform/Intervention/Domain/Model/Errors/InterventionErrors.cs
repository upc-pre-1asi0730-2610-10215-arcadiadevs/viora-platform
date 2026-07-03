using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;

/// <summary>
///     Static domain error codes for the Intervention bounded context.
///     Shared by all 6 aggregates (REQ-CC-2). Codes follow the convention
///     <c>Intervention.{Name}</c>, mirroring <c>SurveillanceErrors</c>.
/// </summary>
public static class InterventionErrors
{
    public static readonly Error NotFound =
        new("Intervention.NotFound", "The specified intervention resource was not found.");

    public static readonly Error ValidationError =
        new("Intervention.ValidationError", "The request failed validation.");

    public static readonly Error ConflictError =
        new("Intervention.ConflictError", "The operation conflicts with the current state of the resource.");

    public static readonly Error DatabaseError =
        new("Intervention.DatabaseError", "A database error occurred while processing the intervention operation.");

    public static readonly Error InternalServerError =
        new("Intervention.InternalServerError", "An unexpected internal error occurred in Intervention.");

    public static readonly Error OperationCancelled =
        new("Intervention.OperationCancelled", "The operation was cancelled.");

    /// <summary>
    ///     REQ-SPEC-2: a specialist's contact info is not unlocked for the
    ///     given request (request not ACCEPTED, or specialist mismatch, or
    ///     the request does not exist).
    /// </summary>
    public static readonly Error ContactNotUnlocked =
        new("Intervention.ContactNotUnlocked", "Specialist contact information is not available for this request.");
}
