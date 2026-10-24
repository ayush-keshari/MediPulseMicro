[CmdletBinding()]
param(
    [string]$ProjectDir = $PSScriptRoot,
    [string]$ProfilesDir = $env:DBT_PROFILES_DIR,
    [string]$Selector = "",
    [switch]$SkipRun,
    [switch]$SkipFreshness
)

$ErrorActionPreference = 'Stop'

$gateArgs = @((Join-Path $PSScriptRoot 'quality_gate.py'), '--project-dir', $ProjectDir)
if ($ProfilesDir) { $gateArgs += @('--profiles-dir', $ProfilesDir) }
if ($Selector) { $gateArgs += @('--select', $Selector) }
if ($SkipRun) { $gateArgs += '--skip-run' }
if ($SkipFreshness) { $gateArgs += '--skip-freshness' }
& python @gateArgs
if ($LASTEXITCODE -ne 0) { throw "dbt quality gate failed with exit code $LASTEXITCODE" }
