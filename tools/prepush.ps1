# Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#
# Everything CI judges, before the push instead of after it.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/prepush.ps1
#   ... -SkipCoverage      the eleven measured suites + gate take ~10 min; skip only for a docs-only change
#                          (the wiki suite and the WPF suite run either way)
#
# Why this file exists: the checks were being run one at a time and from memory, so the one that had never
# been run locally -- the docfx site the Pages workflow builds -- was the one that broke. A list that must be
# remembered is only as good as the memory of whoever remembers it.
#
# WHAT THIS CANNOT JUDGE, ever, on a Windows box:
#   * The Pages workflow runs on ubuntu-latest. Anything conditioned on a non-Windows host --
#     EnableWindowsTargeting above all -- is INERT here and live there. A change to any TargetFramework, to a
#     csproj property, or to docs/docfx.json therefore has no local verdict: READ it against the runner's OS.
#     Do not push a probe to find out -- one push per finished strand, then block on the Actions page.
#   * The MAUI projects (no workload on this box) and anything the workload would resolve.

[CmdletBinding()]
param(
    [switch]$SkipCoverage,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$failures = [System.Collections.Generic.List[string]]::new()

function Step {
    param([string]$Name, [scriptblock]$Body)
    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    try {
        & $Body
    } catch {
        $failures.Add("$Name -- $($_.Exception.Message)")
        Write-Host "    FAILED: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Assert-CleanBuild {
    param([string]$Project)
    $out = & dotnet build $Project -c $Configuration --nologo 2>&1 | Out-String
    $warnings = [regex]::Match($out, '(\d+) Warning\(s\)')
    $errors = [regex]::Match($out, '(\d+) Error\(s\)')
    if (-not $errors.Success) { throw "no build summary for $Project" }
    $w = [int]$warnings.Groups[1].Value
    $e = [int]$errors.Groups[1].Value
    Write-Host ("    {0,-58} {1} warning(s), {2} error(s)" -f (Split-Path -Leaf $Project), $w, $e)
    if ($e -ne 0 -or $w -ne 0) {
        ($out -split "`n" | Where-Object { $_ -match ': (error|warning) ' } | Select-Object -First 8) | ForEach-Object { Write-Host "      $_" }
        throw "$Project is not 0 warnings / 0 errors"
    }
}

# --- 1. Everything CI builds, plus everything CI does NOT build but a contributor still breaks --------------
Step "Build (CI solution filter)" { Assert-CleanBuild "MatPlotLibNet.CI.slnf" }

Step "Build (projects outside the CI filter)" {
    # MAUI is excluded on purpose: no workload on this box, and none on the Pages runner either.
    $outside = @(
        "Src/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.csproj",
        "Tst/MatPlotLibNet.Wpf/MatPlotLibNet.Wpf.Tests.csproj",
        "Src/MatPlotLibNet.Uno/MatPlotLibNet.Uno.csproj",
        "Src/MatPlotLibNet.Notebooks/MatPlotLibNet.Notebooks.csproj",
        "Benchmarks/MatPlotLibNet.Benchmarks/MatPlotLibNet.Benchmarks.csproj"
    ) + (Get-ChildItem "Samples" -Directory | ForEach-Object {
        Get-ChildItem $_.FullName -Filter "*.csproj" -File | Select-Object -ExpandProperty FullName
    })
    foreach ($p in $outside) { if (Test-Path $p) { Assert-CleanBuild $p } }
}

# --- 2. The eleven measured suites and the strict per-class gate ---------------------------------------------
if (-not $SkipCoverage) {
    Step "Tests + strict coverage gate" {
        $ps = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
        & $ps -NoProfile -ExecutionPolicy Bypass -File "tools/coverage/run.ps1" -Check -Strict | Tee-Object -Variable gate | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "the gate did not pass" }
        if (($gate -join "`n") -notmatch "PASS: All \d+ classes") { throw "no PASS line from the gate" }
    }
} else {
    Write-Host ""
    Write-Host "==> Tests + coverage gate SKIPPED (-SkipCoverage)" -ForegroundColor Yellow
}

# --- 2b. The Windows-only suite the runners cannot reach -----------------------------------------------------
# MatPlotLibNet.Wpf is not in the CI filter and not in the coverage runsettings, because the CI runners are
# ubuntu. Its suite therefore runs in exactly one place: here. Building it is not the same as running it, and a
# test nothing runs is a comment.
Step "WPF suite (Windows only, runs nowhere else)" {
    $exe = Get-ChildItem "Tst/MatPlotLibNet.Wpf/bin/$Configuration" -Recurse -Filter "MatPlotLibNet.Wpf.Tests.exe" -ErrorAction SilentlyContinue |
           Select-Object -First 1
    if (-not $exe) { throw "the WPF suite was not built; nothing to run" }
    & $exe.FullName | Tee-Object -Variable wpf | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "the WPF suite failed" }
    $summary = ($wpf -join "`n")
    if ($summary -notmatch "Failed:\s*0") { throw "no clean summary line from the WPF suite" }
}

