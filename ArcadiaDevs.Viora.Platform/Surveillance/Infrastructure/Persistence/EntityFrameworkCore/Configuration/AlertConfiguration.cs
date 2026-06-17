using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        
        builder.OwnsOne(a => a.PlotId, pi =>
        {
            pi.Property(p => p.Value).HasColumnName("PlotId");
        });

        builder.Property(a => a.Type).HasConversion<string>();
        builder.Property(a => a.Severity).HasConversion<string>();
    }
}
