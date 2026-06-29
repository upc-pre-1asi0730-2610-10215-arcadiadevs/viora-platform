# changelog

all notable changes to this project will be documented in this file.

the format is based on [keep a changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [semantic versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.6] - 2026-06-29

### fixed
- split two `Handle` declarations crammed onto a single line in `IIoTDeviceCommandService.cs:13` onto separate lines for readability
- `Plot.UpdateInformation` now returns `Result<Unit, Error>` instead of `Result<Plot, Error>` — validate-then-apply pattern replaces mutate-and-return-self
- swagger ui is now gated behind `IsDevelopment() || IsStaging()` — `/swagger` returns 404 in production

### added
- `Shared.Domain.Model.Unit` type for `Result` payloads with no value
- `LoggingCommandBehavior` pipeline behavior with structured logging: information on success, error on exception, with command name and elapsed milliseconds
- 5 new unit tests: 3 for `Plot.UpdateInformation` validation, 2 for `LoggingCommandBehavior` logging behavior

## [1.7.5] - 2026-06-28

### added
- new xunit test project at `tests/arcadiadevs.viora.platform.tests/` wired into `viora-platform.sln` under a `tests` solution folder
- 12 unit tests covering 13 given-when-then scenarios across the iam bounded context:
  - `HashingService`: 3 tests (non-empty hash, two-calls-differ, verify-true, verify-false)
  - `TokenService`: 6 tests (claim shape + exp window, round-trip, expired, tampered, empty, null)
  - `UserCommandService`: 3 tests (weak password, username already taken, invalid credentials) with `didnotreceive()` guards on the security-critical early-return paths
- test project structure: folders mirroring the `iam / agronomic / surveillance / shared` ddd bounded contexts (the latter three as `.gitkeep` placeholders for future slices)
- xunit + nsubstitute + coverlet test stack pinned at the package versions documented in the test project's `README.md` (xunit 2.9.3, nsubstitute 5.3.0, coverlet.collector 6.0.4, etc.)
- test project `README.md` documenting stack choice, deferred items, the `exp` assertion window (`+6d23h..+7d+5s`), and the license-drift caveat for moq and fluentassertions

### changed
- (none)

### fixed
- `TokenService.ValidateToken` now correctly distinguishes expired from invalid jwts under `JsonWebTokenHandler` 8.x, which returns a `TokenValidationResult` with `IsValid=false` instead of throwing; the pre-fix `try/catch (SecurityTokenExpiredException)` never caught and would have silently misreported every expired token as invalid
- expired-token test scenario now sets `NotBefore` and `IssuedAt` in the past (not only `Expires`) so the handler's lifetime-ordering check fires before the token-expired check

### security
- the security-critical early-return paths in `UserCommandService` are now covered by unit tests with `didnotreceive()` guards: weak password prevents hashing, username already taken prevents `AddAsync`, and invalid credentials prevents token generation
- the per-file coverage for `HashingService.cs` and `TokenService.cs` is at 100% (the security-critical jwt and password-hashing paths); aggregate coverage across the three covered files is 74.3%

## [1.7.0] - 2026-06-25

### added
- new iam (identity and access management) bounded context with jwt-based authentication
- role-based authorization with three domain roles: oliveproducer, phytosanitaryspecialist, administrator
- custom request authorization middleware (replaces addjwtbearer; performs a db lookup on every authenticated request, matching the wa-learning-center-platform reference)
- admin-only role assignment endpoint: `post /api/v1/users/{id}/roles`
- iiamcontextfacade exposing `existsuserasync` and `getrolesbyuseridasync` for cross-context user lookup from agronomic and surveillance
- startup guard that refuses to boot in production when the jwt secret is the placeholder value
- problemdetails-based error responses for all `iam.*` error codes with en + es localization (iamerrors.resx)
- curl-based smoke tests covering s1 (auth endpoints), s2 (role assignment), and s3 (cross-context facade) acceptance criteria
- opt-in documentation (`docs/iam-opt-in.md`) explaining how agronomic and surveillance should consume the facade
- addusers and addroles ef core migrations (users, roles, user_roles tables)
- directory.build.props with explicit version, assemblyversion, fileversion, and informationalversion set to 1.7.0

### changed
- requestauthorizationmiddleware now uses jwtvalidationresult to distinguish expired from invalid tokens
- httpcontext.user is now populated with role claims so `isinrole()` works as expected for `[authorize(roles="...")]`
- sign-up endpoint is admin-only when `iaspnetcore_environment=production`

### fixed
- token validation now distinguishes expired tokens from invalid tokens (was collapsing both into a single iam.tokeninvalid response)
- claimsprincipal was missing role claims; isinrole() always returned false in role-aware authorization
- missing error keys added to iamerrors.resx (en + es) for sdd verify coverage

### security
- jwt secret is now guarded at startup: production refuses to boot with the placeholder secret
- sign-up in production requires the caller to be in the administrator role

## [1.6.0] - 2026-06-17

### added
- surveillance: review alert endpoint with status transitions
- surveillance: get alert detail with full audit trail
- surveillance: get recent alerts with pagination and filtering
- surveillance: create pest sighting with geo coordinates
- community risk aggregation endpoint in surveillance
- alerts overview endpoint in surveillance
- iagronomiccontextfacade for cross-context decoupling
- align-ddd-conventions refactor across all bounded contexts

### fixed
- ef core model now uses schema-agnostic configuration to boot on render
- surveillance: 404 returned when reviewing a missing alert (was 500)

[1.7.6]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.7.5...release/1.7.6
[1.7.5]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.7.0...1.7.5
[1.7.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.6.0...1.7.0
[1.6.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.4.0...release/1.6.0
