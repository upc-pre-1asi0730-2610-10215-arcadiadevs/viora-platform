using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySurveillanceConfiguration(this ModelBuilder builder)
    {
        builder.Entity<SymptomDictionaryItem>().HasKey(s => s.Id);
        builder.Entity<SymptomDictionaryItem>().Property(s => s.Id).IsRequired().ValueGeneratedNever();

        builder.Entity<PestSightingReport>().HasKey(p => p.Id);
        builder.Entity<PestSightingReport>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        
        builder.Entity<PestSightingReport>().OwnsOne(p => p.PlotId, pi =>
        {
            pi.Property(p => p.Value).HasColumnName("PlotId");
        });

        builder.Entity<PestSightingReport>().OwnsOne(p => p.ReporterUserId, ru =>
        {
            ru.Property(p => p.Value).HasColumnName("ReporterUserId");
        });

        builder.Entity<PestSightingReport>().Property(p => p.RiskZone).HasConversion<string>();
        builder.Entity<PestSightingReport>().Property(p => p.ObservedSeverity).HasConversion<string>();
        builder.Entity<PestSightingReport>().Property(p => p.CalculatedRisk).HasConversion<string>();
        builder.Entity<PestSightingReport>().Property(p => p.ProbableThreat).HasConversion<string>();
        builder.Entity<PestSightingReport>().Property(p => p.Status).HasConversion<string>();

        builder.Entity<PestSightingReport>().OwnsOne(p => p.Symptoms, s =>
        {
            // Simple string conversion for Symptoms VO
            s.Property(s => s.Items).HasConversion(
                v => string.Join(",", v.Select(x => x.Description)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => new Domain.Model.ValueObjects.Symptom(x)).ToList().AsReadOnly()
            ).HasColumnName("Symptoms");
        });

        builder.Entity<Alert>().HasKey(a => a.Id);
        builder.Entity<Alert>().Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        
        builder.Entity<Alert>().OwnsOne(a => a.PlotId, pi =>
        {
            pi.Property(p => p.Value).HasColumnName("PlotId");
        });

        builder.Entity<Alert>().Property(a => a.Type).HasConversion<string>();
        builder.Entity<Alert>().Property(a => a.Severity).HasConversion<string>();
    }
}
