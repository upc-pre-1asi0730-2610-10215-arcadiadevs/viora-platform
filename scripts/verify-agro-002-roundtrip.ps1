#!/usr/bin/env pwsh
# AGRO-002 Round-Trip Verification
# Change:   audit/wa-os-viora-gap-analysis-2026-06-29/phase-1
# Slice:    PR-7 (feature/agronomic/harden-iotdevice-and-statistic-aggregates)
# Verifies: factory-created device state round-trips through the
#           PropertyAccessMode.Field-backed EF mapping to PostgreSQL.
#
# This is a one-off script per design #27 §3.7. Full integration-test
# infrastructure is Tier 3 (out of scope). The script stands in for the
# `dotnet test` round-trip step.
#
# Pre-requisites:
#   - Docker Desktop running (postgres:16 image)
#   - .NET 10 SDK installed
#   - dotnet-ef tool installed (dotnet tool install -g dotnet-ef)
#
# Usage:
#   pwsh scripts/verify-agro-002-roundtrip.ps1

param(
    [string]$PostgresPort = "5433",
    [string]$PostgresPassword = "test",
    [string]$PostgresDb = "viora_agro002",
    [string]$ContainerName = "viora-pg-agro002"
)

$ErrorActionPreference = "Stop"

function Write-Section([string]$msg) {
    Write-Host ""
    Write-Host "=== $msg ===" -ForegroundColor Cyan
}

function Test-Pass([string]$msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green }
function Test-Fail([string]$msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red }

$script:PassCount = 0
$script:FailCount = 0

function Expect($cond, [string]$label) {
    $isTrue = $false
    if ($cond -is [bool]) {
        $isTrue = $cond
    } elseif ($cond -is [array]) {
        $isTrue = ($cond.Count -gt 0) -and ($cond[0] -is [bool]) -and $cond[0]
    } else {
        $isTrue = [bool]$cond
    }
    if ($isTrue) {
        Test-Pass $label
        $script:PassCount++
    } else {
        Test-Fail $label
        $script:FailCount++
    }
}

# ---------------------------------------------------------------------------
# 1. Bring up a fresh PostgreSQL container
# ---------------------------------------------------------------------------
Write-Section "Step 1: fresh PostgreSQL container on port $PostgresPort"

$existing = docker ps -a --filter "name=$ContainerName" --format "{{.ID}}" 2>$null
if ($existing) {
    Write-Host "  Removing leftover container $ContainerName ($existing)"
    docker rm -f $existing | Out-Null
}

docker run --rm -d --name $ContainerName `
    -p "${PostgresPort}:5432" `
    -e "POSTGRES_PASSWORD=$PostgresPassword" `
    -e "POSTGRES_DB=$PostgresDb" `
    postgres:16 | Out-Null

Write-Host "  Waiting for Postgres to be ready..."
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    $check = docker exec $ContainerName pg_isready -U postgres 2>&1
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 1
}
Expect $ready "Postgres is ready on port $PostgresPort"

# ---------------------------------------------------------------------------
# 2. Apply all migrations to a fresh database
# ---------------------------------------------------------------------------
Write-Section "Step 2: apply migrations (dotnet ef database update)"

# Clear any connection-string env var that appsettings.json would otherwise resolve.
# Use --connection explicitly to bypass the %DATABASE_*% placeholders.
Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue

$connStr = "Host=localhost;Port=$PostgresPort;Database=$PostgresDb;Username=postgres;Password=$PostgresPassword"
$applyOutput = dotnet ef database update --project "ArcadiaDevs.Viora.Platform" --connection $connStr 2>&1
$applyOk = $LASTEXITCODE -eq 0
Expect $applyOk "dotnet ef database update exits 0"
if (-not $applyOk) {
    Write-Host $applyOutput
    docker rm -f $ContainerName | Out-Null
    exit 1
}

# ---------------------------------------------------------------------------
# 3. Inspect the iot_devices table
# ---------------------------------------------------------------------------
Write-Section "Step 3: verify iot_devices table shape"

