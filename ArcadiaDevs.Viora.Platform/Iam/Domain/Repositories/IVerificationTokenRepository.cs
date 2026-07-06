using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;

/// <summary>
///     The verification-token repository.
/// </summary>
public interface IVerificationTokenRepository : IBaseRepository<VerificationToken>
{
    /// <summary>
    ///     Finds a verification token by its opaque token value.
    /// </summary>
    Task<VerificationToken?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    ///     Finds all verification tokens issued to the given user.
    /// </summary>
    Task<IReadOnlyList<VerificationToken>> FindByUserIdAsync(int userId, CancellationToken cancellationToken);
}
