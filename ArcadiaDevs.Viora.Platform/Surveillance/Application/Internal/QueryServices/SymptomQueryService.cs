using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;

public class SymptomQueryService(ISymptomDictionaryItemRepository symptomRepository) : ISymptomQueryService
{
    public async Task<IEnumerable<SymptomDictionaryItem>> Handle(GetAllSymptomsQuery query, CancellationToken cancellationToken = default)
    {
        return await symptomRepository.ListAsync(cancellationToken);
    }
}
