using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     EF Core repository for the <see cref="Expense"/> aggregate.
///     Standalone (does NOT extend BaseRepository&lt;T&gt;) per R13/R15.
/// </summary>
public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Expense?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Expense>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task SaveAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<Expense>()
            .FirstOrDefaultAsync(e => e.Id == expense.Id, cancellationToken);

        if (existing == null)
            await _context.Set<Expense>().AddAsync(expense, cancellationToken);
        else
            _context.Set<Expense>().Update(expense);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Expense>> FindByGrowerIdAsync(long growerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Expense>()
            .AsNoTracking()
            .Where(e => e.GrowerId == growerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Expense>> FindByPlotIdAsync(long plotId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Expense>()
            .AsNoTracking()
            .Where(e => e.PlotId == plotId)
            .ToListAsync(cancellationToken);
    }
}