# --- 2c. The wiki, compiled ----------------------------------------------------------------------------------
# Tst/MatPlotLibNet.Wiki compiles every C# block of the wiki against the packages as built. The coverage runner
# does not run it (it measures no class), so it runs here even with -SkipCoverage: a wiki edit is a docs-only
# change too. It reads the wiki beside the repository; CI clones the wiki as PUSHED, so a local edit that is not
# committed is a verdict on text CI never sees, and a wiki commit not yet pushed must go out before the repository.
Step "Wiki suite (every C# block of the wiki, compiled)" {
    $wiki = Join-Path (Split-Path -Parent $repoRoot) "MatPlotLibNet.wiki"
    if (-not (Test-Path (Join-Path $wiki "Home.md"))) {
        throw "the wiki is not beside the repository, at $wiki -- clone it from the repository's root: git clone https://github.com/xkqg/MatPlotLibNet.wiki.git ../MatPlotLibNet.wiki"
    }
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $dirty = @(git -C $wiki status --porcelain)
        if ($dirty.Count -gt 0) { throw "the wiki has $($dirty.Count) uncommitted change(s); CI compiles the wiki as pushed -- commit them first" }
        $ahead = @(try { git -C $wiki log --oneline "@{u}..HEAD" 2>$null } catch { })
        if ($ahead.Count -gt 0) { Write-Host "    the wiki is $($ahead.Count) commit(s) ahead of its remote: push the wiki BEFORE the repository" -ForegroundColor Yellow }
    }
    $exe = Get-ChildItem "Tst/MatPlotLibNet.Wiki/bin/$Configuration" -Recurse -Filter "MatPlotLibNet.Wiki.Tests.exe" -ErrorAction SilentlyContinue |
           Select-Object -First 1
    if (-not $exe) { throw "the wiki suite was not built; nothing to run" }
    & $exe.FullName | Tee-Object -Variable wikiRun | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "the wiki suite failed" }
    if (($wikiRun -join "`n") -notmatch "Failed:\s*0") { throw "no clean summary line from the wiki suite" }
}

