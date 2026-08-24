# ============================================================================
# MediPulseMicro - Data Quality Check Runner
# Executes all SQL data quality check scripts and combines results.
# ============================================================================

# Exit on any error
$ErrorActionPreference = 'Stop'

try {
    # Parameters for sqlcmd
    $server = "localhost,1433"
    $user = "sa"
    $password = $env:SA_PASSWORD
    $database = "MediPulseMicro"

    if (-not $password) {
        Write-Error "Environment variable SA_PASSWORD is not set."
        exit 1
    }

    # Get all .sql files in DQ directory, sorted for deterministic order
    $sqlFiles = Get-ChildItem -Path "$PSScriptRoot\DQ" -Filter "*.sql" | Sort-Object Name

    if ($sqlFiles.Count -eq 0) {
        Write-Error "No SQL files found in $PSScriptRoot\DQ"
        exit 1
    }

    # Initialize combined results
    $allChecks = @()
    $overallPassed = $true

    foreach ($sqlFile in $sqlFiles) {
        Write-Host "Executing $($sqlFile.Name)..."

        # Build sqlcmd command
        $sqlcmdArgs = @(
            "-S", $server,
            "-U", $user,
            "-P", $password,
            "-d", $database,
            "-h", "-1", # Remove column headers
            "-s", ",",  # Column separator (we'll use default tab? Actually JSON output doesn't need separator)
            "-W",       # Remove trailing spaces
            "-Q", "SET NOCOUNT ON; EXECUTE sp_executesql N'" + (Get-Content $sqlFile.FullName -Raw).Replace("'", "''") + "'"
        )

        # Execute sqlcmd and capture output
        $output = & sqlcmd @sqlcmdArgs 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -ne 0) {
            Write-Error "sqlcmd failed for $($sqlFile.Name) with exit code $exitCode. Output: $output"
            $overallPassed = $false
            continue
        }

        # sqlcmd may output extra lines (like empty lines). We expect a single JSON string.
        # Trim and ignore empty lines.
        $jsonLines = $output -join "`n" | Where-Object { $_ -match '\S' }
        if ($jsonLines.Count -eq 0) {
            Write-Warning "No output from $($sqlFile.Name)"
            continue
        }

        # Attempt to parse JSON
        try {
            $json = $jsonLines | ConvertFrom-Json
            if ($json -and $json.Checks) {
                $allChecks += $json.Checks
                # Check for any failures in this file
                $failedChecks = $json.Checks | Where-Object { $_.Status -eq 1 }
                if ($failedChecks) {
                    $overallPassed = $false
                    Write-Host "  FAILED: $($failedChecks.Count) check(s) failed in $($sqlFile.Name)"
                    foreach ($fc in $failedChecks) {
                        Write-Host "    $($fc.CheckName): $($fc.Message)"
                    }
                } else {
                    Write-Host "  PASSED: All checks passed in $($sqlFile.Name)"
                }
            } else {
                Write-Warning "Unexpected JSON structure from $($sqlFile.Name): $jsonLines"
                $overallPassed = $false
            }
        } catch {
            Write-Error "Failed to parse JSON from $($sqlFile.Name): $_"
            Write-Error "Output was: $jsonLines"
            $overallPassed = $false
        }
    }

    # Output combined JSON
    $combinedResult = @{ Checks = $allChecks } | ConvertTo-Json -Depth 4
    Write-Output $combinedResult

    # Exit with appropriate code
    if ($overallPassed) {
        Write-Host "Overall: ALL DATA QUALITY CHECKS PASSED."
        exit 0
    } else {
        Write-Host "Overall: SOME DATA QUALITY CHECKS FAILED."
        exit 1
    }
} catch {
    Write-Error "Unexpected error: $_"
    exit 1
}