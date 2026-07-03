namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     Selects the representation returned by GET / or GET /{plotId}.
///     Dispatched via <c>?view=</c> query parameter.
/// </summary>
public enum PlotView
{
    Overview,
    Detail,
    Monitoring,
    Weather
}
