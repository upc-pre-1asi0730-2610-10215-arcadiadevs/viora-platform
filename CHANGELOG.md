# changelog

all notable changes to this project will be documented in this file.

the format is based on [keep a changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [semantic versioning](https://semver.org/spec/v2.0.0.html).

## [1.12.0] - 2026-06-30

### added
- `Agronomic/Domain/Model/Services/IYieldForecastEstimator.cs` — new domain port `IYieldForecastEstimator.Estimate(Plot, AgronomicStatistic?, ChillRequirement, DynamicNutritionPolicyOptions) -> decimal` (A1, PR-C). Pure-function port of the OS `YieldForecastEstimator.java`; signature follows design §5.1 (engram #45).
- `Agronomic/Domain/Model/Services/YieldForecastEstimator.cs` — `sealed class` implementation registered as a singleton in `Program.cs`. Math: `yieldTonnes = baseYield × clamp(0.5 + 0.7·ndvi, 0.5, 1.2) × min(1, accumulatedChill / requirementChill)`, rounded to 2 decimals. Base yield 5.5 t/ha matches the OS `YieldEstimationPolicy` default. The policy is part of the signature so the estimator is deterministic per configuration and unit-testable without DI.
- `Agronomic/Infrastructure/Configuration/DynamicNutritionPolicyOptions.cs` — `public sealed class` bound from the new `Agronomic:DynamicNutrition` configuration section (8 fields mirroring the OS `DynamicNutritionPolicy.java` shape: `TemperatureReferenceCelsius`, `HighRiskNdviThreshold`, `ModerateRiskNdviThreshold`, `HighRiskWindowDays`, `ExtremeRiskWindowDays`, `FoliarSupportDosageLitersPerHectare`, `PotassiumCalciumDosageKilogramsPerHectare`, `BiostimulantDosageLitersPerHectare`). Defaults: 20.0 / 0.30 / 0.50 / 14 / 21 / 2.5 / 3.0 / 1.2.
- `Agronomic/Infrastructure/Configuration/DynamicNutritionPolicyOptionsValidator.cs` — `IValidateOptions<DynamicNutritionPolicyOptions>` enforcing the OS invariants at startup (CC-5 fail-fast): NDVI thresholds in [-1, 1] with `HighRiskNdviThreshold < ModerateRiskNdviThreshold`, `windowDays >= 1`, all dosages strictly positive.
- `Program.cs` — registers `IYieldForecastEstimator` as a singleton and binds `DynamicNutritionPolicyOptions` via `AddOptionsWithValidateOnStart<DynamicNutritionPolicyOptions>().Bind(...)`. The validator is registered as a singleton `IValidateOptions<T>` and runs on startup; an invalid config aborts the host. The same options class is reused by `IDynamicNutritionPlanGenerator` in PR-D2 (single source of truth per design §5.2.1).
- `appsettings.json` — new `Agronomic:DynamicNutrition` section with the 8 OS-default values.

### changed
- `Agronomic/Application/Internal/QueryServices/MonitoringSummaryQueryService.cs` — the three hard-coded `simulatedNdvi = 0.65m`, `simulatedYieldProjection = 4500m`, and `simulatedWeather = new WeatherSnapshot(22.5m, WeatherStatus.Sunny, ...)` literals (and the `120.5m` chill fallback) are removed. The resource values now reflect real provider-backed reads:
  - NDVI = the latest `AgronomicStatistic.NdviValue` for the representative plot, or `0m` if no statistic exists.
  - Yield = `_yieldForecastEstimator.Estimate(representative, latestStatistic, chillRequirement, _policy.Value)`.
  - Weather = `await _weatherDataService.GetCurrentWeatherSnapshotAsync(representative, ct)`; a `null` snapshot propagates as `AgronomicErrors.WeatherUnavailable` (no fabricated `22.5m/Sunny/Medium` fallback).
  - Chill hours fall back to `0m` + a `Warning`-level log line when no plot has AgroMonitoring data (the legacy `120.5m` literal is gone; there is no fabricated-data fallback, CC-8).
  - The constructor now injects 5 new dependencies: `IWeatherDataService`, `IYieldForecastEstimator`, `IAgronomicStatisticRepository`, `IOptions<DynamicNutritionPolicyOptions>`, and `ChillRequirementResolver`. The representative-plot selection is deterministic (`OrderByDescending(IsActive).ThenBy(Id).First()`).
- Acceptance gate: `grep -rn "0.65m\|4500m\|22.5m\|120.5m" ArcadiaDevs.Viora.Platform/Agronomic/Application/Internal/QueryServices/MonitoringSummaryQueryService.cs` returns 0 matches (the spec's acceptance gate, engram #43 §A1).

### notes
- No tests written and no `dotnet test` run during this PR (Phase 2 user decision, engram #50). Build sanity only (`dotnet build`).
- No schema change; no EF migration; the only DB impact is on the AgronomicStatistic reads (read-only).

## [1.11.2] - 2026-06-30

### added
- `Agronomic/Domain/Model/Aggregates/IoTDevice.cs` — new `ActivationCode? ActivationCode { get; private set; }` property (A4 part 2). Nullable at the persistence boundary so legacy devices pre-dating the catalog can keep `null`; new devices created via the new `Claim` factory always carry a non-null code. The legacy `Create` factory is unchanged and continues to emit devices without an `ActivationCode` (back-compat).
- `Agronomic/Domain/Model/Aggregates/IoTDevice.cs` — new `static Result<IoTDevice, Error> Claim(long plotId, string deviceName, ActivationCode code, IClock clock)` factory (A4 part 2). Mirrors the existing `Create` factory with the additional `code == null` → `ACTIVATION_CODE_REQUIRED` invariant. The device is emitted in `IoTDeviceStatus.Pending` and the `ActivationCode` is bound to the aggregate.
- `Agronomic/Application/Internal/CommandServices/IoTDeviceCommandService.cs` — `Handle(CreateIoTDeviceCommand)` now performs the full parse-check-claim-save flow against the `IActivationCodeCatalog` from PR-B1 (A4 part 2):
  1. Parse the `ActivationCode` VO from the command string; `ArgumentException` is caught and surfaced as `AgronomicErrors.InvalidActivationCodeFormat`.
  2. `_catalog.IsIssued(code)` returns `false` → `AgronomicErrors.ActivationCodeNotRecognized` (the code is well-formed but not in the issued-code catalog).
  3. `_repository.ExistsByActivationCodeAsync(code, ct)` returns `true` → `AgronomicErrors.ActivationCodeAlreadyClaimed` (the pre-flight guard against double-claim).
  4. `IoTDevice.Claim(...)` propagates any factory failure (e.g. `PLOT_ID_REQUIRED` / `DEVICE_NAME_REQUIRED` / `ACTIVATION_CODE_REQUIRED`).
  5. `_repository.AddAsync(device)` + `_unitOfWork.CompleteAsync(ct)`; the race guard catches `DbUpdateException` wrapping a Postgres 23505 SQLSTATE on the `ix_iot_devices_activation_code` index and maps it to the same `AgronomicErrors.ActivationCodeAlreadyClaimed` failure.
  The constructor now also injects `IUnitOfWork` (replacing the legacy `SaveAsync` direct-save path) so the race guard can wrap the save in a try/catch.
- `Agronomic/Domain/Repositories/IIoTDeviceRepository.cs` — new `Task<bool> ExistsByActivationCodeAsync(ActivationCode code, CancellationToken)` (A4 part 2). The interface also gains `Task AddAsync(IoTDevice device, CancellationToken)` so the command service can call `Add` + `CompleteAsync` separately, letting the race guard wrap the save.
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Repositories/IoTDeviceRepository.cs` — `ExistsByActivationCodeAsync` implementation: `Context.Set<IoTDevice>().AsNoTracking().AnyAsync(d => d.ActivationCode != null && d.ActivationCode.Value == code.Value, cancellationToken)`. `AddAsync` is inherited from `BaseRepository<IoTDevice>`.
- `Agronomic/Resources/AgronomicMessages.resx` + `AgronomicMessages.es.resx` — 3 new error keys: `InvalidActivationCodeFormat` (en) / `Formato de código de activación inválido` (es), `ActivationCodeNotRecognized` (en) / `Código de activación no reconocido` (es), `ActivationCodeAlreadyClaimed` (en) / `Código de activación ya canjeado` (es).
- `Agronomic/Domain/Model/Errors/AgronomicErrors.cs` — 3 new `static readonly Error` constants matching the resx keys: `InvalidActivationCodeFormat`, `ActivationCodeNotRecognized`, `ActivationCodeAlreadyClaimed`. The codes follow the existing `Agronomic.<Name>` convention.
- `Migrations/20260630055455_AddIoTDeviceActivationCode.cs` — new EF Core migration that adds a nullable `activation_code` `varchar(20)` column to `iot_devices` and a unique index `ix_iot_devices_activation_code` (A4 part 2 schema change). The column is nullable in v1 so the migration is safe to apply on a populated database; the unique index is the backstop against double-claim races that slip past the pre-flight `ExistsByActivationCodeAsync` check.

### changed
- `Agronomic/Interfaces/Rest/Resources/CreateIoTDeviceResource.cs` — `POST /api/v1/plots/{plotId}/iot-devices` request body now **requires** `activationCode` (was optional in v1.11.1). The new field is decorated with `[Required]` and `[StringLength(20)]` to match the column shape.
- `Agronomic/Domain/Model/Commands/CreateIoTDeviceCommand.cs` — `CreateIoTDeviceCommand` ctor now requires a non-blank `activationCode` parameter and validates it; the assembler (`CreateIoTDeviceCommandFromResourceAssembler.cs`) passes the resource's `activationCode` through. Existing callers that omit the field will throw `ArgumentException` at construction time.
- **Deployment runbook** — `iot_devices.activation_code` is added as a nullable column. Operators must either (a) backfill the column for every existing device with the activation code that corresponds to the device's IoT unit, or (b) deactivate the existing devices in the field. New devices created via the API are always claimed against a real issued code. **There is no automatic backfill** in this migration; the choice between (a) and (b) is an operational one.

### notes
- **No tests written** (Phase 2 user decision, engram #50). The 84 existing xUnit tests in `tests/ArcadiaDevs.Viora.Platform.Tests/Iam/` are untouched and were not re-run as part of this PR. Behavioural verification rests on code review and manual smoke. `sdd-verify` at the end of Phase 2 will mark A4 part 2 as "not verified, no test coverage"; this is accepted.
- **One deviation from the spec**: the design sketch in engram #45 specified `public ActivationCode ActivationCode { get; private set; } = null!;` (non-nullable with a null-forgiving initializer). The implemented property is `ActivationCode? ActivationCode { get; private set; }` (nullable) so that the EF Core materializer can correctly hydrate legacy `iot_devices` rows whose `activation_code` is `NULL` (pre-PR-B2 devices). The `null!` pattern would compile but would throw `NullReferenceException` at materialization time for legacy rows; the nullable shape matches the migration's `nullable: true` column definition and keeps the legacy `IoTDevice.Create` factory path safe.
- **One deviation from the spec**: the design sketch in engram #45 §7.3 specified `builder.HasIndex(d => d.ActivationCode.Value).IsUnique()` in the EF configuration. EF Core 9 rejects the `.Value` navigation as an invalid member-access expression (the index expression must be a direct property/field access), so the unique index is declared in the migration's `Up` method instead of in the configuration. The configuration still maps the column (name + converter + max length + nullable) so the model snapshot is consistent.

## [1.11.0] - 2026-06-30

### changed
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Repositories/PlotRepository.cs` — `HasRelatedOperationalRecordsAsync` now short-circuits across all 3 intra-BC aggregates that own a `PlotId` foreign key: `IoTDevice`, `DynamicNutritionPlan`, and `AgronomicStatistic` (A3). Previously the method checked `IoTDevice` only, so a plot with a `DynamicNutritionPlan` or `AgronomicStatistic` (but no IoT devices) would be physically deleted and leave orphan FK references in those tables. The new behaviour routes those plots through logical deletion (`Plot.Deactivate()`) via the existing `PlotDeletionPolicy`. The XML doc carries a `TODO AGRONOMIC-A3-CROSSBC` note marking `SHARED-015` (`IAgronomicContextFacade`) as the deferred resolution for cross-BC `Alert` and `PestSightingReport` checks; per locked decision #2 in engram #42, cross-BC checks remain a known limitation until SHARED-015 lands.

### notes
- **No tests written** (Phase 2 user decision, engram #50). The 84 existing xUnit tests in `tests/ArcadiaDevs.Viora.Platform.Tests/Iam/` are untouched and were not re-run as part of this PR. Behavioural verification rests on code review and manual smoke. `sdd-verify` at the end of Phase 2 will mark A3 as "not verified, no test coverage"; this is accepted.

## [1.10.0] - 2026-06-29

### added
- `Surveillance/Domain/Model/Events/AlertGeneratedIntegrationEvent.cs` — cross-BC `record` carrying primitive `long PlotId`, `long AlertId`, `string ThreatType`, `DateTime GeneratedAt`. CC-1 xml-doc on every field documents "primitive transport, recipient must wrap in its own PlotId/UserId VO". Published by the Surveillance BC on the in-process `Cortex.Mediator` bus (`IEvent`/`IEventHandler<T>`) when an `Alert` is created with `ThreatType == PHENOLOGICAL_RISK` (SURV-002). The post-commit publish is fire-and-forget; an event-bus failure surfaces as a `Result.Failure` (matches the existing `AlertCreatedEvent` error model).
- `Agronomic/Application/Internal/EventHandlers/AlertGeneratedIntegrationEventHandler.cs` — Agronomic-side `IEventHandler<AlertGeneratedIntegrationEvent>` (SURV-002). Filters on `ThreatType == PHENOLOGICAL_RISK` (case-insensitive); no-op for any other threat type. For matching events, wraps the primitive `PlotId` in `Agronomic.Domain.Model.ValueObjects.PlotId` (CC-1) and calls `IRecommendDynamicNutritionPlanCommandService.Handle(RecommendDynamicNutritionCommand(alertId, plotId))`. Handler is auto-registered via `AddCortexMediator` assembly scan. Handler exceptions are logged and swallowed per CC-2 (the originating alert in Surveillance is already committed; no retry, no DLQ).
- `Surveillance/Domain/Model/Queries/GetAlertsByUserIdQuery.cs` — `record(long UserId, string? Sort, int Limit)`. New query type for the SURV-003 sort fix.
- 4 new `POST /api/v1/alerts/{id}/{action}` endpoints on `AlertsController` (SURV-003, all class-level `[Authorize]`, all map `Result<Unit,Error>` to RFC 7807 `ProblemDetails` per CC-6):
  - `POST /api/v1/alerts/{id}/confirm` — calls `Alert.ConfirmFromInspection()` (any non-terminal → `UNDER_REVIEW`, severity +1).
  - `POST /api/v1/alerts/{id}/dismiss` — calls `Alert.Dismiss()` (any non-`DISMISSED` → `DISMISSED`, terminal).
  - `POST /api/v1/alerts/{id}/escalate` — calls `Alert.Escalate()` (severity +1, no state change).
  - `POST /api/v1/alerts/{id}/link-report?reportId={reportId}` — calls `Alert.LinkReport(PestSightingReportId)` (attaches the report, no state change).
- 4 new command records in `Surveillance/Domain/Model/Commands/MarkAlertAsReviewedCommand.cs` — `ConfirmAlertCommand`, `DismissAlertCommand`, `EscalateAlertCommand`, `LinkAlertReportCommand`. Each is the in-process command shape consumed by `IAlertCommandService`.
- 4 new `Handle(...)` overloads on `IAlertCommandService` (returning `Task<Result<Unit, Error>>`) and matching implementations on `AlertCommandService`. Each loads the alert via the repository, applies the state-machine method, persists on success, and surfaces the state-machine `Result<Unit, Error>` directly (so `ALERT_TERMINAL` failures propagate as 4xx).
- 15 new xUnit tests across 3 test files (TDD strict mode):
  - `tests/.../Surveillance/Application/Internal/CommandServices/AlertCommandServiceCrossBcEventTests.cs` — 1 fact + 5 theory cases pinning the publish-on-`PHENOLOGICAL_RISK` and no-publish-on-`PEST_SYMPTOM`/`CLIMATE_EXTREME`/`CHILL_DEFICIT`/`WATER_STRESS`/`UNKNOWN` behaviour.
  - `tests/.../Agronomic/Application/Internal/EventHandlers/AlertGeneratedIntegrationEventHandlerTests.cs` — 1 fact + 4 theory cases pinning the recommend-on-`PHENOLOGICAL_RISK` and no-call-on-other-threat-type behaviour (with CC-1 wrap verification on the `Agronomic.PlotId` value).
  - `tests/.../Surveillance/Interfaces/Rest/Controllers/AlertsControllerStateTransitionTests.cs` — 4 controller tests: `Confirm_OnValid_Returns200`, `Confirm_OnDismissed_Returns400ProblemDetails`, `GET_Alerts_SortBySeverity_NotEmptyList`, `GET_Alerts_OnEmptyTimeline_ReturnsEmptyArrayNot500`. Includes a hand-rolled `TestProblemDetailsFactory` + stub `IStringLocalizer<ErrorMessages>` so the controller's `CreateProblemDetails(...)` calls never NRE.

### changed
- `Surveillance/Application/CommandServices/IAlertCommandService.cs` + `Internal/CommandServices/AlertCommandService.cs` — grew 4 new `Handle(...)` overloads for the SURV-003 state-machine transitions. The existing `Handle(CreateAlertCommand)` now publishes the cross-BC `AlertGeneratedIntegrationEvent` post-commit when `alert.Type == EThreatType.PHENOLOGICAL_RISK` (SURV-002). The `using` block uses an explicit `Unit = ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Unit;` alias to disambiguate from `Cortex.Mediator.Unit` (a pre-existing namespace collision in the same file).
- `Surveillance/Application/QueryServices/IAlertQueryService.cs` + `Internal/QueryServices/AlertQueryService.cs` — new `Handle(GetAlertsByUserIdQuery, ...)` overload routes the sort key: `recent` (CreatedAt desc, default), `severity` (severity desc, then createdAt desc), `type` (type asc, then createdAt desc). Empty timelines return `Enumerable.Empty<AlertSummaryResource>()` — never null, never 500. The implementation re-uses the existing `FindByPlotIdInOrderByCreatedAtDescAsync(...)` repository method and sorts client-side per the sort key, keeping the EF query count constant.
- `Surveillance/Interfaces/Rest/Controllers/AlertsController.cs` — replaced the empty-list placeholder for non-`recent` sorts with a single `GetAlertsByUserIdQuery` dispatch (SURV-003 sort fix). Constructor gained `IStringLocalizer<ErrorMessages>` and `ProblemDetailsFactory` dependencies (matches the `UsersController` CC-6 pattern). Added 4 new state-transition endpoints + the shared `MapTransitionFailureToResult(...)` / `BuildOkWithAlertAsync(...)` helpers.

### notes
- **PR size:exception** (925 lines vs 400 budget, precedent: PR-6a tag 1.9.0). 502 lines of production code + 423 lines of test code. The SURV-002 + SURV-003 deliverables cannot be sliced without breaking the test-with-impl work-unit pattern (the controller test contains the `TestProblemDetailsFactory` + stub localizer that both deliverables share).

## [1.10.0-rc] - 2026-06-29

### added
- `Surveillance/Domain/Model/Events/AlertUpdatedEvent.cs` — `record` carrying `long AlertId`, `long PlotId` (CC-1 primitive transport), and a `string Transition` label (`CONFIRMED` / `DISMISSED` / `ESCALATED` / `LINKED_REPORT`) so observers can discriminate the originating method without sniffing the resulting state.
- `Shared/Domain/Model/Events/IHasDomainEvents.cs` — contract for aggregates that raise domain events; the post-commit `SaveChangesInterceptor` dispatcher (CC-4) is deferred to SHARED-011 and is **not** wired in Phase 1. The interface exposes `IReadOnlyCollection<IEvent> DomainEvents` so the future dispatcher can route through the existing `Cortex.Mediator` bus without an additional layer of abstraction.
- `Surveillance/Domain/Model/ValueObjects/EAlertSeverityExtensions.cs` — `RaiseOne()` implements the severity ladder `LOW → MEDIUM → HIGH → CRITICAL` and caps at `CRITICAL` (no overflow).
- `Surveillance/Domain/Model/Aggregates/Alert.cs` — 4 new state-machine domain methods (SURV-001). Each returns `Result<Unit, Error>`, leaves state unchanged on failure, and raises an `AlertUpdatedEvent` on every successful transition:
  - `ConfirmFromInspection()` — from any non-terminal state (`ACTIVE` / `UNDER_REVIEW` / `RESOLVED` is *not* terminal for this method, but `DISMISSED` and `RESOLVED` are) to `UNDER_REVIEW`; raises severity by one level. Returns `ALERT_TERMINAL` on `DISMISSED` / `RESOLVED` source.
  - `Dismiss()` — from any non-`DISMISSED` state to `DISMISSED` (terminal). Returns `ALERT_TERMINAL` on already-`DISMISSED`.
  - `Escalate()` — raises severity by one level without changing status. Always succeeds.
  - `LinkReport(PestSightingReportId)` — attaches the report id to the new `LinkedReportId` property; no state change. Always succeeds. The `LinkedReportId` property is marked `[NotMapped]` in Phase 1; the EF column and FK migration are added in a future phase.
- `tests/ArcadiaDevs.Viora.Platform.Tests/Surveillance/Domain/Model/Aggregates/AlertTests.cs` — 11 xUnit tests pinning the state machine: `ACTIVE → UNDER_REVIEW` transition + severity raise, `ConfirmFromInspection` on `DISMISSED` returns `ALERT_TERMINAL` and leaves state unchanged, `Dismiss` from `UNDER_REVIEW` and from `ACTIVE`, `Escalate` severity raise and cap at `CRITICAL`, `LinkReport` attaches without state change, `MarkAsReviewed` preservation, `IHasDomainEvents` implementation, and `DomainEvents` empty on construction.

### changed
- `Surveillance/Domain/Model/Aggregates/Alert.cs` — the aggregate now implements `IHasDomainEvents` and exposes a private `List<IEvent> _domainEvents` field. The existing `MarkAsReviewed()` is **unchanged** and is still the path used by `PATCH /api/v1/alerts/{id}`; the controller surface is **not** modified in this PR (the 4 new HTTP endpoints + the cross-BC `AlertGeneratedIntegrationEvent` publication land in PR-8b).

## [1.9.1] - 2026-06-29

### changed
- `Agronomic/Domain/Model/Aggregates/IoTDevice.cs` — public setters replaced by private setters; the legacy `new IoTDevice(plotId, deviceName, status)` ctor replaced by a static `IoTDevice.Create(plotId, deviceName, clock)` factory returning `Result<IoTDevice, Error>`. The factory validates `plotId > 0` and non-blank `deviceName`, stamps `CreatedAt` from the constructor-injected `IClock` (SHARED-008), and emits the device in `IoTDeviceStatus.Pending`. New domain methods: `Activate()` (Pending → Active), `Deactivate()` (Active → Inactive), `UpdateInformation(name, status)` (validate-then-apply, returns `Result<Unit, Error>`), and a state-machine-agnostic `RecordReading()` no-op forward-compat hook for the future `IHasDomainEvents` dispatcher (CC-4). The lowercase `update` method is removed in favour of the Plot-pattern `UpdateInformation`.
- `Agronomic/Domain/Model/Aggregates/AgronomicStatistic.cs` — public ctor replaced by a static `AgronomicStatistic.Create(userId, plotId, measurementDate, ndviValue, chillPortions, chillHours, chillModelState)` factory returning `Result<AgronomicStatistic, Error>`. Validates `userId > 0`, `plotId > 0`, NDVI in `[-1, 1]`, `chillPortions >= 0`, `chillHours >= 0`. `null` `chillModelState` defaults to `ChillModelState.Empty()`. New `RecordReading(...)` domain method re-applies the same validation contract and updates the measurement in place, returning `Result<Unit, Error>` and leaving state unchanged on failure.
- `Agronomic/Domain/Model/ValueObjects/IoTDeviceStatus.cs` — additive enum value `IoTDeviceStatus.Pending` (no schema change; the column is `varchar(20)` storing the enum name as a string).
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Configuration/IoTDeviceConfiguration.cs` — `builder.UsePropertyAccessMode(PropertyAccessMode.Field)` so EF Core reads and writes the aggregate's backing fields directly. Explicit `DeviceName → device_name` column mapping locks the snake_case column name; no schema change vs. the v1.9.0 InitialCreate migration.
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Configuration/AgronomicStatisticConfiguration.cs` — same `UsePropertyAccessMode(PropertyAccessMode.Field)` call.
- `Agronomic/Application/Internal/CommandServices/IoTDeviceCommandService.cs` — constructor-injects `IClock` (per SHARED-008) and routes device creation through `IoTDevice.Create(...)`. Update path now calls `device.UpdateInformation(...)` (returns `Result<Unit, Error>`).
- `Agronomic/Application/Internal/CommandServices/AgronomicStatisticIngestionService.cs` — routes ingestion through `AgronomicStatistic.Create(...)`. A factory validation failure is treated as a per-plot skip (`return false`) so a single bad plot cannot poison the ingestion report; the report's `WithSkipped()` counter still reflects the miss.

### added
- `tests/ArcadiaDevs.Viora.Platform.Tests/Agronomic/Domain/Model/Aggregates/IoTDeviceTests.cs` — 10 xUnit tests pinning the `Create` validation contract (empty device name, non-positive plot id), the `Pending → Active` / `Active → Inactive` state machine, the `Activate` failure on non-`Pending` source state, the `Deactivate` failure on non-`Active` source state, and the factory `CreatedAt` stamping.
- `tests/ArcadiaDevs.Viora.Platform.Tests/Agronomic/Domain/Model/Aggregates/AgronomicStatisticTests.cs` — 9 xUnit tests pinning the `Create` validation contract (non-positive user/plot id, out-of-range NDVI, negative chill), the null-`chillModelState` default to `Empty()`, the `RecordReading` update path, and the no-mutation guarantee on validation failure.
- `scripts/verify-agro-002-roundtrip.ps1` — one-off round-trip verification: fresh `postgres:16` container, apply all migrations, assert `iot_devices` + `agronomic_statistics` table shape, round-trip a factory-shaped row INSERT → SELECT (Pending → Active UPDATE state machine), then tear down. Stands in for the Tier 3 test harness (out of scope).

## [1.9.0] - 2026-06-29

### added
- SHARED-001 part 1: 4 new EF Core migrations that persist the 4 Agronomic aggregates that previously had no PostgreSQL representation (`agronomic_statistics` and `monitoring_summaries` are new; `iot_devices` and `dynamic_nutrition_plans` were already in `InitialCreate` and the matching `AddIoTDevice` / `AddDynamicNutritionPlan` migrations are empty no-ops kept for alphabetical ordering). Migrations ship with the per-BC `Apply<BC>Configuration` extension methods in place (SHARED-014, shipped in 1.8.2).
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Configuration/AgronomicStatisticConfiguration.cs` — `IEntityTypeConfiguration<AgronomicStatistic>` maps the aggregate to `agronomic_statistics` (long id, user_id, plot_id, measurement_date, ndvi_value, chill_portions, chill_hours, and the flattened `ChillModelState` value object via `OwnsOne` → `chill_model_intermediate_product`, `chill_model_previous_hour_temperature_celsius`, `chill_model_prior_hour_temperature_celsius`). Two indexes: `(plot_id, measurement_date)` and `user_id`.
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Configuration/MonitoringSummaryConfiguration.cs` — `IEntityTypeConfiguration<MonitoringSummary>` maps the aggregate to `monitoring_summaries` (long id via `MonitoringSummaryId` value-converter, user_id via `UserId` value-converter, `general_health_status` enum-as-string, `average_ndvi` / `accumulated_chill_hours` / `yield_projection` via decimal value-converters, `last_synchronization_at` via DateTimeOffset value-converter, plus the flattened `WeatherSnapshot` (4 columns) and `MitigationRecommendation` (3 columns) record VOs via `ComplexProperty`). One index on `user_id`.
- `Agronomic/Application/Internal/Configuration/Extensions/ModelBuilderExtensions.cs` — `ApplyAgronomicConfiguration` now wires the 2 new configurations in alphabetical order.
- `Agronomic/Domain/Model/ValueObjects/MitigationRecommendation.cs` — additive parameterless constructor for EF Core materialization as a `ComplexProperty`. The validating constructor is unchanged.

### changed
- `Migrations/AppDbContextModelSnapshot.cs` regenerated to include `AgronomicStatistic` and `MonitoringSummary` plus their flattened value-object sub-fields. The pre-1.9.0 per-BC config sections are byte-equivalent (verified by PR-5's `NoOpAfterRefactor` migration round-trip).
- 2 Surveillance-owned record value-object sub-fields (the `PlotId` and `ReporterUserId` owned types on `Alert` / `PestSightingReport`) now correctly have their `id` columns in the snapshot (post-PR-5 SHARED-014 refactor).

## [1.8.2] - 2026-06-29

### changed
- `AppDbContext.OnModelCreating` no longer uses `builder.ApplyConfigurationsFromAssembly(typeof(PlotConfiguration).Assembly)`. It now calls three explicit per-BC extension methods — `builder.ApplyAgronomicConfiguration()`, `builder.ApplyIamConfiguration()`, `builder.ApplySurveillanceConfiguration()` — and then `UseSnakeCaseNamingConvention()`. Each bounded context now owns its own EF Core mapping; the `AppDbContext` only orchestrates the call order. This is the SHARED-014 standalone refactor and is the load-bearing pre-requisite for the new EF migrations shipping in the next release.

### added
- `Agronomic/Infrastructure/Persistence/EntityFrameworkCore/Configuration/Extensions/ModelBuilderExtensions.cs` — `ApplyAgronomicConfiguration` now wires the 4 Agronomic `IEntityTypeConfiguration<>` classes (`PlotConfiguration`, `IoTDeviceConfiguration`, `AgroMonitoringPlotIntegrationConfiguration`, `DynamicNutritionPlanConfiguration`). The previous file only wired 2 of them.
- `Iam/Infrastructure/Persistence/EntityFrameworkCore/Configuration/Extensions/ModelBuilderExtensions.cs` — `ApplyIamConfiguration` now delegates to `UserConfiguration` + `RoleConfiguration` instead of the previous inline `builder.Entity<User>()` calls (which were missing the `HasIndex` / `HasColumnName` / `Roles` relationship that the proper configurations already provide).
- `Surveillance/Infrastructure/Persistence/EntityFrameworkCore/Configuration/Extensions/ModelBuilderExtensions.cs` — new file. `ApplySurveillanceConfiguration` wires the 3 Surveillance `IEntityTypeConfiguration<>` classes (`AlertConfiguration`, `PestSightingReportConfiguration`, `SymptomDictionaryItemConfiguration`).

## [1.8.1] - 2026-06-29

### changed
- `IWeatherDataService` now resolves to `AgroMonitoringWeatherDataService` exclusively. The previous `WeatherDataServiceAdapter` (which returned hard-coded 22.5 °C / Sunny snapshots) is removed. AgroMonitoring is the sole weather provider in v1; there is no fabricated-data fallback.

### added
- `Agronomic/Infrastructure/ExternalServices/AgroMonitoringWeatherDataService.cs` — real implementation that delegates to the existing `AgroMonitoringApiClient` via the new `IAgroMonitoringWeatherClient` port. On client `Result.Failure` returns null (caller surfaces the unavailability); on client exception logs and rethrows (caller surfaces a 5xx). Never returns fabricated data.
- `Agronomic/Infrastructure/ExternalServices/Configuration/AgroMonitoringWeatherOptions.cs` + `AgroMonitoringWeatherOptionsValidator` — `IValidateOptions<>` bound from `Agronomic:Weather:AgroMonitoring:ApiKey`. Fails fast at startup in all environments when the key is missing, empty, or whitespace-only (CC-5).
- `Agronomic/Infrastructure/ExternalServices/IAgroMonitoringWeatherClient.cs` — weather-only port on top of `AgroMonitoringApiClient`, scoped to the methods the new service actually needs (lets the service be unit-tested with NSubstitute without an `HttpClient`).
- `Agronomic/Application/Internal/QueryServices/GetPlotWeatherForecastQueryService` now constructor-injects `IWeatherDataService` and delegates the snapshot to the real provider. Returns `AgronomicErrors.WeatherUnavailable` when the real provider can't be reached (no fabricated fallback).
- 9 new unit tests: 4 for `AgroMonitoringWeatherOptionsValidator` (missing / empty / whitespace / valid keys) + 5 for `AgroMonitoringWeatherDataService` (success mapping, failure returns null, exception propagates, history failure returns null, source metadata reports `AgroMonitoring`).
- README: new "Weather Provider (AgroMonitoring)" section documenting the required config key and the operational risk of the no-fallback design.

## [1.8.0] - 2026-06-29

### added
- `Shared.Domain.IClock` abstraction + `Shared.Infrastructure.SystemClock` implementation registered as a singleton in `Program.cs`. Every `DateTime.UtcNow` / `DateTimeOffset.UtcNow` in `*/Application/Internal/**` is now resolved through the ctor-injected `IClock` (domain layer untouched). 2 new unit tests pin the behaviour: `SystemClockTests` and `AgronomicStatisticIngestionServiceClockTests`.
- `GET /api/v1/users/me` endpoint on `UsersController` (`[Authorize]`-protected, class-level also). Returns the authenticated user as a `UserResource` (200) or 404 `ProblemDetails` if the user was deleted between token issuance and request time. 3 new unit tests cover the OK / 404 / `[Authorize]` paths.

### changed
- `Agronomic/Domain/Services/ClimateRiskEvaluator.cs` moved to `Agronomic/Domain/Model/Services/` to align with the convention used by `ChillAccumulationCalculator`, `ChillRequirementResolver`, and `PlotDeletionPolicy`. Namespace updated to `Agronomic.Domain.Model.Services`.
- `MonitoringSummaryQueryService` now constructor-injects `ClimateRiskEvaluator` (singleton, registered in `Program.cs`) instead of `new`ing it; also constructor-injects `IClock`.
- 11 `Agronomic/Application/Internal/**` services now constructor-inject `IClock` and replace `DateTimeOffset.UtcNow` / `DateTime.UtcNow` with `_clock.UtcNow`.
- `TokenService.GenerateToken` documents that secret-length / placeholder / empty checks are enforced at startup by `TokenSettingsValidator` (SHARED-003) — no re-check needed in the token service.

## [1.7.7] - 2026-06-29

### fixed
- all 9 unprotected controllers now have class-level `[Authorize]` attribute — only `AuthenticationController` sign-in/sign-up endpoints remain `[AllowAnonymous]`
- `DEV-ONLY-PLEASE-CHANGE-ME` placeholder removed from `appsettings.json` — secret is now empty by default and validated at startup

### added
- `TokenSettingsValidator` implementing `IValidateOptions<TokenSettings>` — fails fast at startup in all environments if JWT secret is missing, too short (<32 chars), or set to the placeholder value
- `DynamicNutritionRecommendedEventHandler` and `NutritionApplicationCertifiedEventHandler` log-and-exit stubs for the 2 orphaned Agronomic events (per design-decisions #28)
- 8 new unit tests: 4 for `TokenSettingsValidator`, 4 for the two event handlers
- JWT configuration instructions in README with environment variable and user-secrets examples

### changed
- `AgronomicStatisticIngestionService`, `AgronomicStatisticsIngestionReport`, and `IAgronomicStatisticIngestionService` moved from `Agronomic/Application/CommandServices/` to `Agronomic/Application/Internal/CommandServices/` — public folder now contains only interfaces
- Production-only JWT startup guard replaced with `AddOptionsWithValidateOnStart<TokenSettings>` that validates in all environments

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

[1.10.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.10.0-rc...release/1.10.0
[1.10.0-rc]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.9.1...release/1.10.0-rc
[1.7.7]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.7.6...release/1.7.7
[1.9.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.8.2...release/1.9.0
[1.9.1]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.9.0...release/1.9.1
[1.7.6]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.7.5...release/1.7.6
[1.7.5]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.7.4...release/1.7.5
[1.7.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.6.0...release/1.7.0
[1.6.0]: https://github.com/upc-pre-1asi0730-2610-10215-arcadiadevs/viora-platform/compare/release/1.4.0...release/1.6.0
