using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Design-time factory for <see cref="AppDbContext" />.
///     Used by <c>dotnet ef migrations add</c>/<c>dotnet ef database update</c>
///     at design time. Reads the same DATABASE_URL/PORT/SCHEMA/USER/PASSWORD
///     values as the runtime composition root (Program.cs) — from user
///     secrets or environment variables — and expands them into the
///     connection string template from appsettings.json. Falls back to a
///     hardcoded local default if none of those are set.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionStringTemplate = config.GetConnectionString("DefaultConnection")
                               ?? "Host=%DATABASE_URL%;Port=%DATABASE_PORT%;Database=%DATABASE_NAME%;Username=%DATABASE_USER%;Password=%DATABASE_PASSWORD%;Search Path=%DATABASE_SCHEMA%";

        var connectionString = connectionStringTemplate
            .Replace("%DATABASE_URL%", config["DATABASE_URL"] ?? "localhost")
            .Replace("%DATABASE_PORT%", config["DATABASE_PORT"] ?? "5432")
            .Replace("%DATABASE_NAME%", config["DATABASE_NAME"] ?? "viora_platform")
            .Replace("%DATABASE_SCHEMA%", config["DATABASE_SCHEMA"] ?? "public")
            .Replace("%DATABASE_USER%", config["DATABASE_USER"] ?? "postgres")
            .Replace("%DATABASE_PASSWORD%", config["DATABASE_PASSWORD"] ?? "postgres");

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql(connectionString);

        return new AppDbContext(builder.Options);
    }
}
