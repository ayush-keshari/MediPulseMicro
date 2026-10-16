[CmdletBinding()]
param(
    [switch]$SkipIntegration,
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [switch]$Rebuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Invoke-CheckedCommand {
    param([Parameter(Mandatory)] [string]$Name, [Parameter(Mandatory)] [scriptblock]$Command)
    Write-Host "`n==> $Name" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Name failed with exit code $LASTEXITCODE" }
}

function Assert-CommandAvailable([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

Assert-CommandAvailable 'dotnet'
if (-not $SkipFrontend) { Assert-CommandAvailable 'npm' }
if (-not $SkipIntegration) { Assert-CommandAvailable 'docker' }

$qualityArgs = @()
if ($SkipFrontend) { $qualityArgs += '-SkipFrontend' }
if ($SkipBackend) { $qualityArgs += '-SkipBackend' }

Invoke-CheckedCommand 'Offline backend and frontend validation' {
    & (Join-Path $repoRoot 'Run-QualityChecks.ps1') @qualityArgs
}

if (-not $SkipIntegration) {
    $integrationArgs = @()
    if ($Rebuild) { $integrationArgs += '-Rebuild' }
    Invoke-CheckedCommand 'Docker-backed integration validation' {
        & (Join-Path $repoRoot 'Run-IntegrationTests.ps1') @integrationArgs
    }
}

Write-Host "`nAll selected MediPulse validation stages passed." -ForegroundColor Green
