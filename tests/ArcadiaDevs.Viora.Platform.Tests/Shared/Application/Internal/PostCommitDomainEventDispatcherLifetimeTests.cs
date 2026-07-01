using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Application.Internal;

/// <summary>
///     Regression guard for the pre-existing DI lifetime bug in
///     <see cref="PostCommitDomainEventDispatcher"/> (obs #81).
///     <para>
///         The dispatcher is registered as <b>scoped</b> in
///         <c>Program.cs</c> because its constructor consumes
///         <c>Cortex.Mediator.IMediator</c>, which is registered as
///         <b>scoped</b> by default. A singleton dispatcher would
///         trigger the <c>Cannot consume scoped service from
///         singleton</c> validation error when the host is built
///         (which is the only reason the F1a harness surfaced the
///         bug — unit tests construct the SUT directly and skip
///         the scope validation).
///     </para>
///     <para>
///         This test boots the host via
///         <see cref="IntegrationTestBase.Factory"/> and asserts the
///         scoped lifetime contract: same instance within a scope,
///         different instances across scopes. If a future change
///         flips the production registration back to singleton, this
///         test fails at the <c>AddScoped</c> resolution step (the
///         host build itself would throw the scope-validation
///         error first, but the test makes the lifetime contract
///         explicit in code).
///     </para>
/// </summary>
[Collection("Postgres")]
public class PostCommitDomainEventDispatcherLifetimeTests : IntegrationTestBase
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Database", "Postgres")]
    public void PostCommitDomainEventDispatcher_Should_Be_Registered_As_Scoped()
    {
        // Given a WebApplicationFactory<Program> booted against a real
        // Testcontainers.PostgreSql instance (see IntegrationTestBase).
        // When I resolve the dispatcher from two different scopes.
        // Then I get different instances (scoped lifetime).
        // And within a single scope, I get the same instance (not
        // transient).
        using var scope1 = Factory.Services.CreateScope();
        using var scope2 = Factory.Services.CreateScope();

        var d1a = scope1.ServiceProvider.GetRequiredService<PostCommitDomainEventDispatcher>();
        var d1b = scope1.ServiceProvider.GetRequiredService<PostCommitDomainEventDispatcher>();
        var d2 = scope2.ServiceProvider.GetRequiredService<PostCommitDomainEventDispatcher>();

        // Same within scope (scoped, not transient).
        Assert.Same(d1a, d1b);
        // Different across scopes (scoped, not singleton).
        Assert.NotSame(d1a, d2);
    }
}
