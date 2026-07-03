using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The Specialist aggregate root — Intervention-local business fields
///     only (matching/contact metadata). Identity and contact channels
///     (<c>fullName</c>, <c>email</c>, <c>phone</c>) are NOT duplicated here;
///     they are resolved live at read time from the referenced
///     <c>Profile</c> (Role=Specialist) via
///     <c>IProfileContextFacade.GetProfileSummaryAsync</c> (design decision
///     1, obs #267) — this is the fix for OS's known staleness bug where
///     contact fields were duplicated onto a disconnected fake catalog.
/// </summary>
/// <remarks>
///     Every field is ctor-immutable (task 1.1). There is no
///     <c>ApplyUpdate</c> because WU1's REST surface has no
///     <c>PATCH /specialists/{id}</c> — specialists are provisioned only via
///     the startup seed (<see cref="ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands.SeedSpecialistsCommand" />)
///     until a real specialist-signup flow exists (out of this change's
///     scope, per proposal obs #263).
/// </remarks>
public class Specialist
{
    public int Id { get; }

    /// <summary>
    ///     The referenced Profile's UserId (Role=Specialist). Immutable —
    ///     mirrors REQ-CC-3's ctor-only FK convention.
    /// </summary>
    public int ProfileUserId { get; }

    public double SuccessRate { get; }

    public int CaseCount { get; }

    public double DistanceKm { get; }

    public SpecialistTags Tags { get; }

    public EAvailabilityStatus Availability { get; }

    /// <summary>
    ///     Intervention-local business contact channel, distinct from the
    ///     Profile's generic phone/email — gated behind REQ-SPEC-2.
    /// </summary>
    public string? Whatsapp { get; }

    private Specialist()
    {
        Tags = SpecialistTags.Empty;
    }

    public Specialist(
        int profileUserId,
        double successRate,
        int caseCount,
        double distanceKm,
        SpecialistTags tags,
        EAvailabilityStatus availability,
        string? whatsapp = null)
    {
        ProfileUserId = profileUserId;
        SuccessRate = successRate;
        CaseCount = caseCount;
        DistanceKm = distanceKm;
        Tags = tags ?? SpecialistTags.Empty;
        Availability = availability;
        Whatsapp = whatsapp;
    }
}
