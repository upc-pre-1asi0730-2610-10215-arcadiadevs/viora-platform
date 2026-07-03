using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="SeedSpecialistsCommand" /> — idempotently seeds a
///     small fixed demo Specialist catalog, each backed by a real Profile
///     row with <c>Role=Specialist</c>. Mirrors Surveillance's
///     <c>SymptomCommandService</c> seeding shape.
/// </summary>
/// <remarks>
///     Design decision 1 (obs #267): the Profile rows are inserted directly
///     via <see cref="IProfileRepository" />, bypassing
///     <c>IProfileContextFacade.EnsureProfile</c> (which is hardcoded to
///     <c>Role=Producer</c>). This is a seed-only, bootstrap-time exception
///     to the ACL boundary — Intervention and Profile share the same
///     solution/DB, and no live specialist-signup flow exists yet to create
///     these rows through a normal request path.
/// </remarks>
public class SpecialistCommandService(
    ISpecialistRepository specialistRepository,
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
    : ISpecialistCommandService
{
    private sealed record DemoSpecialist(
        int ProfileUserId,
        string FullName,
        string Email,
        string? Phone,
        string? Whatsapp,
        double SuccessRate,
        int CaseCount,
        double DistanceKm,
        IReadOnlyList<string> Tags,
        EAvailabilityStatus Availability);

    private static readonly IReadOnlyList<DemoSpecialist> DemoCatalog = new List<DemoSpecialist>
    {
        new(90001, "Dr. Elena Marquez", "elena.marquez@viora.demo", "+34600000001", "+34600000001",
            92.5, 47, 3.2, new[] { "Xylella", "Pest Control" }, EAvailabilityStatus.AVAILABLE_TODAY),
        new(90002, "Ing. Marco Ruiz", "marco.ruiz@viora.demo", "+34600000002", "+34600000002",
            87.0, 33, 8.7, new[] { "Fungal Disease", "Irrigation" }, EAvailabilityStatus.AVAILABLE_TOMORROW),
        new(90003, "Dra. Sofia Bianchi", "sofia.bianchi@viora.demo", "+34600000003", "+34600000003",
            95.1, 61, 1.5, new[] { "Olive Fly", "Integrated Pest Management" }, EAvailabilityStatus.AVAILABLE_THIS_WEEK),
        new(90004, "Ing. Tomas Herrera", "tomas.herrera@viora.demo", "+34600000004", null,
            78.4, 19, 15.0, new[] { "Soil Health" }, EAvailabilityStatus.UNAVAILABLE)
    }.AsReadOnly();

    public async Task Handle(SeedSpecialistsCommand command, CancellationToken cancellationToken = default)
    {
        var anyAdded = false;

        foreach (var demo in DemoCatalog)
        {
            var existingProfile = await profileRepository.FindByUserIdAsync(demo.ProfileUserId, cancellationToken);
            if (existingProfile is null)
            {
                var profile = new ProfileAggregate(
                    demo.ProfileUserId,
                    ProfileRole.Specialist,
                    demo.FullName,
                    demo.Email,
                    demo.Phone);

                await profileRepository.AddAsync(profile, cancellationToken);
                anyAdded = true;
            }

            if (!await specialistRepository.ExistsByProfileUserIdAsync(demo.ProfileUserId, cancellationToken))
            {
                var specialist = new Specialist(
                    demo.ProfileUserId,
                    demo.SuccessRate,
                    demo.CaseCount,
                    demo.DistanceKm,
                    new SpecialistTags(demo.Tags),
                    demo.Availability,
                    demo.Whatsapp);

                await specialistRepository.AddAsync(specialist, cancellationToken);
                anyAdded = true;
            }
        }

        if (anyAdded)
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
