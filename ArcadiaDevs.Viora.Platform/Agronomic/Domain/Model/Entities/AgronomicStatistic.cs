using System;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;

/// <summary>
/// Represents a snapshot of agronomic statistics for a specific plot at a specific time.
/// </summary>
public class AgronomicStatistic : IAuditableEntity
{
    public int Id { get; set; }
    public int PlotId { get; set; }
    public int UserId { get; set; }
    public DateTimeOffset MeasurementDate { get; set; }
    public decimal NdviValue { get; set; }
    public decimal AccumulatedChillHours { get; set; }
    
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}