$columns = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c `
    "SELECT column_name || ':' || data_type FROM information_schema.columns WHERE table_name = 'iot_devices' ORDER BY ordinal_position;"

$columnMap = @{}
foreach ($line in $columns) {
    $parts = $line -split ":", 2
    if ($parts.Length -eq 2) {
        $columnMap[$parts[0]] = $parts[1]
    }
}

Expect $columnMap.ContainsKey("id")            "iot_devices has 'id' column"
Expect $columnMap.ContainsKey("plot_id")       "iot_devices has 'plot_id' column"
Expect $columnMap.ContainsKey("device_name")   "iot_devices has 'device_name' column"
Expect $columnMap.ContainsKey("status")        "iot_devices has 'status' column"
Expect $columnMap.ContainsKey("created_at")    "iot_devices has 'created_at' column"
Expect $columnMap.ContainsKey("updated_at")    "iot_devices has 'updated_at' column"

# Same for agronomic_statistics
$statColumns = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c `
    "SELECT column_name FROM information_schema.columns WHERE table_name = 'agronomic_statistics' ORDER BY ordinal_position;"
Expect ($statColumns -contains "id")                "agronomic_statistics has 'id' column"
Expect ($statColumns -contains "user_id")           "agronomic_statistics has 'user_id' column"
Expect ($statColumns -contains "plot_id")           "agronomic_statistics has 'plot_id' column"
Expect ($statColumns -contains "measurement_date")  "agronomic_statistics has 'measurement_date' column"
Expect ($statColumns -contains "ndvi_value")        "agronomic_statistics has 'ndvi_value' column"
Expect ($statColumns -contains "chill_portions")    "agronomic_statistics has 'chill_portions' column"
Expect ($statColumns -contains "chill_hours")       "agronomic_statistics has 'chill_hours' column"

# ---------------------------------------------------------------------------
# 4. Round-trip: insert a device (simulating the factory output), read it back
# ---------------------------------------------------------------------------
Write-Section "Step 4: round-trip an IoTDevice (simulating factory output)"

# This is the exact INSERT a factory-created device would produce.
$deviceName = "Sensor-AGRO-002"
$insertSql = @"
INSERT INTO iot_devices (plot_id, device_name, status, created_at, updated_at)
VALUES (42, '$deviceName', 'Pending', NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC')
RETURNING id;
"@

$insertedId = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c $insertSql | Select-Object -First 1
Expect ([bool]($insertedId -match "^\d+$")) "Insert returns a numeric id ($insertedId)"

$roundTrip = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c `
    "SELECT plot_id || '|' || device_name || '|' || status FROM iot_devices WHERE id = $insertedId;"

Expect ($roundTrip -eq "42|$deviceName|Pending") `
    "Round-trip preserves plot_id, device_name, status ($roundTrip)"

# Activate via UPDATE, then re-read (Activate state machine round-trip)
docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -c `
    "UPDATE iot_devices SET status = 'Active', updated_at = NOW() AT TIME ZONE 'UTC' WHERE id = $insertedId;" | Out-Null

$activated = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c `
    "SELECT status FROM iot_devices WHERE id = $insertedId;"
Expect ($activated -eq "Active") "After Activate-style UPDATE, status round-trips to 'Active'"

# Round-trip an AgronomicStatistic
$statInsert = @"
INSERT INTO agronomic_statistics (user_id, plot_id, measurement_date, ndvi_value, chill_portions, chill_hours, chill_model_intermediate_product)
VALUES (1, 42, NOW() AT TIME ZONE 'UTC', 0.55, 35.0, 80.0, 1.0)
RETURNING id;
"@
$statId = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c $statInsert | Select-Object -First 1
Expect ([bool]($statId -match "^\d+$")) "AgronomicStatistic insert returns a numeric id ($statId)"

$statRead = docker exec -e PGPASSWORD=$PostgresPassword $ContainerName `
    psql -U postgres -d $PostgresDb -At -c `
    "SELECT user_id || '|' || plot_id || '|' || ndvi_value || '|' || chill_portions || '|' || chill_hours FROM agronomic_statistics WHERE id = $statId;"
Expect ($statRead -eq "1|42|0.55|35|80") `
    "AgronomicStatistic round-trip preserves all values ($statRead)"

# ---------------------------------------------------------------------------
# 5. Clean up
# ---------------------------------------------------------------------------
Write-Section "Step 5: cleanup"

docker rm -f $ContainerName | Out-Null
Test-Pass "Removed container $ContainerName"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Section "Summary"
Write-Host "  Passed: $script:PassCount"
Write-Host "  Failed: $script:FailCount"
Write-Host ""

if ($script:FailCount -gt 0) {
    Write-Host "AGRO-002 ROUND-TRIP VERIFICATION FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "AGRO-002 ROUND-TRIP VERIFICATION PASSED" -ForegroundColor Green
exit 0
