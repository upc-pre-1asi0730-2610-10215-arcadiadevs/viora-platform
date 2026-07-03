using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;


namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

public sealed class AuditableEntityInterceptor(IClock clock) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAt = now;
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt ??= now;
        }
    }
}