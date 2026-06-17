using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="ISymptomDictionaryItemRepository"/>.
/// </summary>
public class SymptomDictionaryItemRepository(AppDbContext context)
    : BaseRepository<SymptomDictionaryItem>(context), ISymptomDictionaryItemRepository
{
    /// <inheritdoc />
    public async Task<bool> ExistsByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Context.Set<SymptomDictionaryItem>().AnyAsync(s => s.Id == id, cancellationToken);
    }
}
