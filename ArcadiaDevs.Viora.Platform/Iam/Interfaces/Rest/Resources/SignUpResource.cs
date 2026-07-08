namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

public record SignUpResource(
    string Username,
    string Password,
    string Email,
    string FullName,
    string? Role = null,
    string? ReferralCode = null,
    string? Phone = null);