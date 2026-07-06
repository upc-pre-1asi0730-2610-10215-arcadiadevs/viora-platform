namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;

/// <summary>
///     Outbound port for sending transactional emails from the Iam bounded
///     context (REQ-EMAIL-1). Independent of any concrete provider.
/// </summary>
public interface IEmailService
{
    /// <summary>
    ///     Whether a real provider is configured. When <c>false</c>, callers may
    ///     still invoke <see cref="SendVerificationEmailAsync" /> safely — the
    ///     implementation degrades gracefully (REQ-EMAIL-2).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    ///     Sends a verification email containing a link built from
    ///     <paramref name="token" />. Fire-and-forget best-effort: MUST NOT
    ///     throw on a configured provider's failure (REQ-EMAIL-3) and MUST NOT
    ///     block the caller when unconfigured (REQ-EMAIL-2).
    /// </summary>
    Task SendVerificationEmailAsync(
        string toEmail,
        string toName,
        string token,
        CancellationToken cancellationToken);
}
