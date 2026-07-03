using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

public static class SpecialistResourceFromEntityAssembler
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
