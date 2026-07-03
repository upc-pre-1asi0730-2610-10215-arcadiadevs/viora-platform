# Viora Platform

Backend API for the Viora agronomic platform. The project follows a
Domain-Driven Design structure organized by bounded context and architectural
layer.

## Requirements

- .NET 10 SDK
- Docker, optionally
- PostgreSQL, optionally

The application uses an Entity Framework Core in-memory database by default, so
the current Create Plot flow can run without an external datasource.

## Project Structure

```text
.
|-- ArcadiaDevs.Viora.Platform/
|   |-- Agronomic/
|   |-- Shared/
|   |-- Program.cs
|   `-- ArcadiaDevs.Viora.Platform.csproj
|-- docs/
|-- Dockerfile
|-- global.json
`-- viora-platform.sln
```

## Run Locally

From the repository root:

```powershell
dotnet restore viora-platform.sln
dotnet run --project ArcadiaDevs.Viora.Platform
```

Swagger UI is available at:

```text
http://localhost:5269/swagger
```

The implemented endpoint is:

```text
POST /api/v1/plots
```

An example request is available in
`ArcadiaDevs.Viora.Platform/ArcadiaDevs.Viora.Platform.http`.

## PostgreSQL

The app falls back to the EF Core in-memory provider unless `DATABASE_URL` is
set (`Program.cs`). This is read through `IConfiguration`, so it can come from
an OS environment variable OR from `dotnet user-secrets` — no in-repo `.env`
file needed. User secrets is the recommended path for local dev (same
mechanism already used for `Jwt:Secret`):

```powershell
cd ArcadiaDevs.Viora.Platform
dotnet user-secrets set "DATABASE_URL" "localhost"
dotnet user-secrets set "DATABASE_PORT" "5432"
dotnet user-secrets set "DATABASE_NAME" "viora_platform"
dotnet user-secrets set "DATABASE_SCHEMA" "public"
dotnet user-secrets set "DATABASE_USER" "postgres"
dotnet user-secrets set "DATABASE_PASSWORD" "postgres"
```

Equivalently, via real environment variables:

```powershell
$env:DATABASE_URL = "localhost"
$env:DATABASE_PORT = "5432"
$env:DATABASE_NAME = "viora_platform"
$env:DATABASE_SCHEMA = "public"
$env:DATABASE_USER = "postgres"
$env:DATABASE_PASSWORD = "postgres"
```

Use `setup-local.ps1` to provision a locally installed PostgreSQL instance and
create the `viora_platform` database:

```powershell
.\setup-local.ps1 -PostgresPassword "postgres"
```

Then apply migrations:

```powershell
dotnet ef database update --project ArcadiaDevs.Viora.Platform
```

In production the API is hosted on Render, with the database on Filess.

## Weather Provider (AgroMonitoring)

The platform uses [AgroMonitoring](https://agromonitoring.com/) as the **sole**
external weather provider. There is no fabricated-data fallback: if AgroMonitoring
is down, returns an error, or the API key is missing/empty, the platform will
**not** invent a forecast. Endpoints that depend on live weather will surface a
5xx and the original error will be logged.

**Required configuration** (any of the three below):

- `appsettings.json` → `Agronomic:Weather:AgroMonitoring:ApiKey`
- Linux/macOS environment variable: `Agronomic__Weather__AgroMonitoring__ApiKey`
- Windows PowerShell: `$env:Agronomic__Weather__AgroMonitoring__ApiKey = "..."`
- Windows user secrets:
  `dotnet user-secrets set "Agronomic:Weather:AgroMonitoring:ApiKey" "..."`

The application fails to start if the key is missing or empty (this is the only
way to guarantee the production path never falls back to hard-coded
`22.5 °C / Sunny` constants).

> **Operational risk (AGRO-003):** the platform has no alternative weather
> source in v1. If AgroMonitoring's quota is exhausted or the service is
> unavailable, weather-dependent features will return errors. A cache or
> fallback provider is a future enhancement.

## JWT Configuration

The application requires a JWT secret to start. The secret must be at least 32
characters long and must not be the placeholder value.

**Linux / macOS (environment variable):**

```bash
export Jwt__Secret="your-64-byte-random-secret-here"
```

**Windows (PowerShell):**

```powershell
$env:Jwt__Secret = "your-64-byte-random-secret-here"
```

**Windows (user secrets):**

```powershell
cd ArcadiaDevs.Viora.Platform
dotnet user-secrets set "Jwt:Secret" "your-64-byte-random-secret-here"
```

The application will fail to start if the secret is missing, too short, or set
to the placeholder value.

## Docker

Build and run the API from the repository root:

```powershell
docker build -t viora-platform .
docker run --rm -p 8080:8080 viora-platform
```

Swagger will then be available at `http://localhost:8080/swagger`.

## Documentation

Project documentation belongs in the [`docs`](docs/) directory.

- [Domain events architecture](docs/architecture/events.md) — in-process
  `Cortex.Mediator` bus, the `IEvent` / `IEventHandler<TEvent>` contract, the
  post-commit `PostCommitDomainEventDispatcher` (SHARED-011), and the
  best-effort failure-handling semantics (CC-9).

## License

This project is licensed under the MIT License. See
[`LICENSE.md`](LICENSE.md).
