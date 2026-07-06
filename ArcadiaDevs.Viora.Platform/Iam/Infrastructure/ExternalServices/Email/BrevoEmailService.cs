using System.Net.Http.Json;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.ExternalServices.Email.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.ExternalServices.Email;

/// <summary>
///     Brevo-backed <see cref="IEmailService" /> adapter (REQ-EMAIL-2) — the
///     sole concrete implementation of the port. Direct REST POST to Brevo's
///     transactional email API, no SDK.
/// </summary>
/// <remarks>
///     Composes the same off-by-default DI shape as
///     <c>MercadoPagoPaymentGatewayAdapter</c> — raw <see cref="HttpClient" />
///     via <c>AddHttpClient&lt;BrevoEmailService&gt;()</c> +
///     <see cref="IOptions{TOptions}" /> +
///     <c>AddOptionsWithValidateOnStart&lt;BrevoOptions&gt;()</c>. Diverges
///     behaviorally from that precedent (deliberate exception, matches OS's
///     <c>BrevoEmailService</c> exactly): email delivery must never block or
///     fail the caller. When unconfigured, the verification link is logged
///     instead of thrown; when a CONFIGURED provider call fails, the failure
///     is caught and logged only (REQ-EMAIL-3) — never surfaced to the
///     sign-up/resend command.
/// </remarks>
public class BrevoEmailService(
    HttpClient httpClient,
    IOptions<BrevoOptions> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    private readonly BrevoOptions _options = options.Value;

    /// <inheritdoc />
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    /// <inheritdoc />
    public async Task SendVerificationEmailAsync(
        string toEmail,
        string toName,
        string token,
        CancellationToken cancellationToken)
    {
        var verificationLink = $"{_options.VerificationBaseUrl}?token={token}";

        if (!IsConfigured)
        {
            logger.LogWarning(
                "Brevo is not configured; verification link for {Email}: {Link}",
                toEmail, verificationLink);
            return;
        }

        try
        {
            var body = new
            {
                sender = new { email = _options.SenderEmail, name = _options.SenderName },
                to = new[] { new { email = toEmail, name = toName } },
                subject = "Verify your Viora account",
                htmlContent = $"<p>Click <a href=\"{verificationLink}\">here</a> to verify your account.</p>"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "/v3/smtp/email")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Add("api-key", _options.ApiKey);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Brevo verification email send failed with status {StatusCode} for {Email}",
                    response.StatusCode, toEmail);
            }
        }
        catch (HttpRequestException ex)
        {
            // REQ-EMAIL-3: best-effort delivery — never fail the caller.
            logger.LogError(ex, "Network error calling Brevo for verification email to {Email}", toEmail);
        }
    }
}
