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
$efInstalled = (& dotnet tool list --global 2>$null) -match "dotnet-ef"
if (-not $efInstalled) {
    Write-Host "dotnet-ef not found. Installing latest preview..." -ForegroundColor Yellow
    & dotnet tool install --global dotnet-ef --prerelease
} else {
    Write-Host "dotnet-ef already installed." -ForegroundColor Cyan
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
# Locate psql.exe (auto-detect version and path)
# ---------------------------------------------------------------------------
$psqlPath = $null

# 1. Try the version specified by the user
$candidate = "C:\Program Files\PostgreSQL\$PostgresVersion\bin\psql.exe"
if (Test-Path $candidate) { $psqlPath = $candidate }

# 2. Search all versions under "Program Files\PostgreSQL"
if (-not $psqlPath) {
    $pgRoot = "C:\Program Files\PostgreSQL"
    if (Test-Path $pgRoot) {
        $found = Get-ChildItem -Path $pgRoot -Directory | Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "bin\psql.exe" } |
            Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($found) { $psqlPath = $found }
    }
}

# 3. Try Program Files (x86)
if (-not $psqlPath) {
    $pgRoot86 = "C:\Program Files (x86)\PostgreSQL"
    if (Test-Path $pgRoot86) {
        $found = Get-ChildItem -Path $pgRoot86 -Directory | Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "bin\psql.exe" } |
            Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($found) { $psqlPath = $found }
    }
}

# 4. Search PATH
if (-not $psqlPath) {
    $cmd = Get-Command psql -ErrorAction SilentlyContinue
    if ($cmd) { $psqlPath = $cmd.Source }
}

if (-not $psqlPath) {
    Write-Error @"
psql.exe not found. Tried:
  - C:\Program Files\PostgreSQL\$PostgresVersion\bin\psql.exe
  - All versions under C:\Program Files\PostgreSQL\*\bin\
  - All versions under C:\Program Files (x86)\PostgreSQL\*\bin\
  - System PATH

Please install PostgreSQL or pass the correct -PostgresVersion parameter,
or add psql.exe to your PATH.
"@
    exit 1
}

Write-Host "Using psql at: $psqlPath" -ForegroundColor Cyan

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
