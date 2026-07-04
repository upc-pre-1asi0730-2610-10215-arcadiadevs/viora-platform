using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;

/// <summary>
///     Service that handles Specialist read queries (REQ-SPEC-1..3).
/// </summary>
public interface ISpecialistQueryService
{
    /// <summary>Public profile (REQ-SPEC-1) — excludes contact fields.</summary>
    Task<Result<SpecialistPublicProfile, Error>> Handle(GetSpecialistByIdQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gated contact (REQ-SPEC-2).</summary>
    Task<Result<SpecialistContact, Error>> Handle(GetSpecialistContactQuery query, CancellationToken cancellationToken = default);

    /// <summary>Ranked candidate matching (REQ-SPEC-3).</summary>
    Task<IReadOnlyList<SpecialistPublicProfile>> Handle(GetSpecialistCandidatesQuery query, CancellationToken cancellationToken = default);
}
