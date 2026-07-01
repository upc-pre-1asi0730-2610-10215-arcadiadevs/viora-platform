# ArcadiaDevs.Viora.Platform.Tests

## 1. Stack choice (with rationale)

This test project uses **xUnit 2.9.3** + **NSubstitute 5.3.0** + **coverlet.collector 6.0.4** + **Microsoft.NET.Test.Sdk 17.12.0** + **xunit.runner.visualstudio 2.8.2** + **Microsoft.AspNetCore.Mvc.Testing 10.0.9** + **Testcontainers.PostgreSql 4.12.0** + **WireMock.Net 1.5.62**.

**Why these and not the obvious alternatives?** The two most "obvious" .NET test libraries — **Moq** and **FluentAssertions** — both went commercial in 2025. Moq 4.22+ (Jan 2025) flipped to the Xceed commercial license. FluentAssertions 8+ followed. NSubstitute is the MIT-licensed alternative for mocking (cleaner AAA syntax than Moq's `Mock.Of` ceremony), and xUnit's native `Assert.Equal`/`Assert.False`/etc. are sufficient for this slice's readability needs. `AwesomeAssertions` is the MIT fork of FluentAssertions if a future slice needs richer assertion syntax.

xUnit 2.9.x is the production-safe pick for .NET 10; xUnit v3 is still stabilizing its `Microsoft.Testing.Platform` migration.

The Phase 3 release (1.15.0+) adds three more packages: **Microsoft.AspNetCore.Mvc.Testing** for `WebApplicationFactory<Program>`, **Testcontainers.PostgreSql** for real Postgres in Docker (FK enforcement, transactions, `Include()` semantics that InMemory can't fake), and **WireMock.Net** for stubbing the project's outbound HTTP services (AgroMonitoring weather + imagery).

## 2. Test harness

The Phase 3 test harness lives in `tests/.../TestHarness/` and provides the foundation for all F1b-F6b integration + controller tests:

- **`IntegrationTestBase`** — abstract base for tests that need a `WebApplicationFactory<Program>` wired to a Testcontainers.PostgreSql instance. Subclasses get the resolved `Factory` + `PostgresConnectionString` properties.
- **`PostgresTestContainer`** — concrete `Testcontainers.PostgreSql` configuration (image `postgres:16-alpine`, port 0 for dynamic allocation, DB name `viora_test_{guid}`).
- **`TestcontainersFixture<TContainer>`** — xUnit `IAsyncLifetime` fixture that owns a container for the lifetime of a test class.
- **`HarnessCollection`** — `[CollectionDefinition("Postgres")]` for grouping Testcontainers tests so they share a single container and don't race.
- **`HarnessSmokeTest`** — minimal smoke test that boots the host against a Testcontainer; verifies the DI graph + Testcontainers wireup.
- **`FakeClock`** — deterministic `IClock` test double with `Set/Advance/With` API. Thread-safe (lock-protected). Default seed 2026-06-30.
- **`TestAuthHelper`** — extension method on `HttpClient` that injects `X-Test-User-Id/Name/Roles` headers. The headers are read by a test-only middleware (TODO F1b) that populates `HttpContext.Items["User"]` and `HttpContext.User` exactly like the production `RequestAuthorizationMiddleware` would after validating a real JWT. This is auth-agnostic — the future SHARED-015 `AddJwtBearer` migration will swap the production middleware without rewriting any test.
- **`InMemoryRepositories`** — static factory for in-memory `DbContext` + NSubstitute-backed repository fakes. For fast command-service / query-service unit tests that don't need the full Testcontainers harness.
- **`WireMockBuilders`** — static factory for `WireMockServer` instances that fake the project's outbound HTTP services (`IWeatherDataService` + `IAgroMonitoringImageryService`). Default stubs return empty 200 JSON; tests override per-scenario.
- **`appsettings.Test.json`** — test configuration that forces `Database:Provider=Postgres` and sets a stable JWT secret.

## 3. Testcontainers + Docker daemon

**Docker is required.** The Testcontainers-based test classes (any class with `[Trait("Database", "Postgres")]`) require a Docker daemon to be running.

Local development: install Docker Desktop or Docker Engine. The harness starts one container per `[CollectionDefinition("Postgres")]` collection (or per test class if standalone).

**To skip the Testcontainers tests locally** (run only InMemory + unit tests):

```
dotnet test --filter "Database!=Postgres"
```

This runs only the InMemory + unit tests, which do NOT require Docker.

CI: when CI is added, the runner must have a Docker daemon.

## 4. Test category traits

Every new test class in F1b-F6b MUST carry `[Trait("Category", "Unit|Integration|Persistence|Smoke")]`. Testcontainers tests additionally carry `[Trait("Database", "Postgres")]`. InMemory tests carry `[Trait("Database", "InMemory")]`. The traits are the CI split + coverage filter mechanism. Example:

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "Postgres")]
public class MonitoringSummaryQueryServiceTests : IntegrationTestBase { ... }
```

## 5. Test naming (existing convention)

- Test classes: `{ClassName}Tests` (e.g. `PlotRepositoryTests`, `UserCommandServiceTests`).
- Test methods: `Method_Scenario_ExpectedResult` (e.g. `Handle_DeletePlot_WithDynamicNutritionPlan_TriggersLogicalDeletion`).
- The 12 existing IAM tests follow this pattern (per `UserCommandServiceTests.cs`); the 9 A3 tests in `PlotRepositoryTests.cs` + the 3 A3 tests in `PlotCommandServiceDeletePlotTests.cs` follow it too.

## 6. Assertion style

xUnit native `Assert.Equal` / `Assert.True` / `Assert.False` / `Assert.Contains` / `Assert.ThrowsAnyAsync<T>`. NO FluentAssertions (commercial). `AwesomeAssertions` is the MIT fork if richer syntax is needed in a future slice.

## 7. Mocking

NSubstitute 5.3.0. NO Moq (commercial). The `Substitute.For<T>()` + `sub.Received(1).Method(...)` + `sub.DidNotReceive().Method(...)` pattern is the standard.

## 8. JWT `exp` assertion window

`TokenService.GenerateToken` (line 51) hardcodes `Expires = DateTime.UtcNow.AddDays(7)`. Tests that assert on the `exp` claim use a **window of `+6d23h..+7d+5s`** relative to `DateTime.UtcNow` captured before and after the call.

**Do NOT tighten this window.** A `±1s` or `±10s` window causes flaky tests on slow CI runners. The loose window catches the regressions we actually care about (exp in the past, exp in 30 days, exp missing entirely) without flaking.

## 9. License-drift caveat

When adding new test dependencies, **do not re-introduce Moq or FluentAssertions by reflex**. Both went commercial:

- **Moq** 4.22+ (Jan 2025) — Xceed commercial license.
- **FluentAssertions** 8+ — also commercial.

MIT alternatives in use or available:
- **NSubstitute** — already used (MIT, AAA syntax).
- **AwesomeAssertions** — MIT fork of FluentAssertions, drop-in replacement if richer assertion syntax is needed later.

## 10. How to run

From a Windows terminal in `D:\Projects\wa-viora-platform`:

```powershell
dotnet build viora-platform.sln
dotnet test tests\ArcadiaDevs.Viora.Platform.Tests\ArcadiaDevs.Viora.Platform.Tests.csproj --collect:"XPlat Code Coverage"
```

**Expected outcome** (post-Phase 3 / 1.15.0):
- `dotnet build` succeeds.
- `dotnet test` runs ~107 tests (96 pre-Phase 3 + 11 new harness helper tests) and all pass.
- `coverlet.collector` produces a coverage report at `tests\ArcadiaDevs.Viora.Platform.Tests\TestResults\{guid}\coverage.cobertura.xml`.
- **Coverage targets** (per Phase 3 proposal #73): Agronomic 55%, Iam 75%, Surveillance 60%, Shared 80%, Overall 35%.

## 11. Production di lifetime: postcommitdomaineventdispatcher (resolved in 1.15.1)

The production `PostCommitDomainEventDispatcher` (added in Phase 2 PR-F / 1.14.0) was originally registered as **Singleton** but its constructor consumes a scoped `Cortex.Mediator.IMediator`. This was a pre-existing production bug that was invisible to the existing 96 unit tests (which never boot the host) — the DI scope validator only runs when the host is built via `WebApplicationFactory<Program>`.

**Resolution (release 1.15.1):** the dispatcher is now natively registered as **Scoped** in `Program.cs` (it consumes a scoped `IMediator` from `Cortex.Mediator`, so the lifetime must match). The F1a workaround that demoted the dispatcher to `Scoped` in `IntegrationTestBase.ConfigureTestServices` has been removed — the new `PostCommitDomainEventDispatcherLifetimeTests` (regression guard) asserts the production lifetime contract: same instance within a scope, different instances across scopes. Tracked in engram obs #81.
