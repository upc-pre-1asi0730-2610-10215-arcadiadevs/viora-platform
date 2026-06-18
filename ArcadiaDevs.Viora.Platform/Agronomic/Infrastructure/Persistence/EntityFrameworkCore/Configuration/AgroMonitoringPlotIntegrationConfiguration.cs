using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AgroMonitoringPlotIntegrationConfiguration : IEntityTypeConfiguration<AgroMonitoringPlotIntegration>
{
    public void Configure(EntityTypeBuilder<AgroMonitoringPlotIntegration> builder)
    {
        builder.ToTable("agronomic_agro_monitoring_plot_integrations");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PlotId).IsRequired();
        builder.Property(x => x.ExternalPolygonId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BoundaryFingerprint).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ProviderImageryId).HasMaxLength(100);
        builder.Property(x => x.TileUrl).HasMaxLength(500);
        builder.Property(x => x.CaptureDate);
        builder.Property(x => x.NdviMean);
        builder.Property(x => x.CloudPercentage);
        builder.Property(x => x.LastCheckedAt);
        
        builder.HasIndex(x => x.PlotId).IsUnique();
    }
}
