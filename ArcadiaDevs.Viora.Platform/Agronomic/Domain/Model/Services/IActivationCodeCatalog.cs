using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Catalog of activation codes that correspond to real issued sensor units.
///     <para>
///         Mirrors a manufacturer/distributor registry: a producer can only claim a device
///         whose code was actually issued. The format is enforced by
///         <see cref="ActivationCode"/>; this port answers whether such a code exists in
///         the issued set.
///     </para>
///     <para>
///         Implemented by <c>InMemoryActivationCodeCatalog</c> in the Infrastructure layer
///         (a small fixed whitelist for the demo fleet). Swap for a persisted/remote
///         registry without touching the domain.
///     </para>
/// </summary>
public interface IActivationCodeCatalog
{
    /// <summary>
    ///     Checks whether the supplied activation code was issued.
    /// </summary>
    /// <param name="code">A well-formed activation code.</param>
    /// <returns>
    ///     <c>true</c> if the code corresponds to an issued device unit; <c>false</c>
    ///     otherwise (including when <paramref name="code"/> is <c>null</c>).
    /// </returns>
    bool IsIssued(ActivationCode code);
}
