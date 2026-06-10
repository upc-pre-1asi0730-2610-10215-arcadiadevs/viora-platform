using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Implementation of monitoring summary query service.
/// </summary>
public class MonitoringSummaryQueryService : IMonitoringSummaryQueryService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IIoTDeviceRepository _ioTDeviceRepository;

    public MonitoringSummaryQueryService(
        IPlotRepository plotRepository,
        IIoTDeviceRepository ioTDeviceRepository)
    {
        _plotRepository = plotRepository;
        _ioTDeviceRepository = ioTDeviceRepository;
    }

    public async Task<Result<MonitoringSummaryDto, Error>> Handle(
        GetCurrentMonitoringSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var plots = (await _plotRepository.FindAllByOwnerUserIdAsync(query.UserId, cancellationToken)).ToList();
        var plotIds = plots.Select(p => (long)p.Id).ToList();
        
        var devices = plotIds.Any()
            ? (await _ioTDeviceRepository.FindAllByPlotIdsAsync(plotIds, cancellationToken)).ToList()
            : new List<IoTDevice>();

        var totalPlots = plots.Count;
        var totalDevices = devices.Count;
        var activeDevices = devices.Count(d => d.Status == IoTDeviceStatus.Active);
        var inactiveDevices = devices.Count(d => d.Status == IoTDeviceStatus.Inactive);
        var maintenanceDevices = devices.Count(d => d.Status == IoTDeviceStatus.Maintenance);
        
        var averagePlotArea = totalPlots > 0
            ? Math.Round(plots.Average(p => p.AreaSize), 2)
            : 0m;
        
        var deviceHealthPercentage = totalDevices > 0
            ? Math.Round((decimal)activeDevices / totalDevices * 100, 2)
            : 0m;

        var dto = new MonitoringSummaryDto
        {
            TotalPlots = totalPlots,
            TotalDevices = totalDevices,
            ActiveDevices = activeDevices,
            InactiveDevices = inactiveDevices,
            MaintenanceDevices = maintenanceDevices,
            AveragePlotArea = averagePlotArea,
            DeviceHealthPercentage = deviceHealthPercentage,
            ColdAccumulationIndex = 0m, // Placeholder
            YieldProjection = 0m // Placeholder
        };

        return new Result<MonitoringSummaryDto, Error>.Success(dto);
    }
}