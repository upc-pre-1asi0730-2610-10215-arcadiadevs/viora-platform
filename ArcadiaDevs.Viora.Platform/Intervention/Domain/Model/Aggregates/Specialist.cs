namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;

/// <summary>
///     The Specialist aggregate root — now a minimal Intervention-local
///     record, not a business-data catalog. Identity, contact channels, and
///     every marketplace-matching attribute (geo, tags, availability, the
///     Pro badge) are resolved live at read time from the referenced
///     <c>Profile</c> (Role=Specialist) via <c>IProfileContextFacade</c>.
/// </summary>
/// <remarks>
///     Prior to this change, this aggregate stored <c>SuccessRate</c>,
///     <c>CaseCount</c>, <c>DistanceKm</c>, <c>Tags</c>, and
///     <c>Availability</c> as ctor-immutable fields, seeded from a fixed
///     demo catalog — the exact staleness bug OS itself fixed in
///     <c>d569bbe</c> ("match real specialist profiles by geo, tags and
///     availability"). This aggregate now only persists what genuinely has
///     no home on <c>Profile</c>: the row's own id (used as the FK target
///     for <see cref="ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates.InterventionRequest" />/
///     <see cref="ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates.ServiceProposal" />
///     historically, now superseded by <see cref="ProfileUserId" /> directly
///     — see the specialist-live-matching change) and the Intervention-local
///     business contact channel <see cref="Whatsapp" />.
/// </remarks>
public class Specialist
{
    public int Id { get; }

    /// <summary>
    ///     The referenced Profile's UserId (Role=Specialist). This is now
    ///     THE specialist identity used everywhere — REST routes, matching,
    ///     intervention-request/service-proposal FKs — not <see cref="Id" />.
    /// </summary>
    public int ProfileUserId { get; }

    /// <summary>
    ///     Intervention-local business contact channel, distinct from the
    ///     Profile's generic phone/email — gated behind REQ-SPEC-2.
    /// </summary>
    public string? Whatsapp { get; }

    private Specialist()
    {
    }

    public Specialist(int profileUserId, string? whatsapp = null)
    {
        ProfileUserId = profileUserId;
        Whatsapp = whatsapp;
    }
}
