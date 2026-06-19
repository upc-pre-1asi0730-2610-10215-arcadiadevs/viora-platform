using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record NutritionApplication
{
    public DateOnly ApplicationDate { get; }
    public TimeOnly ApplicationTime { get; }
    public IReadOnlyCollection<string> AppliedInputs { get; }
    public EDoseConfirmation DoseConfirmation { get; }
    public string FieldOperator { get; }
    public string? FieldNotes { get; }

    protected NutritionApplication() { }

    public NutritionApplication(
        DateOnly applicationDate,
        TimeOnly applicationTime,
        IReadOnlyCollection<string> appliedInputs,
        EDoseConfirmation doseConfirmation,
        string fieldOperator,
        string? fieldNotes)
    {
        if (applicationDate == default)
            throw new ArgumentException("Application date is required.");
            
        if (applicationTime == default)
            throw new ArgumentException("Application time is required.");
            
        if (appliedInputs == null || !appliedInputs.Any())
            throw new ArgumentException("At least one applied input is required to certify an application.");
            
        var inputsList = appliedInputs.ToList();
        if (inputsList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Applied input names cannot be blank.");

        if (string.IsNullOrWhiteSpace(fieldOperator))
            throw new ArgumentException("Field operator is required.");

        ApplicationDate = applicationDate;
        ApplicationTime = applicationTime;
        AppliedInputs = inputsList.AsReadOnly();
        DoseConfirmation = doseConfirmation;
        FieldOperator = fieldOperator;
        FieldNotes = fieldNotes;
    }
}
