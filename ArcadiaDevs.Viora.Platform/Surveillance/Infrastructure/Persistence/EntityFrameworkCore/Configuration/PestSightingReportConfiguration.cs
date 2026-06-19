using System;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class PestSightingReportConfiguration : IEntityTypeConfiguration<PestSightingReport>
{
    public void Configure(EntityTypeBuilder<PestSightingReport> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        
        builder.OwnsOne(p => p.PlotId, pi =>
        {
            pi.Property<long>("PestSightingReportId").HasColumnName("id");
            pi.Property(p => p.Value).HasColumnName("PlotId");
        });

        builder.OwnsOne(p => p.ReporterUserId, ru =>
        {
            ru.Property<long>("PestSightingReportId").HasColumnName("id");
            ru.Property(p => p.Value).HasColumnName("ReporterUserId");
        });

        builder.Property(p => p.RiskZone).HasConversion<string>();
        builder.Property(p => p.ObservedSeverity).HasConversion<string>();
        builder.Property(p => p.CalculatedRisk).HasConversion<string>();
        builder.Property(p => p.ProbableThreat).HasConversion<string>();
        builder.Property(p => p.Status).HasConversion<string>();

        builder.Property(p => p.Symptoms)
            .HasConversion(
                v => string.Join(",", v.Items.Select(x => x.Description)),
                v => Domain.Model.ValueObjects.Symptoms.FromDescriptions(v.Split(',', StringSplitOptions.RemoveEmptyEntries))
            )
            .HasColumnName("Symptoms");
    }
}
