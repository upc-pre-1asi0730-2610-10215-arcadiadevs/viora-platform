using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Assembles Specialist REST resources from application-layer DTOs (not
///     entities — inputs are already read-model DTOs composed by
///     <c>SpecialistQueryService</c>, hence the <c>FromDto</c> naming rather
///     than the <c>FromEntity</c> convention used by sibling assemblers
///     whose inputs are genuine aggregate/entity instances).
/// </summary>
public static class SpecialistResourceFromDtoAssembler
{
    public static SpecialistResource ToResourceFromDto(SpecialistPublicProfile dto)
    {
        return new SpecialistResource(
            dto.Id,
            dto.FullName,
            dto.Role,
            dto.SuccessRate,
            dto.CaseCount,
            dto.DistanceKm,
            dto.Tags,
            dto.Availability);
    }

    public static SpecialistContactResource ToResourceFromDto(SpecialistContact dto)
    {
        return new SpecialistContactResource(dto.Id, dto.Email, dto.Phone, dto.Whatsapp);
    }
}
