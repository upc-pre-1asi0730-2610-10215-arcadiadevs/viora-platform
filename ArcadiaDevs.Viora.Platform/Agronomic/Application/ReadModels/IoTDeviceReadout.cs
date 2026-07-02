using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels;

/// <summary>
///     Read model pairing an IoT device with its current (simulated) telemetry
///     and the health/alert level derived from it, so the query side can expose
///     the operational status (user-controlled) alongside the derived health.
///     <para>
///         C# port of the OS <c>IoTDeviceReadout.java</c> (16 lines). Lives in
///         the new <see cref="ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels"/>
///         namespace (the OS has <c>application/readmodels/</c>; the WA did not
///         have a read-models folder before 1.17.0).
///     </para>
/// </summary>
/// <param name="Device">The persisted device aggregate.</param>
/// <param name="Readings">The simulated current readings (with the device-type-aware nulling already applied).</param>
/// <param name="Health">The health derived from the readings (Viora thresholds).</param>
public record IoTDeviceReadout(
    IoTDevice Device,
    SensorReadings Readings,
    GeneralHealthStatus Health);
