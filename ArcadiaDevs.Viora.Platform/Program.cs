using ArcadiaDevs.Viora.Platform.Agronomic.Application.Acl;
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
using Cortex.Mediator;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Interfaces.AspNetCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Cortex.Mediator.DependencyInjection;
using Microsoft.EntityFrameworkCore;
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
using Microsoft.Extensions.Options;

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
builder.Services.AddSwaggerGen(options => options.EnableAnnotations());

// Add Database Connection
var useEnvironmentVariables =
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL"));

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

    var connectionString = Environment.ExpandEnvironmentVariables(connectionStringTemplate);
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
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetMyPlotsOverviewQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetMyPlotsOverviewQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotDetailQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotDetailQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotMonitoringSummaryQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotMonitoringSummaryQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotWeatherForecastQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotWeatherForecastQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotsByUserIdQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotsByUserIdQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotByIdQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotByIdQueryService>();
builder.Services.AddScoped<ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices.IGetPlotNdviTileQueryService, ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices.GetPlotNdviTileQueryService>();
builder.Services.AddScoped<IIoTDeviceRepository, IoTDeviceRepository>();
builder.Services.AddScoped<IMonitoringSummaryQueryService, MonitoringSummaryQueryService>();
builder.Services.AddSingleton<ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services.ClimateRiskEvaluator>();
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

builder.Services.AddScoped<IExternalAgronomicService, ExternalAgronomicService>();
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

app.MapHealthChecks("/healthz");

app.MapControllers();

app.Run();

// Marker that exposes the top-level Program entry point to
// WebApplicationFactory<Program> in the test project. Without this
// partial class declaration, the test project cannot reference Program
// (it is implicit and internal because Program.cs uses top-level
// statements). This is a single-line, no-op test helper. See
// tests/ArcadiaDevs.Viora.Platform.Tests/README.md for the harness usage.
public partial class Program { }
