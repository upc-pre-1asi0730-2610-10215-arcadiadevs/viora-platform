using System.Collections.Generic;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories
{
    /// <summary>
    /// Domain repository contract for the IoTDevice aggregate.
    /// (TS12-001)
    /// </summary>
    public interface IIoTDeviceRepository
    {
        Task<IoTDevice> SaveAsync(IoTDevice device);

        Task<IoTDevice?> FindByIdAsync(long id);

        Task<IEnumerable<IoTDevice>> FindAllByPlotIdAsync(long plotId);

        Task<IoTDevice?> FindByIdAndPlotIdAsync(long id, long plotId);

        Task<bool> ExistsByIdAndPlotIdAsync(long id, long plotId);

        Task DeleteAsync(IoTDevice device);
    }
}