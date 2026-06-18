using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

public record IngestAgronomicStatisticsCommand(
    long UserId,
    DateTimeOffset TargetDate
);
