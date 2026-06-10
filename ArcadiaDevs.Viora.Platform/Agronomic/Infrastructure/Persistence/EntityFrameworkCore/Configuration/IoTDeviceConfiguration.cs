using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the <see cref="IoTDevice"/> aggregate root.
/// </summary>
/// <remarks>
///     (TS012TASK002) Maps the IoTDevice aggregate to the <c>iot_devices</c> table
///     in snake_case following the project convention. The <see cref="IoTDeviceStatus"/>
///     enum is stored as a varchar for readability and portability.
/// </remarks>
public class IoTDeviceConfiguration : IEntityTypeConfiguration<IoTDevice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IoTDevice> builder)
    {
        builder.ToTable("iot_devices");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.PlotId)
            .HasColumnName("plot_id")
            .IsRequired();

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        

        // Foreign key index for efficient plot-scoped lookups
        builder.HasIndex(d => d.PlotId)
            .HasDatabaseName("ix_iot_devices_plot_id");

        // Composite index used by FindByIdAndPlotId and ExistsByIdAndPlotId
        builder.HasIndex(d => new { d.Id, d.PlotId })
            .HasDatabaseName("ix_iot_devices_id_plot_id");
    }
}
