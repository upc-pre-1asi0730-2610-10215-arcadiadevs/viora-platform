using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices;

/// <summary>
///     MercadoPago-backed <see cref="IPaymentGateway" /> adapter (REQ-GATE-2)
///     — the sole concrete implementation of the port.
/// </summary>
/// <remarks>
///     Composes two DIFFERENT existing precedents in this codebase (design's
///     PaymentGateway Port Design section, gate-reviewed obs #316) rather
///     than copying either wholesale:
///     <list type="bullet">
///         <item>
///             HTTP-client registration shape from <c>AgroMonitoringApiClient</c>
///             — raw <see cref="HttpClient" /> injected via
///             <c>AddHttpClient&lt;MercadoPagoPaymentGatewayAdapter&gt;()</c>.
///             That class's own ctor-throw-on-missing-config style is
///             deliberately NOT copied — an unconditional throw would crash
///             startup, conflicting with REQ-GATE-2's off-by-default
///             requirement.
///         </item>
///         <item>
///             Options+Validator shape from
///             <c>AgroMonitoringWeatherDataService</c>/
///             <c>AgroMonitoringWeatherOptionsValidator</c> — ctor takes
///             <see cref="IOptions{TOptions}" />, registered via
///             <c>AddOptionsWithValidateOnStart&lt;MercadoPagoOptions&gt;()</c>.
///             Unlike that precedent, <see cref="MercadoPagoOptionsValidator" />
///             adds a genuine off-switch (see its remarks).
///         </item>
///     </list>
///     <see cref="CreateCheckoutAsync" />/<see cref="FetchPaymentAsync" /> are
///     minimal stub HTTP implementations — no live MercadoPago credentials
///     exist to validate a full request/response mapping against in this
///     slice (spec's "Design-deferred items" #1 explicitly scopes only the
///     port contract as required here).
/// </remarks>
public class MercadoPagoPaymentGatewayAdapter : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoPaymentGatewayAdapter> _logger;

    public MercadoPagoPaymentGatewayAdapter(
        HttpClient httpClient,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoPaymentGatewayAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.AccessToken);

    /// <inheritdoc />
    public async Task<Result<CheckoutSession, Error>> CreateCheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new Result<CheckoutSession, Error>.Failure(BillingErrors.PaymentGatewayNotConfigured);
        }

        try
        {
            var body = new
            {
                external_reference = request.ExternalReference,
                items = new[]
                {
                    new
                    {
                        title = request.PlanCode,
                        quantity = 1,
                        unit_price = request.Amount,
                        currency_id = request.Currency
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/checkout/preferences", body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MercadoPago checkout creation failed with status {StatusCode}",
                    response.StatusCode);
                return new Result<CheckoutSession, Error>.Failure(BillingErrors.InternalServerError);
            }

            var payload = await response.Content.ReadFromJsonAsync<MercadoPagoCheckoutResponse>(
                cancellationToken: cancellationToken);

            return payload is null
                ? new Result<CheckoutSession, Error>.Failure(BillingErrors.InternalServerError)
                : new Result<CheckoutSession, Error>.Success(
                    new CheckoutSession(payload.InitPoint, request.ExternalReference));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling MercadoPago checkout creation");
            return new Result<CheckoutSession, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentInfo, Error>> FetchPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new Result<PaymentInfo, Error>.Failure(BillingErrors.PaymentGatewayNotConfigured);
        }

        try
        {
            var response = await _httpClient.GetAsync($"/v1/payments/{paymentId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MercadoPago payment fetch failed with status {StatusCode}",
                    response.StatusCode);
                return new Result<PaymentInfo, Error>.Failure(BillingErrors.InternalServerError);
            }

            var payload = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>(
                cancellationToken: cancellationToken);

            return payload is null
                ? new Result<PaymentInfo, Error>.Failure(BillingErrors.InternalServerError)
                : new Result<PaymentInfo, Error>.Success(
                    new PaymentInfo(
                        payload.Id,
                        payload.Status,
                        payload.TransactionAmount,
                        payload.CurrencyId,
                        payload.ExternalReference));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling MercadoPago payment fetch");
            return new Result<PaymentInfo, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}

/// <summary>
///     Minimal stub deserialization shape for MercadoPago's Checkout Pro
///     preference-creation response. Internal — not a domain type, kept in
///     this file alongside its sole consumer to avoid extra file-count churn
///     on a WU flagged "Medium — watch" for its line budget.
/// </summary>
internal sealed record MercadoPagoCheckoutResponse(
    [property: JsonPropertyName("init_point")] string InitPoint);

/// <summary>
///     Minimal stub deserialization shape for MercadoPago's payment-lookup
///     response. Internal — not a domain type, kept in this file alongside
///     its sole consumer (see <see cref="MercadoPagoCheckoutResponse" />).
/// </summary>
internal sealed record MercadoPagoPaymentResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
    [property: JsonPropertyName("currency_id")] string CurrencyId,
    [property: JsonPropertyName("external_reference")] string ExternalReference);
