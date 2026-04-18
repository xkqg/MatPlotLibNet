# Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#
# Coverage runner -- single command to produce per-class coverage for the entire codebase.
# Default usage:    pwsh tools/coverage/run.ps1
# With report:      pwsh tools/coverage/run.ps1 -Report      (opens HTML in browser)
# With check:       pwsh tools/coverage/run.ps1 -Check       (fails if below thresholds)
# Update baseline:  pwsh tools/coverage/run.ps1 -SetBaseline (writes baseline.cobertura.xml)
#
# Why a custom script?
# The project uses xunit v3 Exe pattern (dotnet run, not dotnet test) which doesn't
# auto-trigger coverlet.msbuild. We use coverlet.console + dotnet-reportgenerator
# (both installed as global tools) and stitch the multiple test assemblies together.

[CmdletBinding()]
param(
    [switch]$Report,        # Open HTML report in browser when done
    [switch]$Check,         # Run check-thresholds.ps1 after collection
    [switch]$SetBaseline,   # Save current run as the new baseline
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$outDir     = Join-Path $repoRoot "out\coverage"
$reportDir  = Join-Path $outDir "report"
$cobertura  = Join-Path $outDir "coverage.cobertura.xml"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Test assemblies that contribute to coverage (built first below).
# Add new test projects here as they are introduced (Phase 1 includes Geo + Skia).
$testProjects = @(
    "Tst\MatPlotLibNet\MatPlotLibNet.Tests.csproj",
    "Tst\MatPlotLibNet.Skia\MatPlotLibNet.Skia.Tests.csproj"
)
# Geo test project added by Phase 1.4 -- include only if it exists yet.
$geoProj = "Tst\MatPlotLibNet.Geo\MatPlotLibNet.Geo.Tests.csproj"
if (Test-Path (Join-Path $repoRoot $geoProj)) { $testProjects += $geoProj }

Write-Host "==> Building test projects ($Configuration)..." -ForegroundColor Cyan
foreach ($proj in $testProjects) {
    & dotnet build (Join-Path $repoRoot $proj) -c $Configuration --nologo -v:q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $proj" }
}

# Coverlet only collects coverage of one assembly per invocation. We run each test
# project separately and merge the resulting Cobertura files via reportgenerator.
$partialFiles = @()
foreach ($proj in $testProjects) {
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $projDir  = [System.IO.Path]::GetDirectoryName($proj)
    $tfm      = "net10.0"
    $dll      = Join-Path $repoRoot (Join-Path $projDir "bin\$Configuration\$tfm\$projName.dll")
    if (-not (Test-Path $dll)) { Write-Warning "Skipping $projName -- assembly not found at $dll"; continue }

    $partial  = Join-Path $outDir "$projName.cobertura.xml"
    Write-Host "==> Coverage: $projName" -ForegroundColor Cyan

    & coverlet $dll `
        --target "dotnet" --targetargs $dll `
        --format cobertura --output $partial `
        --include "[MatPlotLibNet]*" --include "[MatPlotLibNet.Geo]*" `
        --include "[MatPlotLibNet.Skia]*" --include "[MatPlotLibNet.Playground]*" `
        --exclude "[*]MatPlotLibNet.Tests.*" --exclude "[xunit*]*" `
        --exclude "[*]System.Text.RegularExpressions.Generated.*" `
        | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Coverage collection failed for $projName" }
    $partialFiles += $partial
}

Write-Host "==> Merging partials into $cobertura..." -ForegroundColor Cyan
$reports = ($partialFiles -join ";")
& reportgenerator -reports:$reports -targetdir:$outDir -reporttypes:Cobertura -verbosity:Warning | Out-Null
# reportgenerator writes Cobertura.xml -- rename to canonical name
$generated = Join-Path $outDir "Cobertura.xml"
if (Test-Path $generated) { Move-Item -Force $generated $cobertura }

if ($SetBaseline) {
    $baseline = Join-Path $PSScriptRoot "baseline.cobertura.xml"
    Copy-Item -Force $cobertura $baseline
    Write-Host "==> Baseline updated: $baseline" -ForegroundColor Green
}

if ($Report) {
    Write-Host "==> Generating HTML report..." -ForegroundColor Cyan
    & reportgenerator -reports:$cobertura -targetdir:$reportDir -reporttypes:"Html_Light;TextSummary" -verbosity:Warning | Out-Null
    Get-Content (Join-Path $reportDir "Summary.txt") | Select-Object -First 25
    Start-Process (Join-Path $reportDir "index.html")
}

if ($Check) {
    Write-Host "==> Running threshold check..." -ForegroundColor Cyan
    & pwsh (Join-Path $PSScriptRoot "check-thresholds.ps1") -Cobertura $cobertura
    exit $LASTEXITCODE
}

Write-Host "==> Done. Cobertura at: $cobertura" -ForegroundColor Green
