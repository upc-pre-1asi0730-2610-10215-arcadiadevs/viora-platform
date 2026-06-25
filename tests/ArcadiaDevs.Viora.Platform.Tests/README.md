# ArcadiaDevs.Viora.Platform.Tests

## 1. Stack choice (with rationale)

This test project uses **xUnit 2.9.3** + **NSubstitute 5.3.0** + **coverlet.collector 6.0.4** + **Microsoft.NET.Test.Sdk 17.12.0** + **xunit.runner.visualstudio 2.8.2**.

**Why these and not the obvious alternatives?** The two most "obvious" .NET test libraries — **Moq** and **FluentAssertions** — both went commercial in 2025. Moq 4.22+ (Jan 2025) flipped to the Xceed commercial license. FluentAssertions 8+ followed. NSubstitute is the MIT-licensed alternative for mocking (cleaner AAA syntax than Moq's `Mock.Of` ceremony), and xUnit's native `Assert.Equal`/`Assert.False`/etc. are sufficient for this slice's readability needs. `AwesomeAssertions` is the MIT fork of FluentAssertions if a future slice needs richer assertion syntax.

xUnit 2.9.x is the production-safe pick for .NET 10; xUnit v3 is still stabilizing its `Microsoft.Testing.Platform` migration.

## 2. Deferred items

The following are intentionally out of scope for this bootstrap and land in follow-up changes:

- **`Microsoft.AspNetCore.Mvc.Testing 10.0.x`** — needs `public partial class Program { }` marker in the test project (because `Program.cs` uses top-level statements). Used for `WebApplicationFactory<Program>` integration tests in a later change.
- **`Testcontainers.PostgreSql 4.3.0`** — real PostgreSQL in Docker. Used for integration tests that need relational fidelity (FK enforcement, `Include(u => u.Roles)` semantics, transactions). The `Microsoft.EntityFrameworkCore.InMemory` provider already in the src csproj is dev-only and not a substitute for relational tests.
- **`IClock` abstraction** — would replace `DateTime.UtcNow` calls (~30+ across `Agronomic` and `Surveillance`, plus `TokenService.GenerateToken:51`). Deferred because it's a refactor touching many call sites, not a test-only change. Each test that needs deterministic time uses a generous assertion window instead.

## 3. JWT `exp` assertion window

`TokenService.GenerateToken` (line 51) hardcodes `Expires = DateTime.UtcNow.AddDays(7)`. Tests that assert on the `exp` claim use a **window of `+6d23h..+7d+5s`** relative to `DateTime.UtcNow` captured before and after the call.

**Do NOT tighten this window.** A `±1s` or `±10s` window causes flaky tests on slow CI runners. The loose window catches the regressions we actually care about (exp in the past, exp in 30 days, exp missing entirely) without flaking.

**Do NOT introduce `IClock` in this change.** That's a refactor scope, not a test scope.

## 4. License-drift caveat

When adding new test dependencies, **do not re-introduce Moq or FluentAssertions by reflex**. Both went commercial:

- **Moq** 4.22+ (Jan 2025) — Xceed commercial license.
- **FluentAssertions** 8+ — also commercial.

MIT alternatives in use or available:
- **NSubstitute** — already used (MIT, AAA syntax).
- **AwesomeAssertions** — MIT fork of FluentAssertions, drop-in replacement if richer assertion syntax is needed later.

## 5. How to run

From a Windows terminal in `D:\Projects\wa-viora-platform`:

```powershell
dotnet build viora-platform.sln
dotnet test tests\ArcadiaDevs.Viora.Platform.Tests\ArcadiaDevs.Viora.Platform.Tests.csproj --collect:"XPlat Code Coverage"
```

**Expected outcome**:
- `dotnet build` succeeds.
- `dotnet test` runs **12 tests** (3 in `HashingServiceTests`, 6 in `TokenServiceTests`, 3 in `UserCommandServiceTests`) and all pass.
- `coverlet.collector` produces a coverage report at `tests\ArcadiaDevs.Viora.Platform.Tests\TestResults\{guid}\coverage.cobertura.xml`.
- **Coverage thresholds** (per spec v1.2 Req 13):
  - `HashingService.cs` ≥ 80% line coverage
  - `TokenService.cs` ≥ 80% line coverage
  - `UserCommandService.cs` ≥ 25% line coverage (achievable at ~29.6%)
  - Aggregate across the three ≥ 50%
