using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Direction of a metric's change relative to the previous comparable period.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ETrendDirection
{
    /// <summary>The metric increased beyond the stability margin.</summary>
    UP,

    /// <summary>The metric decreased beyond the stability margin.</summary>
    DOWN,

    /// <summary>The metric is essentially unchanged.</summary>
    STABLE
}
