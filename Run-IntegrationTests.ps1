# Run-IntegrationTests.ps1
# A script to run integration tests using docker-compose.test.yml
#
# Usage:
#   ./Run-IntegrationTests.ps1
#
# The script will:
# 1. Start dependencies using docker-compose.test.yml
# 2. Wait for services to be ready
# 3. Run integration tests
# 4. Tear down containers
# 5. Return the exit code from the test run

param(
    [switch] $Rebuild = $false,    # Rebuild Docker images
    [switch] $NoBuild = $false,    # Skip building Docker images
    [string] $TestFilter = ""       # Filter for specific tests (optional)
)

# Set error handling
$ErrorActionPreference = "Stop"

function cleanup {
    Write-Host "Cleaning up Docker containers..." -ForegroundColor Cyan
    docker compose -f docker-compose.test.yml down -v
}

# Ensure cleanup happens even if script fails or is interrupted
trap {
    Write-Host "Interrupted! Cleaning up..." -ForegroundColor Yellow
    cleanup
    exit 1
} EXIT

try {
    Write-Host "Starting integration test environment..." -ForegroundColor Green

    # Build or reuse Docker images
    $buildArgs = @()
    if ($Rebuild) {
        $buildArgs += "--build"
    } elseif ($NoBuild) {
        $buildArgs += "--no-build"
    }

    # Start services in background
    Write-Host "Starting Docker services..." -ForegroundColor Cyan
    docker compose -f docker-compose.test.yml up -d @buildArgs

    # Wait for SQL Server to be ready
    Write-Host "Waiting for SQL Server to be ready..." -ForegroundColor Cyan
    $sqlReady = $false
    for ($i = 0; $i -lt 30; $i++) {
        try {
            $result = docker compose -f docker-compose.test.yml exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'MediPulse@2024!Test' -Q 'SELECT 1' -b -No 2>$null
            if ($LASTEXITCODE -eq 0) {
                $sqlReady = $true
                break
            }
        } catch {}
        Write-Host "Waiting for SQL Server... ($($i+1)/30)" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }

    if (-not $sqlReady) {
        Write-Error "SQL Server did not become ready in time"
        exit 1
    }

    Write-Host "SQL Server is ready!" -ForegroundColor Green

    # Wait for migrator to complete migrations and data loading
    Write-Host "Waiting for migrator to complete..." -ForegroundColor Cyan
    $migratorResult = docker compose -f docker-compose.test.yml wait migrator
    $migratorExitCode = $LASTEXITCODE

    if ($migratorExitCode -ne 0) {
        Write-Error "Migrator failed with exit code: $migratorExitCode"
        # Show migrator logs for debugging
        docker compose -f docker-compose.test.yml logs migrator
        exit $migratorExitCode
    }

    Write-Host "Migrator completed successfully!" -ForegroundColor Green

    # Give services a moment to start
    Write-Host "Waiting for services to start..." -ForegroundColor Cyan
    Start-Sleep -Seconds 10

    # Run integration tests
    Write-Host "Running integration tests..." -ForegroundColor Green

    $testArgs = @("IntegrationTests/IntegrationTests.csproj")
    if ($TestFilter) {
        $testArgs += "--filter", $TestFilter
    }

    # Set environment variable for test fixture
    $env:GATEWAY_BASE_URL = "http://localhost:5000"

    dotnet test @testArgs `
        --no-build `
        --verbosity normal `
        --logger "trx;LogFileName=integration_test_results.trx" `
        --results-directory "./TestResults"

    $testExitCode = $LASTEXITCODE

    if ($testExitCode -eq 0) {
        Write-Host "Integration tests passed!" -ForegroundColor Green
    } else {
        Write-Error "Integration tests failed with exit code: $testExitCode"
    }

    exit $testExitCode
}
finally {
    cleanup
}