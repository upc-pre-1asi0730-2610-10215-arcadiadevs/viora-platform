using ArcadiaDevs.Viora.Platform.Agronomic.Application.Acl;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Swagger;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Hosting;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.OutboundServices.Acl;
using Cortex.Mediator;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Interfaces.AspNetCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Cortex.Mediator.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using ArcadiaDevs.Viora.Platform.Iam.Application.Acl;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices.Configuration;
using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Application.Acl;
using ArcadiaDevs.Viora.Platform.Profile.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Profile.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using Microsoft.Extensions.Options;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure Lower Case URLs
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Localization Configuration
builder.Services.AddLocalization();

// Configure Kebab Case Route Naming Convention
builder.Services.AddControllers(options => options.Conventions.Add(new KebabCaseRouteNamingConvention()))
    .AddDataAnnotationsLocalization();

var corsAllowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
        ?? "http://localhost:5173,http://127.0.0.1:5173,http://localhost:4173,http://127.0.0.1:4173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("VioraWebApp", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.OperationFilter<PlotViewResponseOperationFilter>();

    // Wires the XML doc comments generated via <GenerateDocumentationFile> into
    // Swagger UI/OpenAPI JSON — without this, controller <summary>/<remarks>/
    // <param>/<response> tags are compiled but never reach the API docs.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// Add Database Connection
// Read via IConfiguration (not Environment.GetEnvironmentVariable) so the
// value can come from OS environment variables, appsettings, OR user secrets
// (dotnet user-secrets set DATABASE_URL ...) — CreateBuilder wires user
// secrets into IConfiguration automatically in Development.
var databaseUrl = builder.Configuration["DATABASE_URL"];
var useEnvironmentVariables = !string.IsNullOrWhiteSpace(databaseUrl);

// Configure Database Context and route EF logs through the app logger pipeline.
// SHARED-011: the EF Core SaveChangesInterceptors are now registered here
// (previously, AuditableEntityInterceptor was registered in
// AppDbContext.OnConfiguring). The composition root owns the interceptor
// registration so both interceptors can be DI-injected AND so the locked
// order — AuditableEntityInterceptor FIRST, PostCommitDomainEventDispatcher
// LAST — is enforced in exactly one place.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    // Interceptor order is locked (SHARED-011, design #45 §5.6):
    //   1) AuditableEntityInterceptor FIRST: stamps CreatedAt/UpdatedAt
    //      on every tracked entity BEFORE the post-commit dispatcher
    //      reads the entity into the event payload. If the audit
    //      interceptor ran AFTER the dispatcher, the event payload would
    //      carry a stale (pre-stamp) timestamp.
    //   2) PostCommitDomainEventDispatcher LAST: dispatches
    //      IHasDomainEvents.DomainEvents on the in-process bus after
    //      the DB write commits (CC-9 best-effort, CC-2 in-process bus).
    options.AddInterceptors(
        serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
        serviceProvider.GetRequiredService<PostCommitDomainEventDispatcher>());

    if (!useEnvironmentVariables)
    {
        options.UseInMemoryDatabase("VioraPlatform");
        return;
    }

    var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    var connectionString = connectionStringTemplate
        .Replace("%DATABASE_URL%", databaseUrl)
        .Replace("%DATABASE_PORT%", builder.Configuration["DATABASE_PORT"])
        .Replace("%DATABASE_NAME%", builder.Configuration["DATABASE_NAME"])
        .Replace("%DATABASE_SCHEMA%", builder.Configuration["DATABASE_SCHEMA"])
        .Replace("%DATABASE_USER%", builder.Configuration["DATABASE_USER"])
        .Replace("%DATABASE_PASSWORD%", builder.Configuration["DATABASE_PASSWORD"]);
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    options.UseNpgsql(connectionString)
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// Configure Dependency Injection

