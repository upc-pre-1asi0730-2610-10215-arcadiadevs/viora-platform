using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates;

/// <summary>
///     The profile aggregate root.
/// </summary>
/// <remarks>
///     One profile per user. Role is immutable after construction — enforced
///     at compile time via ctor-only setter and an ApplyUpdate signature that
///     excludes Role entirely.
/// </remarks>
public class Profile
{
    public int Id { get; }

    public int UserId { get; }

    /// <summary>
    ///     The profile role — immutable after construction.
    /// </summary>
    public ProfileRole Role { get; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public string? JobTitle { get; private set; }

    public string? Language { get; private set; }

    public string? Location { get; private set; }

    public string? SpecialtyArea { get; private set; }

    private Profile()
    {
        FullName = string.Empty;
        Email = string.Empty;
    }

    public Profile(
        int userId,
        ProfileRole role,
        string fullName,
        string email,
        string? phone = null,
        string? jobTitle = null,
        string? language = null,
        string? location = null,
        string? specialtyArea = null)
    {
        UserId = userId;
        Role = role;
        FullName = fullName;
        Email = email;
        Phone = phone;
        JobTitle = jobTitle;
        Language = language;
        Location = location;
        SpecialtyArea = specialtyArea;
    }

    /// <summary>
    ///     Applies a null-safe partial update to editable fields.
    /// </summary>
    /// <remarks>
    ///     Role is NOT a parameter — compile-time immutability per spec
    ///     REQ: Aggregate-Level Role Immutability.
    /// </remarks>
    public Profile ApplyUpdate(
        string? fullName = null,
        string? email = null,
        string? phone = null,
        string? jobTitle = null,
        string? language = null,
        string? location = null,
        string? specialtyArea = null)
    {
        if (fullName is not null) FullName = fullName;
        if (email is not null) Email = email;
        if (phone is not null) Phone = phone;
        if (jobTitle is not null) JobTitle = jobTitle;
        if (language is not null) Language = language;
        if (location is not null) Location = location;
        if (specialtyArea is not null) SpecialtyArea = specialtyArea;
        return this;
    }
}
