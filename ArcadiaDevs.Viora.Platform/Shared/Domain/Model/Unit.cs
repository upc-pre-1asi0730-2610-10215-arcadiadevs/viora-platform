namespace ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

/// <summary>
///     Represents the absence of a value, used as a placeholder type parameter
///     when a <see cref="Result{TValue, TError}"/> carries no payload on success.
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    ///     The singleton value.
    /// </summary>
    public static readonly Unit Value = default;
}
