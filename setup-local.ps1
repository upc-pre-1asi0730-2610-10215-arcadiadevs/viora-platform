#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup local development environment for wa-viora-platform.

.DESCRIPTION
    - Ensures PostgreSQL service is running (Windows).
    - Creates the viora_platform database if it does not exist.
    - Sets required environment variables.
    - Applies EF Core migrations.
    - Prints instructions to run the API.

.PARAMETER PostgresPassword
    Password for the postgres user. Default: postgres

.PARAMETER PostgresVersion
    PostgreSQL major version installed on Windows. Default: 17
    Used to locate psql.exe at C:\Program Files\PostgreSQL\<version>\bin\psql.exe.

.PARAMETER DatabaseName
    Name of the database to create. Default: viora_platform

.PARAMETER DotnetRoot
    Path to the .NET installation on Windows. Default: C:\Users\jahat\.dotnet

.EXAMPLE
    .\setup-local.ps1 -PostgresPassword "myPassword"

.EXAMPLE
    .\setup-local.ps1 -PostgresPassword "myPassword" -PostgresVersion 16
#>
param(
    [string]$PostgresPassword = "postgres",
    [string]$PostgresVersion = "17",
    [string]$DatabaseName = "viora_platform",
    [string]$DotnetRoot = "C:\Users\jahat\.dotnet"
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Resolve paths
# ---------------------------------------------------------------------------
$repoRoot = $PSScriptRoot
$projectDir = Join-Path $repoRoot "ArcadiaDevs.Viora.Platform"
$projectFile = Join-Path $projectDir "ArcadiaDevs.Viora.Platform.csproj"

if (-not (Test-Path $projectFile)) {
    Write-Error "Project file not found: $projectFile"
    exit 1
}

# ---------------------------------------------------------------------------
# Configure dotnet environment (non-standard user-local install)
# ---------------------------------------------------------------------------
$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$DotnetRoot\tools;$env:PATH"

Write-Host "Using dotnet from: $DotnetRoot" -ForegroundColor Cyan
& dotnet --version

# ---------------------------------------------------------------------------
# Verify / install dotnet-ef global tool
# ---------------------------------------------------------------------------
$efVersion = $null
try {
    $efVersion = & dotnet ef --version 2>$null
} catch {}

if (-not $efVersion) {
    Write-Host "dotnet-ef not found or incompatible. Installing latest preview..." -ForegroundColor Yellow
    & dotnet tool install --global dotnet-ef --prerelease
} else {
    Write-Host "dotnet-ef version: $efVersion" -ForegroundColor Cyan
}

# ---------------------------------------------------------------------------
# Ensure PostgreSQL service is running
# ---------------------------------------------------------------------------
$pgService = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $pgService) {
    Write-Error "PostgreSQL service not found. Make sure PostgreSQL is installed on Windows."
    exit 1
}

Write-Host "PostgreSQL service status: $($pgService.Status)" -ForegroundColor Cyan

if ($pgService.Status -ne 'Running') {
    Write-Host "Starting PostgreSQL service..." -ForegroundColor Yellow
    Start-Service $pgService.Name
    Start-Sleep -Seconds 2
}

# ---------------------------------------------------------------------------
# Create database if it does not exist
# ---------------------------------------------------------------------------
$psqlPath = "C:\Program Files\PostgreSQL\$PostgresVersion\bin\psql.exe"
if (-not (Test-Path $psqlPath)) {
    Write-Error "psql.exe not found at $psqlPath. Verify PostgresVersion parameter."
    exit 1
}

$env:PGPASSWORD = $PostgresPassword

Write-Host "Checking if database '$DatabaseName' exists..." -ForegroundColor Cyan
$dbExists = & $psqlPath -U postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName'" 2>&1

if ($dbExists -eq "1") {
    Write-Host "Database '$DatabaseName' already exists." -ForegroundColor Green
} else {
    Write-Host "Creating database '$DatabaseName'..." -ForegroundColor Yellow
    & $psqlPath -U postgres -c "CREATE DATABASE $DatabaseName;" 2>&1 | ForEach-Object { Write-Host $_ }
    Write-Host "Database created." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Set application environment variables
# ---------------------------------------------------------------------------
$env:DATABASE_URL = "localhost"
$env:DATABASE_PORT = "5432"
$env:DATABASE_SCHEMA = "public"
$env:DATABASE_USER = "postgres"
$env:DATABASE_PASSWORD = $PostgresPassword
$env:AGROMONITORING_API_KEY = "dummy"

Write-Host "Environment variables set for database connection." -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# Apply EF Core migrations
# ---------------------------------------------------------------------------
Write-Host "Applying EF Core migrations..." -ForegroundColor Cyan
Push-Location $projectDir
try {
    & dotnet ef database update --project $projectFile --startup-project $projectFile
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef database update failed" }
} finally {
    Pop-Location
}

Write-Host "" 
Write-Host "==============================================" -ForegroundColor Green
Write-Host "LOCAL SETUP COMPLETE" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host ""
Write-Host "To run the API, execute from the repo root:"
Write-Host ""
Write-Host "  cd ArcadiaDevs.Viora.Platform"
Write-Host "  `$env:DATABASE_URL='localhost'; `$env:DATABASE_PORT='5432'; `$env:DATABASE_SCHEMA='public'; `$env:DATABASE_USER='postgres'; `$env:DATABASE_PASSWORD='$PostgresPassword'; `$env:AGROMONITORING_API_KEY='dummy'; dotnet run"
Write-Host ""
Write-Host "Then open Swagger at: http://localhost:8080/swagger"
Write-Host ""
