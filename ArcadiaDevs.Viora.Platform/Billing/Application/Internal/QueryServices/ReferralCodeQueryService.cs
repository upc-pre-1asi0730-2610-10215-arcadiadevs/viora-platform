using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.QueryServices;

/// <summary>
///     Handles the ReferralCode get-or-create read (REQ-REF-4) by delegating
///     entirely to <see cref="IReferralCodeCommandService" /> — a thin
///     wrapper, not a duplicate implementation. This is a deliberate,
///     documented idempotent side-effecting read (matches OS behavior): the
///     controller calls a query service per this codebase's REST-read
///     convention, but the underlying operation may create a row on first
///     access (REQ-REF-1).
/// </summary>
public class ReferralCodeQueryService(IReferralCodeCommandService referralCodeCommandService)
    : IReferralCodeQueryService
{
    public Task<Result<ReferralCode, Error>> Handle(
        GetOrCreateReferralCodeByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return referralCodeCommandService.Handle(new GetOrCreateForUserCommand(query.UserId), cancellationToken);
    }
}