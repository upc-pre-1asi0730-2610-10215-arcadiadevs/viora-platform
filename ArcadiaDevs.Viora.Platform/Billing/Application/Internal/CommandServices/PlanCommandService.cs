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
        new("FREE", "Free", 0m, PlanInterval.MONTHLY,
            "Get started with basic plot monitoring.",
            new[] { "1 plot", "Weather overview", "Community alerts" }, 1, 0),
        new("BASIC", "Basic", 49.90m, PlanInterval.MONTHLY,
            "For small producers ready to go digital.",
            new[] { "5 plots", "2 IoT devices", "Pest & disease alerts", "Email support" }, 5, 2),
        new("PRO", "Pro", 129.90m, PlanInterval.MONTHLY,
            "For growing operations that need specialist support.",
            new[] { "20 plots", "10 IoT devices", "Specialist matching", "Priority support" }, 20, 10),
        new("ENTERPRISE", "Enterprise", 349.90m, PlanInterval.MONTHLY,
            "For large operations with advanced monitoring needs.",
            new[] { "100 plots", "50 IoT devices", "Dedicated account manager", "24/7 support" }, 100, 50)
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