// Shared Bounded Context Injection Configuration
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Shared.Domain.IClock, ArcadiaDevs.Viora.Platform.Shared.Infrastructure.SystemClock>();
// SHARED-011: the two EF Core SaveChangesInterceptors are registered
// here (previously AuditableEntityInterceptor was constructed inline in
// AppDbContext.OnConfiguring and could not consume services from the
// host's DI container). The composition root owns the interceptor
// registration so both interceptors can be DI-injected AND so the
// locked order — AuditableEntityInterceptor FIRST,
// PostCommitDomainEventDispatcher LAST — is enforced in exactly one
// place.
//
// LIFETIMES:
//   * AuditableEntityInterceptor: singleton. The audit interceptor is
//     fully stateless (it stamps CreatedAt/UpdatedAt via the local
//     IClock, which is itself a singleton). Singleton matches the
//     "stateless shared infrastructure service" pattern of
//     ClimateRiskEvaluator / InMemoryActivationCodeCatalog.
//   * PostCommitDomainEventDispatcher: scoped. The dispatcher holds an
//     IMediator + ILogger reference and snapshots the ChangeTracker
//     on every SavedChanges call. The ctor is stateless, but the
//     ctor's IMediator dependency is registered scoped by
//     Cortex.Mediator's default; a singleton dispatcher would trigger
//     the "Cannot consume scoped service from singleton" validation
//     error when the host is built (which is why the F1a harness
//     worked around it by demoting the dispatcher to scoped in
//     ConfigureTestServices — see CHANGELOG 1.15.0 R3 note). With
//     this scoped registration, the workaround is no longer needed
//     and the production lifetime matches the dependency graph.
builder.Services.AddSingleton<AuditableEntityInterceptor>();
builder.Services.AddScoped<PostCommitDomainEventDispatcher>();

