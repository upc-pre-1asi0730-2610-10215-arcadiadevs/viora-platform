using System.Reflection;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Unit tests for the specialist-facing <see cref="InterventionRequestCommandService"/>
///     overloads — <see cref="VerifyInterventionRequestCommand"/> and
///     <see cref="DeclineInterventionRequestAsSpecialistCommand"/> (specialist-dashboard-parity).
///     Covers ownership enforcement (SpecialistId match), the documented no-status-guard
///     behavior (<see cref="InterventionRequest.Verify"/>/<see cref="InterventionRequest.Decline"/>
///     succeed from ANY status by design), and the audit-timestamp invariant: the command
///     service itself never touches <see cref="InterventionRequest.CreatedAt"/>/
///     <see cref="InterventionRequest.UpdatedAt"/> — that stamping is owned exclusively by
///     the EF Core <c>AuditableEntityInterceptor</c> (see
///     <c>InterventionRequestAuditTests</c> for the interceptor-level proof).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class InterventionRequestCommandServiceSpecialistTests
{
    private readonly IInterventionRequestRepository _interventionRequestRepository =
        Substitute.For<IInterventionRequestRepository>();
    private readonly ISpecialistRepository _specialistRepository = Substitute.For<ISpecialistRepository>();
    private readonly IIamContextFacade _iamContextFacade = Substitute.For<IIamContextFacade>();
    private readonly IExternalAgronomicService _externalAgronomicService = Substitute.For<IExternalAgronomicService>();
    private readonly IExternalSurveillanceService _externalSurveillanceService = Substitute.For<IExternalSurveillanceService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly InterventionRequestCommandService _sut;

    public InterventionRequestCommandServiceSpecialistTests()
    {
        _sut = new InterventionRequestCommandService(
            _interventionRequestRepository,
            _specialistRepository,
            _iamContextFacade,
            _externalAgronomicService,
            _externalSurveillanceService,
            _unitOfWork);
    }

    /// <summary>
    ///     Builds an <see cref="InterventionRequest"/> with a known Id via reflection
    ///     (its Id is a get-only, EF-materialized property with no setter).
    /// </summary>
    private static InterventionRequest BuildRequest(
        int id,
        int specialistId,
        int growerId = 1,
        long plotId = 1,
        InterventionStatus status = InterventionStatus.PENDING,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        var request = new InterventionRequest(
            growerId,
            plotId,
            specialistId,
            alertId: null,
            reason: "Pest sighting",
            message: "Please assess my plot");

        var idField = typeof(InterventionRequest).GetField(
            "<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField!.SetValue(request, id);

        if (status == InterventionStatus.DECLINED)
        {
            request.Decline("Pre-existing decline for test setup");
        }
        else if (status != InterventionStatus.PENDING)
        {
            var statusProperty = typeof(InterventionRequest).GetProperty(nameof(InterventionRequest.Status));
            statusProperty!.SetValue(request, status);
        }

        request.CreatedAt = createdAt;
        request.UpdatedAt = updatedAt;

        return request;
    }

    // ---------------------------------------------------------------
    // Verify (specialist)
    // ---------------------------------------------------------------

    /// <summary>
    ///     GIVEN a pending request assigned to the calling specialist
    ///     WHEN <see cref="VerifyInterventionRequestCommand"/> is handled
    ///     THEN the request transitions to AWAITING_RESPONSE, is persisted via
    ///     Update + CompleteAsync, and is returned as success.
    /// </summary>
    [Fact]
    public async Task Handle_VerifyAsSpecialist_OwnedPendingRequest_TransitionsToAwaitingResponse()
    {
        // GIVEN a pending request owned by specialist 5
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.PENDING);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new VerifyInterventionRequestCommand(Id: 10, SpecialistId: 5);

        // WHEN the specialist verifies it
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN it succeeds and the status is AWAITING_RESPONSE
        Assert.True(result.IsSuccess);
        var verified = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(InterventionStatus.AWAITING_RESPONSE, verified!.Status);
        _interventionRequestRepository.Received(1).Update(request);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN a request assigned to a DIFFERENT specialist than the caller
    ///     WHEN <see cref="VerifyInterventionRequestCommand"/> is handled
    ///     THEN <see cref="InterventionErrors.NotFound"/> is returned — ownership is
    ///     enforced by treating a mismatch identically to a missing request (no
    ///     existence leak, matches the grower-side decline convention).
    /// </summary>
    [Fact]
    public async Task Handle_VerifyAsSpecialist_WrongSpecialist_ReturnsNotFound()
    {
        // GIVEN a request owned by specialist 5
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.PENDING);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        // AND specialist 99 (not the owner) attempts to verify it
        var command = new VerifyInterventionRequestCommand(Id: 10, SpecialistId: 99);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN NotFound is returned and the request is left untouched
        Assert.True(result.IsFailure);
        Assert.Equal(InterventionErrors.NotFound, ((Result<InterventionRequest, Error>.Failure)result).Error);
        _interventionRequestRepository.DidNotReceive().Update(Arg.Any<InterventionRequest>());
    }

    /// <summary>
    ///     GIVEN a non-existent request id
    ///     WHEN <see cref="VerifyInterventionRequestCommand"/> is handled
    ///     THEN <see cref="InterventionErrors.NotFound"/> is returned.
    /// </summary>
    [Fact]
    public async Task Handle_VerifyAsSpecialist_NonExistentRequest_ReturnsNotFound()
    {
        // GIVEN no request exists for id 404
        _interventionRequestRepository.FindByIdAsync(404, Arg.Any<CancellationToken>())
            .Returns((InterventionRequest?)null);

        var command = new VerifyInterventionRequestCommand(Id: 404, SpecialistId: 5);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN NotFound is returned
        Assert.True(result.IsFailure);
        Assert.Equal(InterventionErrors.NotFound, ((Result<InterventionRequest, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN a request already in a terminal/downstream status (DECLINED)
    ///     WHEN the assigned specialist verifies it
    ///     THEN it STILL succeeds and moves to AWAITING_RESPONSE — this is DOCUMENTED,
    ///     intentional behavior (see <see cref="InterventionRequest.Verify"/> remarks):
    ///     verify/decline are not self-guarded against the current status, mirroring the
    ///     OS parity boundary. This is not a bug; it is the established no-guard
    ///     convention for this aggregate.
    /// </summary>
    [Fact]
    public async Task Handle_VerifyAsSpecialist_AlreadyDeclinedRequest_StillSucceeds_NoStatusGuard_DocumentedBehavior()
    {
        // GIVEN a request already DECLINED
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.DECLINED);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new VerifyInterventionRequestCommand(Id: 10, SpecialistId: 5);

        // WHEN the same specialist verifies the already-declined request
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN it succeeds and overwrites the status to AWAITING_RESPONSE (no guard)
        Assert.True(result.IsSuccess);
        var verified = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(InterventionStatus.AWAITING_RESPONSE, verified!.Status);
    }

    // ---------------------------------------------------------------
    // Decline (specialist)
    // ---------------------------------------------------------------

    /// <summary>
    ///     GIVEN a pending request assigned to the calling specialist
    ///     WHEN <see cref="DeclineInterventionRequestAsSpecialistCommand"/> is handled
    ///     THEN the request transitions to DECLINED with the given reason recorded.
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_OwnedPendingRequest_TransitionsToDeclined()
    {
        // GIVEN a pending request owned by specialist 5
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.PENDING);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 10, DeclineReason: "Out of service area", SpecialistId: 5);

        // WHEN the specialist declines it
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN it succeeds, status is DECLINED, and the reason is recorded
        Assert.True(result.IsSuccess);
        var declined = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(InterventionStatus.DECLINED, declined!.Status);
        Assert.Equal("Out of service area", declined.DeclineReason);
        _interventionRequestRepository.Received(1).Update(request);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN a request assigned to a DIFFERENT specialist than the caller
    ///     WHEN <see cref="DeclineInterventionRequestAsSpecialistCommand"/> is handled
    ///     THEN <see cref="InterventionErrors.NotFound"/> is returned (ownership enforced
    ///     against SpecialistId, distinct from the grower-side decline which checks
    ///     GrowerId).
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_WrongSpecialist_ReturnsNotFound()
    {
        // GIVEN a request owned by specialist 5
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.PENDING);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        // AND specialist 99 (not the owner) attempts to decline it
        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 10, DeclineReason: "Not my request", SpecialistId: 99);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN NotFound is returned and the request is left untouched
        Assert.True(result.IsFailure);
        Assert.Equal(InterventionErrors.NotFound, ((Result<InterventionRequest, Error>.Failure)result).Error);
        Assert.Equal(InterventionStatus.PENDING, request.Status);
        _interventionRequestRepository.DidNotReceive().Update(Arg.Any<InterventionRequest>());
    }

    /// <summary>
    ///     GIVEN a non-existent request id
    ///     WHEN <see cref="DeclineInterventionRequestAsSpecialistCommand"/> is handled
    ///     THEN <see cref="InterventionErrors.NotFound"/> is returned.
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_NonExistentRequest_ReturnsNotFound()
    {
        // GIVEN no request exists for id 404
        _interventionRequestRepository.FindByIdAsync(404, Arg.Any<CancellationToken>())
            .Returns((InterventionRequest?)null);

        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 404, DeclineReason: "n/a", SpecialistId: 5);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN NotFound is returned
        Assert.True(result.IsFailure);
        Assert.Equal(InterventionErrors.NotFound, ((Result<InterventionRequest, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN a blank decline reason
    ///     WHEN <see cref="DeclineInterventionRequestAsSpecialistCommand"/> is handled
    ///     THEN <see cref="InterventionErrors.ValidationError"/> is returned — the
    ///     aggregate's <see cref="InterventionRequest.Decline"/> guard throws
    ///     <see cref="ArgumentException"/>, which the command service translates.
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_BlankReason_ReturnsValidationError()
    {
        // GIVEN an owned pending request
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.PENDING);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 10, DeclineReason: "   ", SpecialistId: 5);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN ValidationError is returned
        Assert.True(result.IsFailure);
        Assert.Equal(InterventionErrors.ValidationError, ((Result<InterventionRequest, Error>.Failure)result).Error);
    }

    /// <summary>
    ///     GIVEN a request already ACCEPTED (a downstream, non-PENDING status)
    ///     WHEN the assigned specialist declines it
    ///     THEN it STILL succeeds and moves to DECLINED — documented no-status-guard
    ///     behavior, identical rationale to the Verify-side regression guard above.
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_AlreadyAcceptedRequest_StillSucceeds_NoStatusGuard_DocumentedBehavior()
    {
        // GIVEN a request already ACCEPTED
        var request = BuildRequest(10, specialistId: 5, status: InterventionStatus.ACCEPTED);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 10, DeclineReason: "Changed my mind", SpecialistId: 5);

        // WHEN the same specialist declines the already-accepted request
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN it succeeds and overwrites the status to DECLINED (no guard)
        Assert.True(result.IsSuccess);
        var declined = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(InterventionStatus.DECLINED, declined!.Status);
    }

    // ---------------------------------------------------------------
    // Audit timestamps — command-service-level invariant
    // ---------------------------------------------------------------

    /// <summary>
    ///     GIVEN a request with pre-existing CreatedAt/UpdatedAt values (as would be
    ///     hydrated from the database)
    ///     WHEN the specialist verifies it
    ///     THEN CreatedAt and UpdatedAt are left EXACTLY as they were — the command
    ///     service/aggregate never write to these properties directly. Stamping is
    ///     owned exclusively by the EF Core <c>AuditableEntityInterceptor</c> at
    ///     SaveChanges time (which is mocked away here via <see cref="IUnitOfWork"/>),
    ///     so this test documents the division of responsibility rather than
    ///     re-implementing the interceptor's logic.
    /// </summary>
    [Fact]
    public async Task Handle_VerifyAsSpecialist_DoesNotMutateAuditTimestamps_DelegatedToInfrastructure()
    {
        // GIVEN a request with known pre-existing audit timestamps
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var request = BuildRequest(
            10, specialistId: 5, status: InterventionStatus.PENDING,
            createdAt: createdAt, updatedAt: updatedAt);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new VerifyInterventionRequestCommand(Id: 10, SpecialistId: 5);

        // WHEN the specialist verifies it
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the timestamps are untouched by the command service itself
        Assert.True(result.IsSuccess);
        var verified = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(createdAt, verified!.CreatedAt);
        Assert.Equal(updatedAt, verified.UpdatedAt);
    }

    /// <summary>
    ///     Same invariant as above, exercised through the specialist decline path.
    /// </summary>
    [Fact]
    public async Task Handle_DeclineAsSpecialist_DoesNotMutateAuditTimestamps_DelegatedToInfrastructure()
    {
        // GIVEN a request with known pre-existing audit timestamps
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var request = BuildRequest(
            10, specialistId: 5, status: InterventionStatus.PENDING,
            createdAt: createdAt, updatedAt: updatedAt);
        _interventionRequestRepository.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(request);

        var command = new DeclineInterventionRequestAsSpecialistCommand(
            Id: 10, DeclineReason: "Not available", SpecialistId: 5);

        // WHEN the specialist declines it
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the timestamps are untouched by the command service itself
        Assert.True(result.IsSuccess);
        var declined = ((Result<InterventionRequest, Error>.Success)result).Value;
        Assert.Equal(createdAt, declined!.CreatedAt);
        Assert.Equal(updatedAt, declined.UpdatedAt);
    }
}
