[CmdletBinding()]
param(
    [string]$ProjectDir = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [string]$ProfilesDir = $env:DBT_PROFILES_DIR,
    [string]$Selector = "",
    [switch]$SkipRun,
    [switch]$SkipFreshness
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProfilesDir)) {
    $ProfilesDir = $ProjectDir
}

function Invoke-Dbt {
    param([Parameter(Mandatory)] [string]$Name, [Parameter(Mandatory)] [string[]]$Arguments)

    Write-Host "`n==> $Name" -ForegroundColor Cyan
    & dbt @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

$commonArgs = @(
    '--project-dir', $ProjectDir,
    '--profiles-dir', $ProfilesDir,
    '--no-partial-parse'
)
$selectorArgs = @()
if (-not [string]::IsNullOrWhiteSpace($Selector)) {
    $selectorArgs = @('--select', $Selector)
}

Invoke-Dbt 'Install dbt project dependencies' (@('deps') + $commonArgs)
Invoke-Dbt 'Validate dbt metadata' (@('parse') + $commonArgs)

if (-not $SkipRun) {
    Invoke-Dbt 'Materialize selected staging models' (@('run') + $commonArgs + $selectorArgs)
}

Invoke-Dbt 'Run selected data-quality tests' (@('test') + $commonArgs + $selectorArgs)

if (-not $SkipFreshness) {
    Invoke-Dbt 'Verify inventory source freshness' (@('source', 'freshness') + $commonArgs + @('--select', 'source:mediapulse_main.InventoryPositions'))
}

Write-Host "`nQuality gate completed successfully. Re-running it is safe; no source rows are modified." -ForegroundColor Green
