using System.Security.Cryptography;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="ReferralCode" /> commands (REQ-REF-1..3). FK
///     validation follows the same direct-injection pattern as
///     <c>SubscriptionCommandService</c>/<c>CouponCommandService</c> — no
///     wrapper adapter around <see cref="IIamContextFacade" />.
/// </summary>
public class ReferralCodeCommandService(
    IReferralCodeRepository referralCodeRepository,
    IIamContextFacade iamContextFacade,
    IUnitOfWork unitOfWork)
    : IReferralCodeCommandService
{
    // REQ-REF-2: excludes ambiguous characters (0/O, 1/I/L).
    private const string CodeAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int CodeLength = 6;
    private const int MaxGenerationAttempts = 20;

    public async Task<Result<ReferralCode, Error>> Handle(
        GetOrCreateForUserCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await iamContextFacade.ExistsUserAsync(command.UserId, cancellationToken))
            {
                return new Result<ReferralCode, Error>.Failure(BillingErrors.NotFound);
            }

            // REQ-REF-1: get-or-create idempotency — a repository lookup, not
            // an aggregate self-guard.
            var existing = await referralCodeRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (existing is not null)
            {
                return new Result<ReferralCode, Error>.Success(existing);
            }

            var code = await GenerateUniqueCodeAsync(cancellationToken);
            if (code is null)
            {
                return new Result<ReferralCode, Error>.Failure(BillingErrors.InternalServerError);
            }

            var referralCode = new ReferralCode(command.UserId, code);

            await referralCodeRepository.AddAsync(referralCode, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<ReferralCode, Error>.Success(referralCode);
        }
        catch (ArgumentException)
        {
            return new Result<ReferralCode, Error>.Failure(BillingErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<ReferralCode, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<ReferralCode, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<ReferralCode, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    /// <summary>
    ///     Generates a <c>VIORA-XXXXXX</c> code via a cryptographically
    ///     secure random source, looping until <see cref="IReferralCodeRepository.ExistsByCodeAsync" />
    ///     confirms uniqueness (REQ-REF-2). Bounded by
    ///     <see cref="MaxGenerationAttempts" /> — with a ~1 billion-entry
    ///     keyspace, exhausting it is not expected in practice.
    /// </summary>
    private async Task<string?> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var candidate = GenerateCandidateCode();
            if (!await referralCodeRepository.ExistsByCodeAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string GenerateCandidateCode()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            buffer[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return $"VIORA-{new string(buffer)}";
    }
}
