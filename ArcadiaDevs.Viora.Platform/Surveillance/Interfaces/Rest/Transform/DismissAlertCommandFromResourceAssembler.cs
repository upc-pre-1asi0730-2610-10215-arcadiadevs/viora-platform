using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

/// <summary>
/// Assembler to map an (optional) <see cref="DismissAlertResource"/> to a <see cref="DismissAlertCommand"/>.
/// </summary>
public static class DismissAlertCommandFromResourceAssembler
{
    /// <summary>
    /// Transforms the resource into a command. <paramref name="resource"/> may be
    /// <c>null</c> when the caller omits the request body entirely.
    /// </summary>
    /// <param name="alertId">The alert id from the route.</param>
    /// <param name="resource">The incoming REST resource, or <c>null</c> if omitted.</param>
    /// <returns>The generated command.</returns>
    public static DismissAlertCommand ToCommandFromResource(long alertId, DismissAlertResource? resource)
    {
        return new DismissAlertCommand(alertId, resource?.Reason);
    }
}
