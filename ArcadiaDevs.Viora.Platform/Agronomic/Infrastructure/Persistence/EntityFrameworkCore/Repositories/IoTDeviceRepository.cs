using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository implementation for the <see cref="IoTDevice"/> aggregate using Entity Framework Core.
/// </summary>
/// <remarks>
///     (TS012TASK002) All read-only query methods use <c>AsNoTracking</c> for performance.
///     Write operations go through <c>SaveChangesAsync</c> via <see cref="UnitOfWork"/>.
/// </remarks>
public class IoTDeviceRepository : BaseRepository<IoTDevice>, IIoTDeviceRepository
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="IoTDeviceRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public IoTDeviceRepository(AppDbContext context) : base(context)
    {
    }

     /// <inheritdoc />
     public async Task<IoTDevice> SaveAsync(IoTDevice device)
     {
         var existingDevice = await FindByIdAsync(device.Id);
         if (existingDevice == null)
         {
             await AddAsync(device);
         }
         else
         {
             Update(device);
         }
         await Context.SaveChangesAsync();
         return device;
     }

     /// <inheritdoc />
     public async Task<IoTDevice?> FindByIdAsync(long id)
     {
         return await Context.Set<IoTDevice>()
             .AsNoTracking()
             .FirstOrDefaultAsync(d => d.Id == id);
     }

     /// <inheritdoc />
     public async Task<IEnumerable<IoTDevice>> FindAllByPlotIdAsync(long plotId)
     {
         return await Context.Set<IoTDevice>()
             .AsNoTracking()
             .Where(d => d.PlotId == plotId)
             .ToListAsync();
     }

     /// <inheritdoc />
     public async Task<IoTDevice?> FindByIdAndPlotIdAsync(long id, long plotId)
     {
         return await Context.Set<IoTDevice>()
             .AsNoTracking()
             .FirstOrDefaultAsync(
                 d => d.Id == id && d.PlotId == plotId);
     }

     /// <inheritdoc />
     public async Task<bool> ExistsByIdAndPlotIdAsync(long id, long plotId)
     {
         return await Context.Set<IoTDevice>()
             .AnyAsync(
                 d => d.Id == id && d.PlotId == plotId);
     }

     /// <inheritdoc />
     public async Task DeleteAsync(IoTDevice device)
     {
         Remove(device);
         await Context.SaveChangesAsync();
     }
}
