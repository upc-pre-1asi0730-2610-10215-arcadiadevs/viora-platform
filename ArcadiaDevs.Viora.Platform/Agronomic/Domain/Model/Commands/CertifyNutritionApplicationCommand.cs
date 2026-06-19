using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

public record CertifyNutritionApplicationCommand
{
    public long UserId { get; }
    public long PlanId { get; }
    public DateOnly ApplicationDate { get; }
    public TimeOnly ApplicationTime { get; }
    public IReadOnlyCollection<string> AppliedInputs { get; }
    public string DoseConfirmation { get; }
    public string FieldOperator { get; }
    public string? FieldNotes { get; }

    public CertifyNutritionApplicationCommand(
        long userId,
        long planId,
        DateOnly applicationDate,
        TimeOnly applicationTime,
        IEnumerable<string> appliedInputs,
        string doseConfirmation,
        string fieldOperator,
        string? fieldNotes)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be a positive number.");
            
        if (planId <= 0)
            throw new ArgumentException("Plan ID must be a positive number.");

        if (applicationDate == default)
            throw new ArgumentException("Application date is required.");
            
        if (applicationTime == default)
            throw new ArgumentException("Application time is required.");
            
        if (appliedInputs == null || !appliedInputs.Any())
            throw new ArgumentException("At least one applied input is required.");
            
        var inputsList = appliedInputs.ToList();

        if (string.IsNullOrWhiteSpace(doseConfirmation))
            throw new ArgumentException("Dose confirmation is required.");

        if (string.IsNullOrWhiteSpace(fieldOperator))
            throw new ArgumentException("Field operator is required.");

        UserId = userId;
        PlanId = planId;
        ApplicationDate = applicationDate;
        ApplicationTime = applicationTime;
        AppliedInputs = inputsList.AsReadOnly();
        DoseConfirmation = doseConfirmation;
        FieldOperator = fieldOperator;
        FieldNotes = fieldNotes;
    }
}
