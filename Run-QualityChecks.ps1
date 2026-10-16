[CmdletBinding()]
param(
    [switch]$SkipFrontend,
    [switch]$SkipBackend
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
Set-Location $repoRoot

if (-not $SkipBackend) {
    Write-Host 'Running backend tests...' -ForegroundColor Cyan
    dotnet test Backend.slnx --configuration Release --verbosity minimal -m:1 -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "Backend tests failed with exit code $LASTEXITCODE" }
}

if (-not $SkipFrontend) {
    Push-Location (Join-Path $repoRoot 'Frontend')
    try {
        if (-not (Test-Path 'node_modules')) {
            npm ci --legacy-peer-deps
            if ($LASTEXITCODE -ne 0) { throw "Frontend dependency install failed with exit code $LASTEXITCODE" }
        }

        Write-Host 'Running frontend tests, type checking, linting, and build...' -ForegroundColor Cyan
        npm test -- --watch=false
        if ($LASTEXITCODE -ne 0) { throw "Frontend tests failed with exit code $LASTEXITCODE" }
        npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Frontend type checking failed with exit code $LASTEXITCODE" }
        npm run lint
        if ($LASTEXITCODE -ne 0) { throw "Frontend linting failed with exit code $LASTEXITCODE" }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "Frontend build failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }
}

Write-Host 'All selected quality checks passed.' -ForegroundColor Green
