namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.ExternalServices.Email.Configuration;

/// <summary>
///     Strongly-typed options for the Brevo email adapter (REQ-EMAIL-2). Bound
///     from configuration section <see cref="SectionName" />. Validated at
///     startup by <see cref="BrevoOptionsValidator" /> — off by default, so
///     the app builds/runs with zero real credentials until an operator
///     explicitly turns this on.
/// </summary>
public class BrevoOptions
{
    /// <summary>
    ///     The configuration section path used for binding.
    /// </summary>
    public const string SectionName = "Iam:Email:Brevo";

    /// <summary>
    ///     Off by default (REQ-EMAIL-2) — the app MUST build/run with zero real
    ///     credentials until an operator explicitly turns this on.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     The Brevo API key. Required only when <see cref="Enabled" /> is
    ///     <c>true</c>.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    ///     The sender email address used on outgoing verification emails.
    /// </summary>
    public string SenderEmail { get; set; } = "no-reply@viora.app";

    /// <summary>
    ///     The sender display name used on outgoing verification emails.
    /// </summary>
    public string SenderName { get; set; } = "Viora";

    /// <summary>
    ///     The base URL the verification link is built against
    ///     (<c>{VerificationBaseUrl}?token={token}</c>).
    /// </summary>
    public string VerificationBaseUrl { get; set; } = "http://localhost:4200/verify";
}
