namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Optional request body for the specialist-scoped decline
///     (<c>POST /intervention-requests/{id}/declines</c>). When omitted or
///     blank, the command service defaults to "Declined by specialist".
/// </summary>
public record SpecialistDeclineResource(string? Reason);
