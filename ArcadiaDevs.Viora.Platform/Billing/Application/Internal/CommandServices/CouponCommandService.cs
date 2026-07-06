using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="Coupon" /> commands (REQ-COUP-2). FK validation
///     follows the same direct-injection pattern as
///     <c>SubscriptionCommandService</c> — no wrapper adapter around
///     <see cref="IIamContextFacade" />.
/// </summary>
public class CouponCommandService(
    ICouponRepository couponRepository,
    IIamContextFacade iamContextFacade,
    IClock clock,
    IUnitOfWork unitOfWork)
    : ICouponCommandService
{
    public async Task<Result<Coupon, Error>> Handle(
        RedeemCouponCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await iamContextFacade.ExistsUserAsync(command.UserId, cancellationToken))
            {
                return new Result<Coupon, Error>.Failure(BillingErrors.NotFound);
            }

            if (!CouponCatalog.TryGetTemplate(command.Code, out var template))
            {
                return new Result<Coupon, Error>.Failure(BillingErrors.NotFound);
            }

            // REQ-COUP-2: per-user idempotency guard — a DIFFERENT user MAY
            // redeem the same code; only a repeat by the SAME user conflicts.
            if (await couponRepository.ExistsByUserIdAndCodeAsync(command.UserId, command.Code, cancellationToken))
            {
                return new Result<Coupon, Error>.Failure(BillingErrors.ConflictError);
            }

            var validUntil = clock.UtcNow.AddDays(template.ValidityDays);
            var coupon = new Coupon(
                command.UserId,
                command.Code,
                template.Description,
                template.DiscountPercent,
                validUntil,
                template.Conditions);

            await couponRepository.AddAsync(coupon, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Coupon, Error>.Success(coupon);
        }
        catch (ArgumentException)
        {
            return new Result<Coupon, Error>.Failure(BillingErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<Coupon, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Coupon, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Coupon, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