# --- 3. The samples that RUN, actually run -------------------------------------------------------------------
# A build is not a run. A change to a collection the control room keeps its history in compiles, passes every
# unit test, and can still put nothing on the wall -- and committing that was the omission this step exists to
# make impossible. Two samples are servers and can be probed headlessly; the desktop heads are named here so
# their absence is a decision rather than an oversight.
Step "Samples that run: start and probe" {
    $servers = @(
        @{ Name = "ControlRoom"; Project = "Samples/MatPlotLibNet.Samples.ControlRoom"; Port = 5391; Expect = "<svg" },
        @{ Name = "WebApi";      Project = "Samples/MatPlotLibNet.Samples.WebApi";      Port = 5392; Expect = "<svg"; Path = "api/chart/sales.svg" },
        # The same server, asked for a right-to-left title: this project renders without the Skia package, so the
        # SVG carries <text> elements and the library must have marked the title with its reading direction.
        @{ Name = "WebApi-intl"; Project = "Samples/MatPlotLibNet.Samples.WebApi";      Port = 5393; Expect = 'direction="rtl"'; Path = "api/chart/international.svg" }
    )

    foreach ($server in $servers) {
        $log = Join-Path $env:TEMP "prepush-$($server.Name).log"
        Remove-Item -Force $log -ErrorAction SilentlyContinue
        $url = "http://127.0.0.1:$($server.Port)"
        $proc = Start-Process -FilePath "dotnet" -PassThru -WindowStyle Hidden -RedirectStandardOutput $log `
            -ArgumentList @("run", "--project", $server.Project, "-c", $Configuration, "--urls", $url)
        try {
            $body = $null
            foreach ($attempt in 1..40) {
                Start-Sleep -Milliseconds 750
                if ($proc.HasExited) { throw "$($server.Name) exited with $($proc.ExitCode) before serving -- see $log" }
                try {
                    $body = (Invoke-WebRequest "$url/$($server.Path)" -UseBasicParsing -TimeoutSec 20).Content
                    break
                } catch { }
            }

            if ($null -eq $body) { throw "$($server.Name) never answered on $url -- see $log" }
            if ($body -notmatch [regex]::Escape($server.Expect)) {
                throw "$($server.Name) answered without '$($server.Expect)' -- it served a page with no chart on it"
            }

            $charts = ([regex]::Matches($body, "<svg")).Count
            Write-Host ("    {0,-14} {1,7} bytes, {2} chart(s)" -f $server.Name, $body.Length, $charts)
        } finally {
            if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
        }
    }

    Write-Host "    NOT probed here: Blazor, GraphQL, AspNetCore (servers, no chart on the root page),"
    Write-Host "    Playground (WASM), and the desktop heads Wpf / Avalonia / Uno -- those need a window."
}

# --- 4. The site the Pages workflow builds -------------------------------------------------------------------
Step "docfx metadata + site" {
    $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
    if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) { throw "docfx not installed: dotnet tool install -g docfx" }
    Copy-Item -Recurse -Force images docs/images -ErrorAction SilentlyContinue
    # Stale pages for types that no longer exist are gitignored, so only a local checkout ever sees them --
    # and they produce InvalidBookmark warnings that read exactly like a real broken link.
    $cutoff = (Get-Date).AddMinutes(-1)
    foreach ($yml in Get-ChildItem "docs/api" -Filter "*.yml" -File) {
        if ($yml.LastWriteTime -ge $cutoff) { continue }
        git check-ignore -q $yml.FullName
        if ($LASTEXITCODE -eq 0) { Remove-Item -Force $yml.FullName }
    }
    $meta = & docfx metadata docs/docfx.json 2>&1 | Out-String
    Copy-Item -Force docs/api-toc.yml docs/api/toc.yml
    $build = & docfx build docs/docfx.json 2>&1 | Out-String
    foreach ($phase in @(@("metadata", $meta), @("build", $build))) {
        $errors = [regex]::Match($phase[1], '(\d+) error\(s\)')
        if (-not $errors.Success -or [int]$errors.Groups[1].Value -ne 0) {
            ($phase[1] -split "`n" | Where-Object { $_ -match '^error|error [A-Z]+\d+' } | Select-Object -First 10) | ForEach-Object { Write-Host "      $_" }
            throw "docfx $($phase[0]) reported errors"
        }
        Write-Host ("    docfx {0,-10} 0 error(s)" -f $phase[0])
    }
    # A referenced project that fails to load is reported as a WARNING here and as an ERROR on the Linux
    # runner: docfx falls back to the raw ref assemblies, and then the same assembly is imported twice.
    $spill = ([regex]::Matches($meta, "Found project reference without a matching metadata reference")).Count
    Write-Host "    project references without metadata: $spill (3 is the known baseline -- MORE means a project stopped loading)"
    if ($spill -gt 3) { throw "docfx lost a project reference that used to load ($spill > 3)" }
}

# --- verdict --------------------------------------------------------------------------------------------------
Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host "PRE-PUSH GREEN. ONE push for the whole finished strand -- never a push to let CI judge a piece." -ForegroundColor Green
    Write-Host "What this could not judge: the Linux-only half (EnableWindowsTargeting, TargetFramework and" -ForegroundColor Green
    Write-Host "docfx.json changes). Read those against the runner's OS, then BLOCK on the Actions page until" -ForegroundColor Green
    Write-Host "every workflow on that SHA is complete." -ForegroundColor Green
    exit 0
}

Write-Host "PRE-PUSH RED:" -ForegroundColor Red
$failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
exit 1
