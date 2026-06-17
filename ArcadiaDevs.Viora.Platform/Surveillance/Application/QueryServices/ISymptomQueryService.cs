using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;

/// <summary>
/// Service that handles queries related to the symptoms catalog.
/// </summary>
public interface ISymptomQueryService
{
    /// <summary>
    /// Retrieves all symptom dictionary items.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of symptom items.</returns>
    Task<IEnumerable<SymptomDictionaryItem>> Handle(GetAllSymptomsQuery query, CancellationToken cancellationToken = default);
}
