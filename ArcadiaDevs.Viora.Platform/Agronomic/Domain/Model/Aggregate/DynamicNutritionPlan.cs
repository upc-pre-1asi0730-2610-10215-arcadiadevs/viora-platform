using System;
using System.Collections.Generic;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;

/// <summary>
///     Dynamic Nutrition Plan aggregate root.
/// </summary>
public class DynamicNutritionPlan : IAuditableEntity
{
    private readonly List<NutritionInputRecommendation> _inputRecommendations = new();

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int PlotId { get; private set; }
    public ENutritionPlanStatus Status { get; private set; }
    public NutritionApplicationWindow ApplicationWindow { get; private set; } = null!;
    public PlanRationale Rationale { get; private set; } = null!;
    public DateTimeOffset GeneratedDate { get; private set; }
    public NutritionApplication? Application { get; private set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public IReadOnlyCollection<NutritionInputRecommendation> InputRecommendations => _inputRecommendations.AsReadOnly();

    protected DynamicNutritionPlan() { }

    private DynamicNutritionPlan(
        int userId,
        int plotId,
        ENutritionPlanStatus status,
        IEnumerable<NutritionInputRecommendation> inputRecommendations,
        NutritionApplicationWindow applicationWindow,
        PlanRationale rationale,
        DateTimeOffset generatedDate)
    {
        ValidateRequiredFields(inputRecommendations, applicationWindow, rationale);
        ValidateConsistency(inputRecommendations, applicationWindow, generatedDate);

        UserId = userId;
        PlotId = plotId;
        Status = status;
        _inputRecommendations.AddRange(inputRecommendations);
        ApplicationWindow = applicationWindow;
        Rationale = rationale;
        GeneratedDate = generatedDate;
    }

    public static DynamicNutritionPlan Recommend(
        int userId,
        int plotId,
        IEnumerable<NutritionInputRecommendation> inputRecommendations,
        NutritionApplicationWindow applicationWindow,
        PlanRationale rationale,
        DateTimeOffset generatedDate)
    {
        var plan = new DynamicNutritionPlan(
            userId,
            plotId,
            ENutritionPlanStatus.Active,
            inputRecommendations,
            applicationWindow,
            rationale,
            generatedDate);

        // Domain event registration is handled by the command service in C#
        return plan;
    }

    public void Supersede()
    {
        if (Status != ENutritionPlanStatus.Active)
        {
            throw new InvalidOperationException("Only an active dynamic nutrition plan can be superseded.");
        }
        Status = ENutritionPlanStatus.Superseded;
    }

    public void CertifyApplication(NutritionApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (Status != ENutritionPlanStatus.Active)
        {
            throw new InvalidOperationException("Only an active dynamic nutrition plan can be certified.");
        }

        if (Application != null)
        {
            throw new InvalidOperationException("This dynamic nutrition plan has already been certified.");
        }

        var planInputs = _inputRecommendations.Select(r => r.Value).ToList();
        var unknownInput = application.AppliedInputs.FirstOrDefault(applied => !planInputs.Contains(applied));
        if (unknownInput != null)
        {
            throw new ArgumentException($"Applied input '{unknownInput}' is not part of this plan.");
        }

        Application = application;
    }

    public bool IsActive() => Status == ENutritionPlanStatus.Active;
    public bool IsCertified() => Application != null;

    private static void ValidateRequiredFields(
        IEnumerable<NutritionInputRecommendation> inputRecommendations,
        NutritionApplicationWindow applicationWindow,
        PlanRationale rationale)
    {
        ArgumentNullException.ThrowIfNull(inputRecommendations);
        ArgumentNullException.ThrowIfNull(applicationWindow);
        ArgumentNullException.ThrowIfNull(rationale);
    }

    private static void ValidateConsistency(
        IEnumerable<NutritionInputRecommendation> inputRecommendations,
        NutritionApplicationWindow applicationWindow,
        DateTimeOffset generatedDate)
    {
        var recommendations = inputRecommendations.ToList();
        if (!recommendations.Any())
        {
            throw new ArgumentException("A dynamic nutrition plan requires at least one input recommendation.");
        }

        if (!recommendations.Any(r => r.Status == ENutritionInputStatus.Recommended))
        {
            throw new ArgumentException("A dynamic nutrition plan requires at least one RECOMMENDED input.");
        }

        if (applicationWindow.IsExpiredOn(generatedDate))
        {
            throw new ArgumentException("Application window cannot end before the plan generated date.");
        }
    }
}