// External API Clients
builder.Services.AddHttpClient<AgroMonitoringApiClient>(client =>
{
    var baseUrl = builder.Configuration["ExternalApis:AgroMonitoring:BaseUrl"]
        ?? "https://api.agromonitoring.com";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
// AGRO-003: expose the weather-only port so the AgroMonitoringWeatherDataService
// can be unit-tested with a substitute without spinning up an HttpClient.
builder.Services.AddScoped<IAgroMonitoringWeatherClient>(sp => sp.GetRequiredService<AgroMonitoringApiClient>());

// AgroMonitoring weather options: validated at startup (CC-5). AGRO-003 — sole weather
// provider in v1, no fabricated-data fallback if the key is missing.
builder.Services.AddSingleton<IValidateOptions<AgroMonitoringWeatherOptions>, AgroMonitoringWeatherOptionsValidator>();
builder.Services.AddOptionsWithValidateOnStart<AgroMonitoringWeatherOptions>()
    .Bind(builder.Configuration.GetSection(AgroMonitoringWeatherOptions.SectionName));

// Agronomic Bounded Context Injection Configuration
builder.Services.AddSingleton(new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.ChillRequirementPolicy(50.0));
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ChillRequirementResolver>();
builder.Services.AddScoped<IPlotRepository, PlotRepository>();
builder.Services.AddScoped<IAgronomicContextFacade, AgronomicContextFacade>();
builder.Services.AddScoped<IAgroMonitoringPlotIntegrationRepository, AgroMonitoringPlotIntegrationRepository>();
builder.Services.AddScoped<IAgroMonitoringImageryService, AgroMonitoringImageryServiceAdapter>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices.IWeatherDataService, ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.AgroMonitoringWeatherDataService>();
builder.Services.AddScoped<IPlotCommandService, PlotCommandService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IPlotQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.PlotQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotDetailQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotDetailQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotMonitoringSummaryQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotMonitoringSummaryQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotWeatherForecastQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotWeatherForecastQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotNdviTileQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotNdviTileQueryService>();
builder.Services.AddScoped<IIoTDeviceRepository, IoTDeviceRepository>();
builder.Services.AddScoped<IMonitoringSummaryQueryService, MonitoringSummaryQueryService>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ClimateRiskEvaluator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.PhenologicalRiskEvaluator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ChillSeasonEvaluator>();
builder.Services.AddSingleton<IActivationCodeCatalog, InMemoryActivationCodeCatalog>();
// A1 (PR-C): yield forecast estimator (pure-function port of OS YieldForecastEstimator).
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.IYieldForecastEstimator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.YieldForecastEstimator>();
// A1 (PR-C) + A2 (PR-D1) — dynamic-nutrition policy bound from configuration.
// The same options class is consumed by the yield estimator (this PR) and the
// dynamic-nutrition plan generator (PR-D2). Single source of truth.
builder.Services.AddSingleton<IValidateOptions<DynamicNutritionPolicyOptions>, DynamicNutritionPolicyOptionsValidator>();
builder.Services.AddOptionsWithValidateOnStart<DynamicNutritionPolicyOptions>()
    .Bind(builder.Configuration.GetSection(DynamicNutritionPolicyOptions.SectionName));
// A2 part 1 (PR-D1) — the 3 per-risk evaluators consumed by the future
// RecommendDynamicNutritionPlanCommandService refactor (PR-D2). Stateless
// pure functions; singleton lifetime matches the existing
// ClimateRiskEvaluator / InMemoryActivationCodeCatalog pattern.
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.IChillDeficitEvaluator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ChillDeficitEvaluator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ILowNdviEvaluator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.LowNdviEvaluator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.IHydricStressEvaluator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.HydricStressEvaluator>();
// A2 part 2 (PR-D2) — the dynamic-nutrition plan generator (port of OS
// DynamicNutritionPlanGenerator.java) + the risk translator (per-risk booleans
// → IReadOnlyCollection<EThreatType>). Both stateless; singleton lifetime
// matches the existing 3 evaluators. The generator throws on empty risks
// (CC-7); the translator's output is fed into the generator as the
// triggering-risk set.
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.IDynamicNutritionPlanGenerator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.DynamicNutritionPlanGenerator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services.IAgronomicRiskTranslator, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services.AgronomicRiskTranslator>();
builder.Services.AddScoped<IAgronomicStatisticRepository, AgronomicStatisticRepository>();
builder.Services.AddScoped<IAgronomicStatisticsQueryService, AgronomicStatisticsQueryService>();
builder.Services.AddScoped<IAgronomicStatisticSeriesQueryService, AgronomicStatisticSeriesQueryService>();
builder.Services.AddScoped<IAgronomicStatisticIngestionService, AgronomicStatisticIngestionService>();
builder.Services.AddScoped<ChillAccumulationCalculator>();
builder.Services.AddScoped<IDynamicNutritionQueryService, DynamicNutritionQueryService>();
builder.Services.AddScoped<IRecommendDynamicNutritionPlanCommandService, RecommendDynamicNutritionPlanCommandService>();
builder.Services.AddScoped<ICertifyNutritionApplicationCommandService, CertifyNutritionApplicationCommandService>();
// 1.17.0 (R1 fix + IoT telemetry): the simulator + health evaluator are
// stateless pure functions (singleton); the query service is scoped to match
// the sibling MonitoringSummaryQueryService. The query service's interface
// was previously unbacked (no concrete class in DI), so the GET endpoint
// threw at resolve time — this registration fixes that gap.
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ISoilReadingSimulator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.SoilReadingSimulator>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ISensorHealthEvaluator, ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.SensorHealthEvaluator>();
builder.Services.AddScoped<IIoTDeviceQueryService, IoTDeviceQueryService>();
// 1.16.2: HydricStressDetectedIntegrationEvent producer — Scoped because it uses
// repositories (Scoped). Resolved via IServiceScopeFactory in the scheduler (D17).
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services.IHydricStressDetectedIntegrationEventProducer, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services.HydricStressDetectedIntegrationEventProducer>();
builder.Services.AddAgronomicStatisticsHosting(builder.Configuration);
// 1.18.0: Expense BC slice (THE LAST phase-3 release)
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<IExpenseCommandService, ExpenseCommandService>();
builder.Services.AddScoped<IExpenseQueryService, ExpenseQueryService>();
// Surveillance Bounded Context Injection Configuration
builder.Services.AddScoped<IPestSightingReportRepository, PestSightingReportRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<ISymptomDictionaryItemRepository, SymptomDictionaryItemRepository>();

builder.Services.AddScoped<IPestSightingCommandService, PestSightingCommandService>();
builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<ISymptomCommandService, SymptomCommandService>();
builder.Services.AddScoped<ISymptomQueryService, SymptomQueryService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<ICommunityRiskQueryService, CommunityRiskQueryService>();
builder.Services.AddScoped<IPestSightingReportQueryService, PestSightingReportQueryService>();

// Fully-qualified: WU3 of Intervention parity (obs #268) registers its own
// same-named IExternalAgronomicService/ExternalAgronomicService pair below
// in a different namespace, which makes the short names ambiguous once
// both `using` directives are in scope.
builder.Services.AddScoped<
    ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl.IExternalAgronomicService,
    ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl.ExternalAgronomicService>();
// WU2 of Intervention parity (surveillance-acl-facade, obs #268): outward-facing
// facade so other bounded contexts (Intervention) can read Alert data via the ACL.
builder.Services.AddScoped<ISurveillanceContextFacade, SurveillanceContextFacade>();
builder.Services.AddScoped<ThreatInferenceService>();
builder.Services.AddScoped<IDynamicNutritionPlanRepository, DynamicNutritionPlanRepository>();

// Health Checks
builder.Services.AddHealthChecks();

// Iam Bounded Context Injection Configuration
builder.Services.AddSingleton<IValidateOptions<TokenSettings>, TokenSettingsValidator>();
builder.Services.AddOptionsWithValidateOnStart<TokenSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IRoleQueryService, RoleQueryService>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();


// Intervention Bounded Context Injection Configuration
// WU1 of 8 (specialist-and-matching, obs #268): Specialist slice.
builder.Services.AddScoped<ISpecialistRepository, SpecialistRepository>();
builder.Services.AddScoped<ISpecialistCommandService, SpecialistCommandService>();
builder.Services.AddScoped<ISpecialistQueryService, SpecialistQueryService>();
builder.Services.AddScoped<SpecialistMatchingPolicy>();
// WU2 of 8 (surveillance-acl-facade, obs #268): Intervention-owned adapter
// consuming Surveillance's outward-facing ISurveillanceContextFacade.
builder.Services.AddScoped<IExternalSurveillanceService, ExternalSurveillanceService>();
// WU3 of 8 (intervention-request, obs #268): InterventionRequest slice +
// Intervention-owned adapter consuming Agronomic's IAgronomicContextFacade.
// Fully-qualified here: Surveillance already registers its own same-named
// IExternalAgronomicService/ExternalAgronomicService pair (line above,
// design's Cross-BC ACL Wiring table) in a different namespace — the
// `using` for that namespace makes the short names ambiguous.
builder.Services.AddScoped<IInterventionRequestRepository, InterventionRequestRepository>();
builder.Services.AddScoped<
    ArcadiaDevs.Viora.Platform.Intervention.Application.OutboundServices.Acl.IExternalAgronomicService,
    ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.OutboundServices.Acl.ExternalAgronomicService>();
builder.Services.AddScoped<IInterventionRequestCommandService, InterventionRequestCommandService>();
builder.Services.AddScoped<IInterventionRequestQueryService, InterventionRequestQueryService>();
// specialist-dashboard-parity: specialist verify/decline + GET /specialist-dashboard.
// WU4 of 8 (service-proposal, obs #268): ServiceProposal slice.
builder.Services.AddScoped<IServiceProposalRepository, ServiceProposalRepository>();
builder.Services.AddScoped<IServiceProposalCommandService, ServiceProposalCommandService>();
builder.Services.AddScoped<IServiceProposalQueryService, ServiceProposalQueryService>();
// WU5 of 8 (treatment-prescription, obs #268): TreatmentPrescription slice.
builder.Services.AddScoped<ITreatmentPrescriptionRepository, TreatmentPrescriptionRepository>();
builder.Services.AddScoped<ITreatmentPrescriptionCommandService, TreatmentPrescriptionCommandService>();
builder.Services.AddScoped<ITreatmentPrescriptionQueryService, TreatmentPrescriptionQueryService>();
// WU6 of 8 (intervention-execution, obs #268): InterventionExecution slice.
builder.Services.AddScoped<IInterventionExecutionRepository, InterventionExecutionRepository>();
builder.Services.AddScoped<IInterventionExecutionCommandService, InterventionExecutionCommandService>();
builder.Services.AddScoped<IInterventionExecutionQueryService, InterventionExecutionQueryService>();
// WU7 of 8 (intervention-outcome, obs #268): InterventionOutcome slice.
builder.Services.AddScoped<IInterventionOutcomeRepository, InterventionOutcomeRepository>();
builder.Services.AddScoped<IInterventionOutcomeCommandService, InterventionOutcomeCommandService>();
builder.Services.AddScoped<IInterventionOutcomeQueryService, InterventionOutcomeQueryService>();
// WU8 of 8 (overview-and-metrics, obs #268): pure read-model slice, no new
// aggregate/migration. InterventionOverviewComposer is a plain domain
// service (no interface), mirroring SpecialistMatchingPolicy's registration.
builder.Services.AddScoped<InterventionOverviewComposer>();
builder.Services.AddScoped<IInterventionOverviewQueryService, InterventionOverviewQueryService>();
builder.Services.AddScoped<IInterventionRequestMetricsQueryService, InterventionRequestMetricsQueryService>();

// Billing Bounded Context Injection Configuration
// WU1 of 9 (plan, obs #319): Plan catalog slice. WU2-WU9 extend this block.
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IPlanCommandService, PlanCommandService>();
builder.Services.AddScoped<IPlanQueryService, PlanQueryService>();
// WU2 of 9 (subscription, obs #319): Subscription slice. FK-validates
// userId via IIamContextFacade (already registered by the Iam DI block)
// and planCode via IPlanRepository (registered above).
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
// WU3 of 9 (payment-method, obs #319): PaymentMethod slice. No
// IIamContextFacade dependency — userId is internally derived (REQ-CC-2
// exemption clause), no public write endpoint (upsert is invoked from
// WU6's webhook reconciliation).
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentMethodCommandService, PaymentMethodCommandService>();
builder.Services.AddScoped<IPaymentMethodQueryService, PaymentMethodQueryService>();
// WU4 of 9 (invoice, obs #319): Invoice slice. No IIamContextFacade
// dependency on the command side — userId is internally derived from an
// already-validated Subscription/checkout flow (REQ-CC-2 exemption
// clause); the read side (list-by-userId) DOES validate via
// IIamContextFacade since userId is direct client input there (REQ-INV-3).
// Creation is internal-only (invoked from WU6's webhook reconciliation).
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceCommandService, InvoiceCommandService>();
builder.Services.AddScoped<IInvoiceQueryService, InvoiceQueryService>();
// WU5 of 9 (payment-gateway-port, obs #319): IPaymentGateway port + the
// MercadoPagoPaymentGatewayAdapter, composing the raw-HttpClient-registration
// shape of AgroMonitoringApiClient with the Options+Validator shape of
// AgroMonitoringWeatherDataService/AgroMonitoringWeatherOptionsValidator
// (design's PaymentGateway Port Design section). Off by default
// (MercadoPagoOptions.Enabled=false) — builds/runs with zero real
// credentials; POST /checkouts returns 503 until configured.
builder.Services.AddHttpClient<MercadoPagoPaymentGatewayAdapter>(client =>
{
    // Reads the same config path MercadoPagoOptions.SectionName binds from —
    // read here directly (not via IOptions<T>) because AddHttpClient's
    // client-config lambda has no IServiceProvider, exactly mirroring
    // AgroMonitoringApiClient's own AddHttpClient registration constraint.
    var baseUrl = builder.Configuration[$"{MercadoPagoOptions.SectionName}:BaseUrl"]
        ?? "https://api.mercadopago.com";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<MercadoPagoPaymentGatewayAdapter>());
builder.Services.AddSingleton<IValidateOptions<MercadoPagoOptions>, MercadoPagoOptionsValidator>();
builder.Services.AddOptionsWithValidateOnStart<MercadoPagoOptions>()
    .Bind(builder.Configuration.GetSection(MercadoPagoOptions.SectionName));
builder.Services.AddScoped<ICheckoutCommandService, CheckoutCommandService>();
// WU6 of 9 (checkout-and-webhook, obs #319): webhook reconciliation.
// MercadoPagoWebhookController (POST /webhooks/mercado-pago, [AllowAnonymous])
// is the sole caller. No new EF migration this slice (no new persisted
// entity — reconciliation only reads/writes Invoice/Subscription/
// PaymentMethod, all already registered above).
builder.Services.AddScoped<IWebhookReconciliationCommandService, WebhookReconciliationCommandService>();
// WU7 of 9 (coupon, obs #319): Coupon slice. FK-validates userId via
// IIamContextFacade (REQ-CC-2); code is looked up in the static in-code
// CouponCatalog (REQ-COUP-1, not DB-backed); redemption idempotency is a
// per-user command-service-level guard (REQ-COUP-2), not an aggregate
// self-guard.
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddScoped<ICouponCommandService, CouponCommandService>();
builder.Services.AddScoped<ICouponQueryService, CouponQueryService>();

// Profile Bounded Context Injection Configuration
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IProfileQueryService, ProfileQueryService>();
builder.Services.AddScoped<IProfileCommandService, ProfileCommandService>();
builder.Services.AddScoped<IProfileContextFacade, ProfileContextFacade>();

// Cortex Mediator
builder.Services.AddCortexMediator([typeof(Program)]);

var app = builder.Build();

// Apply pending EF Core migrations and seed data on startup.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    // Migrations only apply to relational providers; InMemory provider is used for dev/tests.
    if (!context.Database.IsInMemory())
        await context.Database.MigrateAsync();

    // Seeding Surveillance
    var symptomCommandService = services.GetRequiredService<ISymptomCommandService>();
    await symptomCommandService.Handle(new SeedSymptomsCommand());

    // Seeding Iam roles (idempotent — safe to run on every startup)
    await IamDataSeeder.SeedAsync(context);

    // Seeding Intervention demo specialists (WU1 bootstrap — design decision 1,
    // obs #267; each backed by a real Profile row with Role=Specialist).
    // Wrapped in try/catch: under a rolling deploy with multiple replicas,
    // concurrent cold starts can race on the specialists.profile_user_id
    // unique index. A seed failure must not prevent the instance from
    // accepting traffic — log and continue startup instead of crashing.
    // NOTE: the pre-existing SeedSymptomsCommand/IamDataSeeder calls above
    // are not guarded the same way; that gap is out of scope for this fix
    // pass (not touching unrelated seeders to avoid scope creep).
    try
    {
        var specialistCommandService = services.GetRequiredService<ISpecialistCommandService>();
        await specialistCommandService.Handle(new SeedSpecialistsCommand());
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to seed demo specialists during startup; continuing startup anyway.");
    }

    // Seeding the Billing Plan catalog (WU1 bootstrap — REQ-PLAN-1,
    // idempotent by Code). Wrapped in the same try/catch shape as the
    // Specialist seed above: under a rolling deploy with multiple replicas,
    // concurrent cold starts can race on the plans.code unique index, and a
    // seed failure must not prevent the instance from accepting traffic.
    try
    {
        var planCommandService = services.GetRequiredService<IPlanCommandService>();
        await planCommandService.Handle(new SeedPlanCatalogCommand());
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to seed the Plan catalog during startup; continuing startup anyway.");
    }
}

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

// Swagger UI is enabled only in Development and Staging.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Localization Configuration
string[] supportedCultures = ["en", "en-US", "es", "es-PE"];
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.ApplyCurrentCultureToResponseHeaders = true;
app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();

app.UseCors("VioraWebApp");

app.UseAuthorization();

app.UseRequestAuthorization();

app.MapHealthChecks("/healthz").AllowAnonymous();

app.MapControllers();

app.Run();

// Marker that exposes the top-level Program entry point to
// WebApplicationFactory<Program> in the test project. Without this
// partial class declaration, the test project cannot reference Program
// (it is implicit and internal because Program.cs uses top-level
// statements). This is a single-line, no-op test helper. See
// tests/ArcadiaDevs.Viora.Platform.Tests/README.md for the harness usage.
public partial class Program { }
