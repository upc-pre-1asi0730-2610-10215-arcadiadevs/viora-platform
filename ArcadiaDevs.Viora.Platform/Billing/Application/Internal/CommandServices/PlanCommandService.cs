using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="SeedPlanCatalogCommand" /> — idempotently seeds the
///     fixed Plan catalog (REQ-PLAN-1) via a Program.cs startup command
///     invocation, mirroring <c>SpecialistCommandService</c>'s seeding shape.
///     Unlike the Specialist seed, Plan rows carry no PII and no FK to any
///     other bounded context, so this seed is NOT gated to non-Production
///     environments — the catalog is real product data, not demo data.
/// </summary>
public class PlanCommandService(
    IPlanRepository planRepository,
    IUnitOfWork unitOfWork,
    ILogger<PlanCommandService> logger)
    : IPlanCommandService
{
    private sealed record CatalogPlan(
        string Code,
        string Name,
        decimal PriceAmount,
        PlanInterval Interval,
        string Tagline,
        IReadOnlyList<string> Features,
        int PlotLimit,
        int IotLimit);

    private static readonly IReadOnlyList<CatalogPlan> Catalog = new List<CatalogPlan>
    {
        new("grower-plus", "Grower Plus", 149.00m, PlanInterval.MONTHLY,
            "Growing operations with multiple fincas.",
            new[]
            {
                "Up to 10 plots", "10 IoT devices", "Daily NDVI + chill tracking",
                "Pest surveillance alerts", "Dynamic nutrition recommendations",
                "Access to crop protection specialists when risk rises"
            }, 10, 10),
        new("grower-pro", "Grower Pro", 1490.00m, PlanInterval.ANNUAL,
            "Multi-region operations at scale.",
            new[]
            {
                "Up to 50 plots", "100 IoT devices", "Daily NDVI + chill tracking",
                "Pest surveillance alerts", "Dynamic nutrition recommendations",
                "Access to crop protection specialists when risk rises"
            }, 50, 100),
        new("specialist-plus", "Specialist Plus", 79.00m, PlanInterval.MONTHLY,
            "For specialists building their case pipeline.",
            new[] { "Marketplace access", "Unlimited cases", "Specialist dashboard" }, 0, 0),
        new("specialist-pro", "Specialist Pro", 790.00m, PlanInterval.ANNUAL,
            "For specialists who want to stand out to producers.",
            new[] { "Marketplace access", "Unlimited cases", "Specialist dashboard", "Pro badge", "Priority placement" }, 0, 0)
    }.AsReadOnly();

    public async Task Handle(SeedPlanCatalogCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var anyAdded = false;

            foreach (var catalogPlan in Catalog)
            {
                if (await planRepository.ExistsByCodeAsync(catalogPlan.Code, cancellationToken))
                {
                    continue;
                }

                var plan = new Plan(
                    catalogPlan.Code,
                    catalogPlan.Name,
                    catalogPlan.PriceAmount,
                    catalogPlan.Interval,
                    catalogPlan.Tagline,
                    catalogPlan.Features,
                    catalogPlan.PlotLimit,
                    catalogPlan.IotLimit);

                await planRepository.AddAsync(plan, cancellationToken);
                anyAdded = true;
            }

            if (anyAdded)
            {
                await unitOfWork.CompleteAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database error while seeding the Plan catalog.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error while seeding the Plan catalog.");
            throw;
        }
    }
}
