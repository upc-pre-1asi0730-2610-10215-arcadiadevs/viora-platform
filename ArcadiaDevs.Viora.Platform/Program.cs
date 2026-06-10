using ArcadiaDevs.Viora.Platform.Agronomic.Application.Commands.CreatePlot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

using Cortex.Mediator;
using Cortex.Mediator.Commands;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IPlotRepository, PlotRepository>();

// Cortex Mediator
builder.Services.AddCortexMediator(cfg =>
{
    cfg.AddHandler<CreatePlotCommandHandler>();
    cfg.AddOpenPipelineBehavior(typeof(LoggingCommandBehavior<,>));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

app.Run();