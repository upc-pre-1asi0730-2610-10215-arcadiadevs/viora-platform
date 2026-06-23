using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record CertifyNutritionApplicationResource(
    long UserId,
    DateOnly ApplicationDate,
    TimeOnly ApplicationTime,
    IReadOnlyCollection<string> AppliedInputs,
    string DoseConfirmation,
    string FieldOperator,
    string? FieldNotes);
