using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;

/// <summary>
///     Read-only cross-boundary projection of a Profile, returned by
///     <see cref="IProfileContextFacade.GetProfileSummaryAsync" />.
/// </summary>
/// <param name="FullName">The profile's full name.</param>
/// <param name="Email">The profile's email.</param>
/// <param name="Phone">The profile's phone, if set.</param>
/// <param name="Role">The profile's role.</param>
public record ProfileSummary(string FullName, string Email, string? Phone, ProfileRole Role);
