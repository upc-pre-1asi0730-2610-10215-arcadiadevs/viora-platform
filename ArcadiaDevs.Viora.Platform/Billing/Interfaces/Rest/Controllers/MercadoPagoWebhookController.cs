using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

[ApiController]
[Route("webhooks/mercado-pago")]
[Produces(MediaTypeNames.Application.Json)]
[AllowAnonymous]
public class MercadoPagoWebhookController(
    IWebhookReconciliationCommandService webhookReconciliationCommandService,
    ILogger<MercadoPagoWebhookController> logger) : ControllerBase
{
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
