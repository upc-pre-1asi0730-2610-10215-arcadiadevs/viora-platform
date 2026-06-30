using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the <see cref="IoTDevice"/> aggregate root.
/// </summary>
/// <remarks>
///     (TS012TASK002 + AGRO-002) Maps the IoTDevice aggregate to the
///     <c>iot_devices</c> table in snake_case following the project convention.
///     The <see cref="IoTDeviceStatus"/> enum is stored as a varchar for
///     readability and portability. The aggregate uses private setters and
///     exposes a <c>Create</c> factory; EF Core is told to read and write the
///     backing fields directly via <see cref="EntityTypeBuilder{TEntity}.UsePropertyAccessMode"/>,
///     so the private setters are never bypassed at runtime.
/// </remarks>
public class IoTDeviceConfiguration : IEntityTypeConfiguration<IoTDevice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IoTDevice> builder)
    {
        builder.ToTable("iot_devices");

        // AGRO-002: read/write backing fields directly so the private setters
        // on the aggregate are not bypassed. Required for the factory-method +
        // private-setter hardening pattern.
        builder.UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(d => d.PlotId)
            .HasColumnName("plot_id")
            .IsRequired();

        builder.Property(d => d.DeviceName)
            .HasColumnName("device_name")
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
