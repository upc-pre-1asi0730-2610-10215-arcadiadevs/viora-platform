namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     xUnit collection definition that groups all
///     <c>[Trait("Database", "Postgres")]</c> integration tests
///     into a single collection so they share the per-collection
///     Postgres container lifecycle. xUnit disables parallel
///     execution within a collection by default, which keeps
///     Testcontainers + WebApplicationFactory from racing on
///     port allocation / connection-string setup.
/// </summary>
/// <remarks>
///     Usage in a test class:
///     <code>
///     [Collection("Postgres")]
///     public class MyIntegrationTests : IntegrationTestBase
///     { ... }
///     </code>
///     Standalone test classes (not in the collection) get their
///     own <see cref="PostgresTestContainer"/> per test class —
///     useful for tests that need absolute isolation.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class HarnessCollection
{
    /// <summary>
    ///     The canonical xUnit collection name used by Testcontainers +
    ///     WebApplicationFactory integration tests. Mirrors the
    ///     <c>Database:Postgres</c> trait so the two can be cross-referenced
    ///     in CI filters.
    /// </summary>
    public const string Name = "Postgres";
}
