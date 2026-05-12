<#
.SYNOPSIS
    Apply db/schema.sql to a Supabase project (Windows-friendly).

.EXAMPLE
    .\scripts\setup-supabase.ps1 -ConnectionString "postgresql://postgres:PWD@db.PROJECT.supabase.co:5432/postgres"

.NOTES
    Requires psql in PATH. Install via the PostgreSQL Windows installer or `winget install PostgreSQL.PostgreSQL`.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    Write-Error "psql is not installed or not in PATH. Install PostgreSQL client tools first."
    exit 1
}

$schema = Join-Path $PSScriptRoot '..\db\schema.sql' | Resolve-Path
Write-Host ">>> Applying schema to Supabase project..." -ForegroundColor Cyan
& psql $ConnectionString -v ON_ERROR_STOP=1 -f $schema
Write-Host ">>> Done." -ForegroundColor Green
