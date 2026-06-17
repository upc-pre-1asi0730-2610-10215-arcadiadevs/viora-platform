using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;

public class SymptomCommandService(
    ISymptomDictionaryItemRepository symptomRepository,
    IUnitOfWork unitOfWork)
    : ISymptomCommandService
{
    public async Task Handle(SeedSymptomsCommand command, CancellationToken cancellationToken = default)
    {
        var symptomTypes = SymptomTypes.GetAll();

        bool anyAdded = false;

        foreach (var type in symptomTypes)
        {
            if (!await symptomRepository.ExistsByIdAsync(type.Code, cancellationToken))
            {
                var entity = new SymptomDictionaryItem(type.Code, type.DescriptionEn, type.DescriptionEs);
                await symptomRepository.AddAsync(entity, cancellationToken);
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
