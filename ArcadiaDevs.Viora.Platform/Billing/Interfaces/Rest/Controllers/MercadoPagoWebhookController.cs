using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

/// <summary>
///     Inbound MercadoPago payment-event receiver (REQ-GATE-4). Explicit
///     <see cref="RouteAttribute" /> override to <c>webhooks/mercado-pago</c>
///     — NOT versioned under <c>/api/v1</c> (an event receiver, not a public
///     resource) and NOT derivable from <c>KebabCaseRouteNamingConvention</c>
///     (would auto-derive a different compound path from the class name).
/// </summary>
/// <remarks>
///     <see cref="AllowAnonymousAttribute" /> is REQUIRED here, beyond the
///     tasks doc's literal text — MercadoPago sends no Bearer token, and this
///     codebase's custom <c>RequestAuthorizationMiddleware</c> rejects any
///     request without one (401) BEFORE it ever reaches a controller action,
///     unless the endpoint is decorated with this attribute. Without it,
///     REQ-GATE-4's "MUST accept any POST unconditionally and MUST always
///     return 200" would be unreachable — every real MercadoPago delivery
///     would 401 at the authorization middleware, never at this controller.
///     No other Billing controller needs this: WU1-WU5 endpoints are all
///     genuinely user-authenticated. This is the discovered exception.
/// </remarks>
[ApiController]
[Route("webhooks/mercado-pago")]
[Produces(MediaTypeNames.Application.Json)]
[AllowAnonymous]
public class MercadoPagoWebhookController(
    IWebhookReconciliationCommandService webhookReconciliationCommandService,
    ILogger<MercadoPagoWebhookController> logger) : ControllerBase
{
    /// <summary>
    ///     Receives a MercadoPago payment notification and reconciles it
    ///     (REQ-GATE-4, REQ-GATE-5). ALWAYS returns 200, regardless of the
    ///     payload's validity or the reconciliation outcome — no signature
    ///     verification (explicit accepted gap, REQ-GATE-4). Failures are
    ///     logged, never surfaced, so MercadoPago's retry mechanism sees a
    ///     stopped (200) delivery either way.
    /// </summary>
    /// <param name="resource">
    ///     Either MercadoPago's real minimal shape (<c>id</c>/<c>type</c>) or
    ///     the inline synthetic demoability payload. May be malformed or
    ///     even null — both are tolerated.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Always — the notification was received.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] WebhookPayloadResource? resource,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ReconcilePaymentCommand(
                resource?.Id,
                resource?.Type,
                resource?.PaymentId,
                resource?.ExternalReference,
                resource?.Status,
                resource?.Amount,
                resource?.Currency,
                resource?.CardBrand,
                resource?.CardLast4,
                resource?.CardExpMonth,
                resource?.CardExpYear);

            var result = await webhookReconciliationCommandService.Handle(command, cancellationToken);

            if (result is Result<Unit, Error>.Failure failure)
            {
                logger.LogWarning(
                    "MercadoPago webhook reconciliation failed with {ErrorCode}: {ErrorMessage}",
                    failure.Error.Code,
                    failure.Error.Message);
            }
        }
        catch (Exception ex)
        {
            // REQ-GATE-4: belt-and-suspenders — no exception may ever escape
            // this action, even one unrelated to reconciliation logic itself
            // (e.g. a DI/model-binding surprise). The command service already
            // maps its own known failure modes to a logged Result.Failure;
            // this catch only guards the unconditional-200 promise itself.
            logger.LogError(ex, "Unhandled exception while processing MercadoPago webhook");
        }

        return Ok();
    }
}
