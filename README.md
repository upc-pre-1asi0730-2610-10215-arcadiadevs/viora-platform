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

To use PostgreSQL, set `Database:Provider` to `PostgreSql` and configure
`ConnectionStrings:DefaultConnection` in the appropriate appsettings file or
through environment variables.

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

## License

This project is licensed under the MIT License. See
[`LICENSE.md`](LICENSE.md).
