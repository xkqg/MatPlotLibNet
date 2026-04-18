# Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#
# Coverage threshold gate. Reads a Cobertura XML + thresholds.json. Exits 1 if any
# class falls below its configured (or default) line / branch threshold. Used by:
#   - tools/coverage/run.ps1 -Check (local)
#   - .github/workflows/ci.yml      (CI)
#
# Why our own checker instead of coverlet's /p:Threshold?
# Coverlet's threshold is a single global number. We need per-class enforcement with
# documented exemptions, baseline-delta protection (no class may regress), and a
# scannable PR-comment-friendly summary. Doing this in MSBuild is painful.

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Cobertura,
    [string] $Thresholds,
    [string] $Baseline,
    [switch] $NoBaseline,   # Skip baseline comparison (e.g., first run, no baseline yet)
    [switch] $Strict        # Enforce absolute thresholds in addition to baseline regression check.
                            # Default (CI mode) checks ONLY for regression vs baseline so the
                            # gate is green from day 1 and only fails when someone weakens
                            # coverage. The 90/90 absolute target is aspirational and
                            # tracked through the per-phase coverage uplift in COVERAGE.md.
)

# Resolve script directory robustly (PSScriptRoot may be empty when invoked via -File
# with a relative path on Windows PowerShell 5.1).
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Thresholds) { $Thresholds = Join-Path $scriptDir "thresholds.json" }
if (-not $Baseline)   { $Baseline   = Join-Path $scriptDir "baseline.cobertura.xml" }

$ErrorActionPreference = "Stop"

if (-not (Test-Path $Cobertura))  { throw "Coverage file not found: $Cobertura" }
if (-not (Test-Path $Thresholds)) { throw "Thresholds config not found: $Thresholds" }

$cfg     = Get-Content $Thresholds -Raw | ConvertFrom-Json
$defLine   = $cfg.default.line
$defBranch = $cfg.default.branch

# Build exemption lookup once -- JSON property names are class FQN keys
$exempt = @{}
if ($cfg.exemptions) {
    $cfg.exemptions.PSObject.Properties | ForEach-Object {
        $exempt[$_.Name] = $_.Value
    }
}

# Parse Cobertura XML -- extract per-class line + branch coverage
[xml]$xml = Get-Content $Cobertura
$classes = $xml.SelectNodes("//class")
if ($classes.Count -eq 0) { throw "No <class> nodes found in $Cobertura -- corrupt or wrong file?" }

# Optional: load baseline for regression check
$baselineMap = @{}
if (-not $NoBaseline -and (Test-Path $Baseline)) {
    [xml]$baseXml = Get-Content $Baseline
    foreach ($c in $baseXml.SelectNodes("//class")) {
        $name = $c.GetAttribute("name")
        $baselineMap[$name] = [PSCustomObject]@{
            Line   = [double]$c.GetAttribute("line-rate")   * 100
            Branch = [double]$c.GetAttribute("branch-rate") * 100
        }
    }
}

$violations = @()
$regressions = @()

foreach ($c in $classes) {
    $name   = $c.GetAttribute("name")
    $line   = [math]::Round([double]$c.GetAttribute("line-rate")   * 100, 1)
    $branch = [math]::Round([double]$c.GetAttribute("branch-rate") * 100, 1)

    # Resolve effective thresholds (exemption beats default)
    $minLine   = $defLine
    $minBranch = $defBranch
    $reason    = ""
    if ($exempt.ContainsKey($name)) {
        $minLine   = $exempt[$name].line
        $minBranch = $exempt[$name].branch
        $reason    = $exempt[$name].reason
    }

    # Absolute threshold check -- only fails the build under -Strict. Otherwise
    # collected for an informational summary so we can see how many classes still
    # need work without making CI permanently red.
    if ($line -lt $minLine -or $branch -lt $minBranch) {
        $violations += [PSCustomObject]@{
            Class   = $name
            Line    = $line
            Branch  = $branch
            MinLine = $minLine
            MinBr   = $minBranch
            Reason  = $reason
        }
    }

    # Regression: baseline says X but we now have <X (within rounding)
    if ($baselineMap.ContainsKey($name)) {
        $b = $baselineMap[$name]
        # Tolerate 0.1pp jitter from instrumentation noise
        if (($line   -lt $b.Line   - 0.1) -or
            ($branch -lt $b.Branch - 0.1)) {
            $regressions += [PSCustomObject]@{
                Class    = $name
                LineNow  = $line   ; LineBase  = [math]::Round($b.Line, 1)
                BrNow    = $branch ; BrBase    = [math]::Round($b.Branch, 1)
            }
        }
    }
}

# --- Reporting ---
$summary = $xml.SelectSingleNode("//coverage")
if ($summary) {
    $totalLine   = [math]::Round([double]$summary.GetAttribute("line-rate")   * 100, 1)
    $totalBranch = [math]::Round([double]$summary.GetAttribute("branch-rate") * 100, 1)
    Write-Host "Total: $totalLine% line / $totalBranch% branch" -ForegroundColor Cyan
}

if ($violations.Count -gt 0) {
    Write-Host ""
    $verb = if ($Strict) { "FAIL" } else { "INFO (advisory, not failing CI)" }
    $color = if ($Strict) { "Red" } else { "Yellow" }
    Write-Host "${verb}: $($violations.Count) class(es) below 90/90 absolute target:" -ForegroundColor $color
    # Show only the first 10 to keep CI logs scannable
    $violations | Sort-Object Line | Select-Object -First 10 | ForEach-Object {
        $msg = "  $($_.Class) -- line $($_.Line)% (min $($_.MinLine)%), branch $($_.Branch)% (min $($_.MinBr)%)"
        if ($_.Reason) { $msg += "  [exempt: $($_.Reason)]" }
        Write-Host $msg -ForegroundColor $color
    }
    if ($violations.Count -gt 10) {
        Write-Host "  ... and $($violations.Count - 10) more (run -Check locally for full list)" -ForegroundColor $color
    }
}

if ($regressions.Count -gt 0) {
    Write-Host ""
    Write-Host "FAIL: $($regressions.Count) class(es) regressed vs baseline:" -ForegroundColor Red
    $regressions | ForEach-Object {
        Write-Host "  $($_.Class) -- line $($_.LineBase)% -> $($_.LineNow)%, branch $($_.BrBase)% -> $($_.BrNow)%" -ForegroundColor Red
    }
}

# Default behaviour: fail ONLY on regressions vs baseline. Absolute target is informational.
# -Strict: fail on either regressions OR absolute-target violations (used once we hit 90/90 globally).
$strictFailure = $Strict -and $violations.Count -gt 0
if ($regressions.Count -gt 0 -or $strictFailure) {
    Write-Host ""
    Write-Host "Failed: $($regressions.Count) regressed$(if ($Strict) { ", $($violations.Count) below threshold" } else { '' })." -ForegroundColor Red
    exit 1
}

$msg = if ($Strict) {
    "PASS: All $($classes.Count) classes meet 90/90 threshold + no regressions."
} else {
    "PASS: No regressions vs baseline ($($violations.Count) classes still below 90/90 target -- see COVERAGE.md uplift plan)."
}
Write-Host $msg -ForegroundColor Green
exit 0
