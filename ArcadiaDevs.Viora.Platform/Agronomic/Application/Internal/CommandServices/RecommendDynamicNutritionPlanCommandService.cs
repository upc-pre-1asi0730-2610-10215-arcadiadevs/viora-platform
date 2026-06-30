using System;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Exceptions;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Command service that recommends a <see cref="DynamicNutritionPlan"/>
///     for a plot based on its current agronomic risk profile. A2 part 2
///     (PR-D2) refactor: the body is the 12-step sequence from design
///     §5.2.2; the pre-PR-D2 hard-coded <c>120/60/90 kg/ha</c> N/P/K
///     triple is replaced with the new OS-shaped
///     <c>foliar + K-Ca + biostimulant</c> triple driven by
///     <see cref="DynamicNutritionPolicy"/> and emitted by
///     <see cref="IDynamicNutritionPlanGenerator"/>.
///     <para>
///         The constructor is now 12-parameter explicit (was 5); the
///         parameters are the union of the 3 per-risk evaluators
///         (introduced in PR-D1), the new
///         <see cref="IAgronomicRiskTranslator"/> + the new
///         <see cref="IDynamicNutritionPlanGenerator"/>, the existing
///         <see cref="IWeatherDataService"/> (for the
///         <see cref="WeatherSnapshot"/> consumed by the profile +
///         <see cref="IHydricStressEvaluator"/>), the existing
///         repositories, the existing <see cref="IUnitOfWork"/>, the
///         existing <see cref="IMediator"/>, the existing
///         <see cref="IOptions{TOptions}"/>-bound policy options, the
///         existing <see cref="IClock"/>, the existing
///         <see cref="ChillRequirementResolver"/>, and the standard
///         <see cref="ILogger{TCategoryName}"/>. 12 parameters is
///         acceptable for a command service that consolidates 5 distinct
///         providers + 3 evaluators + the generator; a future refactor
///         could group the dependencies into a single VO, but that is
///         out of Phase 2 scope.
///     </para>
///     <para>
///         CC-7 contract: <see cref="IDynamicNutritionPlanGenerator"/>
///         throws <see cref="DynamicNutritionPlanUnavailableException"/>
///         on an empty risk set (no triggering threat). The catch block
///         at the top of <see cref="Handle"/> converts the exception
///         into <see cref="AgronomicErrors.NoTriggeringRisk"/> so the
///         REST surface sees a normal 4xx (no silent default).
///     </para>
///     <para>
///         CC-8 contract: a <c>null</c> weather snapshot from
///         <see cref="IWeatherDataService"/> propagates as
///         <see cref="AgronomicErrors.WeatherUnavailable"/> (no
///         fabricated fallback; matches the MonitoringSummaryQueryService
///         refactor from PR-C).
///     </para>
///     <para>
///         S2.7 contract: the prior <c>Active</c> plan for the same
///         plot is <see cref="DynamicNutritionPlan.Supersede"/>d BEFORE
///         the new plan is persisted. The <see cref="IUnitOfWork"/>
///         commit then saves both state changes in one transaction.
///     </para>
/// </summary>
public class RecommendDynamicNutritionPlanCommandService(
    IDynamicNutritionPlanRepository dynamicNutritionPlanRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    IPlotRepository plotRepository,
    IChillDeficitEvaluator chillDeficitEvaluator,
    ILowNdviEvaluator lowNdviEvaluator,
    IHydricStressEvaluator hydricStressEvaluator,
    IAgronomicRiskTranslator riskTranslator,
    IDynamicNutritionPlanGenerator dynamicNutritionPlanGenerator,
    IWeatherDataService weatherDataService,
    IOptions<DynamicNutritionPolicyOptions> policyOptions,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock,
    ChillRequirementResolver chillRequirementResolver,
    ILogger<RecommendDynamicNutritionPlanCommandService> logger)
    : IRecommendDynamicNutritionPlanCommandService
{
    private const decimal DefaultNdviForEmptyProfile = 0.5m;

    /// <inheritdoc />
    public async Task<Result<DynamicNutritionPlan, Error>> Handle(
        RecommendDynamicNutritionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(dynamicNutritionPlanRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(agronomicStatisticRepository);
        ArgumentNullException.ThrowIfNull(plotRepository);
        ArgumentNullException.ThrowIfNull(chillDeficitEvaluator);
        ArgumentNullException.ThrowIfNull(lowNdviEvaluator);
        ArgumentNullException.ThrowIfNull(hydricStressEvaluator);
        ArgumentNullException.ThrowIfNull(riskTranslator);
        ArgumentNullException.ThrowIfNull(dynamicNutritionPlanGenerator);
        ArgumentNullException.ThrowIfNull(weatherDataService);
        ArgumentNullException.ThrowIfNull(policyOptions);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(chillRequirementResolver);
        ArgumentNullException.ThrowIfNull(logger);

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        try
        {
            // Step 1: fetch the plot; null → PlotNotFound.
            var plot = await plotRepository.FindByIdAsync(command.PlotId, cancellationToken);
            if (plot is null || plot.IsDeleted)
            {
                return new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.PlotNotFound);
            }

            // Step 2: latest agronomic statistic (drives NDVI trend + chill portions).
            var latestStatistic = await agronomicStatisticRepository
                .FindLatestByPlotIdAsync(command.PlotId, cancellationToken);

            // Step 2.5: latest weather snapshot. A null snapshot propagates
            // as a failure (CC-8: no fabricated fallback). Mirrors the
            // MonitoringSummaryQueryService refactor (PR-C).
            var weather = await weatherDataService
                .GetCurrentWeatherSnapshotAsync(plot, cancellationToken);
            if (weather is null)
            {
                logger.LogWarning(
                    "Live weather data is unavailable for plot {PlotId}; the platform does not provide a fabricated fallback.",
                    command.PlotId);
                return new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.WeatherUnavailable);
            }

            // Steps 3-5: per-risk evaluators (PR-D1).
            var chillRequirement = chillRequirementResolver.ResolveFor(plot);
            var chillDeficit = chillDeficitEvaluator.HasDeficit(
                chillRequirement,
                latestStatistic is null ? null : (decimal?)latestStatistic.ChillPortions);
            var lowNdvi = lowNdviEvaluator.IsBelowThreshold(latestStatistic, policyOptions.Value);
            var hydricStress = hydricStressEvaluator.IsUnderStress(weather, latestStatistic);

            // Step 6: build the read-only profile. The snapshot is the
            // real one from IWeatherDataService (NOT a synthetic constant).
            // The NDVI is from the latest statistic; when no statistic
            // exists yet, fall back to 0.5m (matches the pre-PR-D2
            // default; the OS has no equivalent fallback because the OS
            // hydrates the profile from the same statistic the WA reads).
            var ndviValue = latestStatistic is null
                ? new NdviValue((double)DefaultNdviForEmptyProfile)
                : new NdviValue(latestStatistic.NdviValue);
            var profile = new AgronomicRiskProfile(
                ClimateRiskLevel: weather.ClimateRiskLevel,
                NdviValue: ndviValue,
                WeatherSnapshot: weather,
                ChillRequirement: chillRequirement,
                LatestStatistic: latestStatistic);

            // Step 7: translate the per-risk booleans + the snapshot
            // climate level into the EThreatType set the generator iterates.
            var risks = riskTranslator.Translate(
                profile.ClimateRiskLevel,
                chillDeficit,
                lowNdvi,
                hydricStress);

            // Step 8: find any prior Active plan for the same plot and
            // supersede it BEFORE the new plan is persisted (S2.7).
            var priorActive = await dynamicNutritionPlanRepository
                .FindActiveByPlotIdAsync(command.PlotId, cancellationToken);
            priorActive?.Supersede();

            // Convert the IOptions-bound config to the validated VO the
            // generator consumes. The 9-field constructor enforces the
            // same invariants as the OS DynamicNutritionPolicy.java; the
            // startup validator already guarantees the underlying options
            // pass, so this conversion cannot throw in production.
            var policy = new DynamicNutritionPolicy(
                temperatureReferenceCelsius: policyOptions.Value.TemperatureReferenceCelsius,
                highRiskNdviThreshold: policyOptions.Value.HighRiskNdviThreshold,
                moderateRiskNdviThreshold: policyOptions.Value.ModerateRiskNdviThreshold,
                highRiskWindowDays: policyOptions.Value.HighRiskWindowDays,
                extremeRiskWindowDays: policyOptions.Value.ExtremeRiskWindowDays,
                foliarSupportDosageLitersPerHectare: policyOptions.Value.FoliarSupportDosageLitersPerHectare,
                potassiumCalciumDosageKilogramsPerHectare: policyOptions.Value.PotassiumCalciumDosageKilogramsPerHectare,
                biostimulantDosageLitersPerHectare: policyOptions.Value.BiostimulantDosageLitersPerHectare,
                chillDeficitRatio: policyOptions.Value.ChillDeficitRatio);

            // Step 9: delegate to the generator. The generator throws on
            // empty risks (CC-7); the catch below converts the throw to
            // a typed failure. Any other exception falls through to the
            // outer catch as a generic GenerationError.
            var plan = dynamicNutritionPlanGenerator.GeneratePlan(
                userId: command.UserId,
                plotId: command.PlotId,
                risks: risks,
                profile: profile,
                policy: policy,
                generatedDate: now);

            // Step 10: persist the new plan.
            await dynamicNutritionPlanRepository.AddAsync(plan, cancellationToken);

            // Step 11: commit the unit of work (saves the supersede + the
            // new plan in one transaction).
            await unitOfWork.CompleteAsync(cancellationToken);

            // Step 12: publish the cross-cutting event AFTER commit,
            // preserving the existing pre-PR-D2 publish contract.
            var domainEvent = new DynamicNutritionRecommendedEvent(
                plan.Id,
                plan.PlotId,
                plan.UserId,
                plan.Rationale.TriggeringRiskLevel.ToString());
            await mediator.PublishAsync(domainEvent, cancellationToken);

            logger.LogInformation(
                "Successfully generated Dynamic Nutrition Plan {PlanId} for plot {PlotId} (risks: {RiskCount})",
                plan.Id, command.PlotId, risks.Count);

            return new Result<DynamicNutritionPlan, Error>.Success(plan);
        }
        catch (DynamicNutritionPlanUnavailableException ex)
        {
            // CC-7: the generator refused because no triggering risk was
            // observed. Surface as a normal 4xx via the typed error.
            logger.LogInformation(
                "No triggering risk observed for plot {PlotId}; refusing to generate a plan. Reason: {Reason}",
                command.PlotId, ex.Message);
            return new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.NoTriggeringRisk);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating automated Dynamic Nutrition Plan for plot {PlotId}", command.PlotId);
            return new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.GenerationError);
        }
    }
}
