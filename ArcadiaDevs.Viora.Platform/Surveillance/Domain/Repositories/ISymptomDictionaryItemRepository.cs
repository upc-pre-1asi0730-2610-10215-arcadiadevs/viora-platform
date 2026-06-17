using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

/// <summary>
/// Repository interface for managing <see cref="SymptomDictionaryItem"/> entities.
/// </summary>
public interface ISymptomDictionaryItemRepository : IBaseRepository<SymptomDictionaryItem>
{
    /// <summary>
    /// Checks if a symptom item exists by its string identifier.
    /// </summary>
    /// <param name="id">The string identifier of the symptom.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the symptom exists, otherwise false.</returns>
    Task<bool> ExistsByIdAsync(string id, CancellationToken cancellationToken = default);
}
