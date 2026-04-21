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
# auto-trigger coverlet.msbuild. We use Microsoft's `dotnet-coverage` tool (the
# official cross-platform coverage collector for the Microsoft Testing Platform that
# xUnit v3 runs on) plus `dotnet-reportgenerator-globaltool` (both installed as
# global tools) and stitch the multiple test assemblies together.
#
# Why not coverlet.console? coverlet.console 10.0.0 (released 2026-04-17) added MTP
# integration but its attach path silently captures zero coverage on Ubuntu CI runners
# — tests pass, the cobertura file is generated, but it has zero <class> entries.
# dotnet-coverage is what xUnit v3 + Microsoft both recommend for v3 projects.

[CmdletBinding()]
param(
    [switch]$Report,        # Open HTML report in browser when done
    [switch]$Check,         # Run check-thresholds.ps1 after collection
    [switch]$SetBaseline,   # Save current run as the new baseline
    [switch]$Strict,        # Enforce absolute 90/90 thresholds (not just baseline-regression check)
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot   = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$outDir     = Join-Path $repoRoot "out\coverage"
$reportDir  = Join-Path $outDir "report"
$cobertura  = Join-Path $outDir "coverage.cobertura.xml"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Test assemblies that contribute to coverage (built first below).
# KEEP IN SYNC with MatPlotLibNet.CI.slnf + run.sh. Any test project referenced by CI
# must also be measured here — otherwise classes exercised only by those projects
# appear uncovered in the baseline and the 90/90 gate reports false gaps.
# (Phase O follow-on 2026-04-18: added Blazor, Avalonia, AspNetCore, Interactive,
#  DataFrame, GraphQL — previously only Tests + Skia + Geo were measured, so the
#  other 6 test projects' contributions were invisible to the coverage report.)
$testProjects = @(
    "Tst\MatPlotLibNet\MatPlotLibNet.Tests.csproj",
    "Tst\MatPlotLibNet.Skia\MatPlotLibNet.Skia.Tests.csproj"
)
# Additional CI-gated projects. Guard each with Test-Path so the script still runs
# in earlier-version checkouts where a project doesn't exist yet.
$optionalProjs = @(
    "Tst\MatPlotLibNet.Geo\MatPlotLibNet.Geo.Tests.csproj",
    "Tst\MatPlotLibNet.Blazor\MatPlotLibNet.Blazor.Tests.csproj",
    "Tst\MatPlotLibNet.Avalonia\MatPlotLibNet.Avalonia.Tests.csproj",
    "Tst\MatPlotLibNet.AspNetCore\MatPlotLibNet.AspNetCore.Tests.csproj",
    "Tst\MatPlotLibNet.Interactive\MatPlotLibNet.Interactive.Tests.csproj",
    "Tst\MatPlotLibNet.DataFrame\MatPlotLibNet.DataFrame.Tests.csproj",
    "Tst\MatPlotLibNet.GraphQL\MatPlotLibNet.GraphQL.Tests.csproj"
)
foreach ($p in $optionalProjs) {
    if (Test-Path (Join-Path $repoRoot $p)) { $testProjects += $p }
}

Write-Host "==> Building test projects ($Configuration)..." -ForegroundColor Cyan
foreach ($proj in $testProjects) {
    & dotnet build (Join-Path $repoRoot $proj) -c $Configuration --nologo -v:q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $proj" }
}

# dotnet-coverage collects coverage of one process tree per invocation. We run each
# test project separately and merge the resulting Cobertura files via reportgenerator.
# Module include/exclude rules live in coverage.runsettings (DRY across run.sh/run.ps1).
$settings = Join-Path $PSScriptRoot "coverage.runsettings"
$partialFiles = @()
foreach ($proj in $testProjects) {
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $projDir  = [System.IO.Path]::GetDirectoryName($proj)
    $tfm      = "net10.0"
    $dll      = Join-Path $repoRoot (Join-Path $projDir "bin\$Configuration\$tfm\$projName.dll")
    if (-not (Test-Path $dll)) { Write-Warning "Skipping $projName -- assembly not found at $dll"; continue }

    $partial  = Join-Path $outDir "$projName.cobertura.xml"
    Write-Host "==> Coverage: $projName" -ForegroundColor Cyan

    & dotnet-coverage collect "dotnet exec $dll" `
        --settings $settings `
        --output $partial `
        --output-format cobertura `
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
    $checkArgs = @("-Cobertura", $cobertura)
    if ($Strict) { $checkArgs += "-Strict" }
    & pwsh (Join-Path $PSScriptRoot "check-thresholds.ps1") @checkArgs
    exit $LASTEXITCODE
}

Write-Host "==> Done. Cobertura at: $cobertura" -ForegroundColor Green
