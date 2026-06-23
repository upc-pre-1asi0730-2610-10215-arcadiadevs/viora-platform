@echo off
REM Wrapper to run setup-local.ps1 bypassing PowerShell execution policy.
REM Usage: setup-local.cmd [PostgresPassword] [PostgresVersion] [DatabaseName]

set "POSTGRES_PASSWORD=%~1"
if "%POSTGRES_PASSWORD%"=="" set "POSTGRES_PASSWORD=postgres"

set "POSTGRES_VERSION=%~2"
if "%POSTGRES_VERSION%"=="" set "POSTGRES_VERSION=17"

set "DATABASE_NAME=%~3"
if "%DATABASE_NAME%"=="" set "DATABASE_NAME=viora_platform"

powershell.exe -ExecutionPolicy Bypass -File "%~dp0setup-local.ps1" -PostgresPassword "%POSTGRES_PASSWORD%" -PostgresVersion "%POSTGRES_VERSION%" -DatabaseName "%DATABASE_NAME%"

pause
