# Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#
# Pack the packages the fleet consumes into the shared local feed, from a CLEAN tree, then VERIFY the feed.
# Explicit pack (never GeneratePackageOnBuild into the feed): a build must not rewrite the feed under an existing
# version number — a consumer that already extracted that id/version keeps the OLD bytes. Never repack a version
# a consumer has DEPLOYED; bump the in-flight number instead (CONTRIBUTING.md: the CHANGELOG heading stays [Next]).
#
#   powershell -File tools\pack-feed.ps1            # packs the version the csprojs carry
param(
    [string]$Feed = 'C:\Ait\ait-nuget'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$projects = @(
    'Src\MatPlotLibNet\MatPlotLibNet.csproj',
    'Src\MatPlotLibNet.AspNetCore\MatPlotLibNet.AspNetCore.csproj',
    'Src\MatPlotLibNet.Blazor\MatPlotLibNet.Blazor.csproj',
    'Src\MatPlotLibNet.Skia\MatPlotLibNet.Skia.csproj'
)
$version = ([xml](Get-Content (Join-Path $root $projects[0]))).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (Get-ChildItem $Feed -Filter "MatPlotLibNet.$version.nupkg" -ErrorAction SilentlyContinue) {
    throw "MatPlotLibNet $version is already on the feed - bump the in-flight number, never repack."
}
foreach ($p in $projects) {
    $dir = Split-Path -Parent (Join-Path $root $p)
    Get-ChildItem $dir -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}
foreach ($p in $projects) {
    Write-Host "pack $p ($version)"
    dotnet build (Join-Path $root $p) -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "build failed: $p" }
    dotnet pack (Join-Path $root $p) -c Release -o $Feed --nologo -v q --no-build
    if ($LASTEXITCODE -ne 0) { throw "pack failed: $p" }
}
Write-Host "--- packed $version ---"
Get-ChildItem $Feed -Filter "MatPlotLibNet*.$version.nupkg" | ForEach-Object { $_.Name }
Write-Host '--- VerifyFeed ---'
dotnet run --project C:\Ait\Core\tools\feed\VerifyFeed -- $Feed
if ($LASTEXITCODE -ne 0) { throw "VerifyFeed FAILED" }
Write-Host 'VerifyFeed OK'
