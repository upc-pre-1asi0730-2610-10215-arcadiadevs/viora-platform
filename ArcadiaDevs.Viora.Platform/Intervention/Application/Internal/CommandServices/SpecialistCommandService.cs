using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="SeedSpecialistsCommand" /> — idempotently seeds a
///     small fixed demo Specialist catalog, each backed by a real Profile
///     row with <c>Role=Specialist</c>. Mirrors Surveillance's
///     <c>SymptomCommandService</c> seeding shape.
/// </summary>
/// <remarks>
///     Fixed after the initial WU1 review pass: the Profile rows are now
///     provisioned via <see cref="IProfileContextFacade.EnsureProfile" />
///     (extended with a <c>role</c> parameter, defaulting to
///     <c>Role=Producer</c>, so it can also provision <c>Role=Specialist</c>
///     profiles) instead of reaching directly into Profile's own
///     <c>IProfileRepository</c> — the original design's stated rationale
///     ("no facade method supports Role=Specialist") is resolved by that
///     signature extension rather than by bypassing the ACL boundary. The
///     seed is also now gated to non-Production environments only, since it
///     writes fabricated demo PII (<c>FullName</c>/<c>Email</c>/<c>Phone</c>)
///     with hardcoded <c>ProfileUserId</c> values that have no FK constraint
///     against real IAM users.
/// </remarks>
public class SpecialistCommandService(
    ISpecialistRepository specialistRepository,
    IProfileContextFacade profileContextFacade,
    IUnitOfWork unitOfWork,
    IHostEnvironment environment,
    ILogger<SpecialistCommandService> logger)
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
        if (environment.IsProduction())
        {
            logger.LogInformation("Skipping demo Specialist seed in Production environment.");
            return;
        }

        try
        {
            var anyAdded = false;

            foreach (var demo in DemoCatalog)
            {
                await profileContextFacade.EnsureProfile(
                    demo.ProfileUserId,
                    demo.FullName,
                    demo.Email,
                    demo.Phone,
                    role: ProfileRole.Specialist,
                    ct: cancellationToken);

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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database error while seeding demo specialists.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error while seeding demo specialists.");
            throw;
        }
    }
}
