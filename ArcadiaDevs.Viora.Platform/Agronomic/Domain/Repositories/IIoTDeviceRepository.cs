using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories
{
    /// <summary>
    /// Domain repository contract for the IoTDevice aggregate.
    /// (TS12-001)
    /// </summary>
    public interface IIoTDeviceRepository
    {
        Task<IoTDevice> SaveAsync(IoTDevice device);

        /// <summary>
        ///     Adds a new <see cref="IoTDevice"/> to the change tracker
        ///     (A4 part 2). The caller MUST follow up with
        ///     <c>IUnitOfWork.CompleteAsync</c> to flush; this split lets
        ///     the command service wrap the save in a
        ///     <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
        ///     catch for the activation-code race guard.
        /// </summary>
        Task AddAsync(IoTDevice device, CancellationToken cancellationToken = default);

        Task<IoTDevice?> FindByIdAsync(long id);

        Task<IEnumerable<IoTDevice>> FindAllByPlotIdAsync(long plotId);

        Task<IoTDevice?> FindByIdAndPlotIdAsync(long id, long plotId);

        Task<bool> ExistsByIdAndPlotIdAsync(long id, long plotId);

        Task DeleteAsync(IoTDevice device);

        Task<IEnumerable<IoTDevice>> FindAllByPlotIdsAsync(
            IEnumerable<long> plotIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Checks whether any <see cref="IoTDevice"/> is already bound to
        ///     the supplied <paramref name="code"/> (A4 part 2).
        ///     <para>
        ///         Pre-flight check used by <c>IoTDeviceCommandService</c> to
        ///         reject double-claim requests before they reach the database.
        ///         The unique index on <c>iot_devices.activation_code</c> is
        ///         the defence-in-depth backstop; a Postgres 23505 on
        ///         <c>SaveChangesAsync</c> is also mapped to the same error.
        ///     </para>
        /// </summary>
        Task<bool> ExistsByActivationCodeAsync(
            ActivationCode code,
            CancellationToken cancellationToken = default);
    }
}