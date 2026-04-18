#!/usr/bin/env bash
# Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
# Licensed under the MIT License. See LICENSE file in the project root for full license information.
#
# Linux/macOS counterpart of run.ps1. Same flags:
#   --report      Open HTML report (uses xdg-open / open)
#   --check       Run threshold gate after collection
#   --baseline    Save current run as new baseline
#
# Why dotnet-coverage instead of coverlet.console?
# xUnit v3 runs on Microsoft Testing Platform (MTP). coverlet.console 10.0.0
# (released 2026-04-17) added MTP integration but its attach path silently captures
# zero coverage on Ubuntu CI runners — tests pass, the cobertura file is generated,
# but it contains zero <class> entries. Microsoft's own dotnet-coverage tool is
# designed for MTP and works reliably across Windows/Linux/macOS, which is what xUnit
# v3 + MTP teams recommend for v3 projects.

set -euo pipefail

REPORT=0
CHECK=0
BASELINE=0
CFG="Release"

for arg in "$@"; do
    case "$arg" in
        --report)   REPORT=1 ;;
        --check)    CHECK=1 ;;
        --baseline) BASELINE=1 ;;
        --debug)    CFG="Debug" ;;
    esac
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="$REPO_ROOT/out/coverage"
COBERTURA="$OUT_DIR/coverage.cobertura.xml"
SETTINGS="$REPO_ROOT/tools/coverage/coverage.runsettings"
mkdir -p "$OUT_DIR"

# KEEP IN SYNC with MatPlotLibNet.CI.slnf + run.ps1. Any test project referenced by
# CI must also be measured here — otherwise classes exercised only by those projects
# appear uncovered in the baseline and the 90/90 gate reports false gaps.
# (Phase O follow-on 2026-04-18: added Blazor, Avalonia, AspNetCore, Interactive,
#  DataFrame, GraphQL — previously only Tests + Skia + Geo were measured.)
TEST_PROJECTS=(
    "Tst/MatPlotLibNet/MatPlotLibNet.Tests.csproj"
    "Tst/MatPlotLibNet.Skia/MatPlotLibNet.Skia.Tests.csproj"
)
OPTIONAL_PROJS=(
    "Tst/MatPlotLibNet.Geo/MatPlotLibNet.Geo.Tests.csproj"
    "Tst/MatPlotLibNet.Blazor/MatPlotLibNet.Blazor.Tests.csproj"
    "Tst/MatPlotLibNet.Avalonia/MatPlotLibNet.Avalonia.Tests.csproj"
    "Tst/MatPlotLibNet.AspNetCore/MatPlotLibNet.AspNetCore.Tests.csproj"
    "Tst/MatPlotLibNet.Interactive/MatPlotLibNet.Interactive.Tests.csproj"
    "Tst/MatPlotLibNet.DataFrame/MatPlotLibNet.DataFrame.Tests.csproj"
    "Tst/MatPlotLibNet.GraphQL/MatPlotLibNet.GraphQL.Tests.csproj"
)
for p in "${OPTIONAL_PROJS[@]}"; do
    [ -f "$REPO_ROOT/$p" ] && TEST_PROJECTS+=("$p")
done

echo "==> Building test projects ($CFG)..."
for proj in "${TEST_PROJECTS[@]}"; do
    dotnet build "$REPO_ROOT/$proj" -c "$CFG" --nologo -v:q > /dev/null
done

PARTIALS=()
for proj in "${TEST_PROJECTS[@]}"; do
    NAME="$(basename "$proj" .csproj)"
    PROJ_DIR="$(dirname "$proj")"
    DLL="$REPO_ROOT/$PROJ_DIR/bin/$CFG/net10.0/$NAME.dll"
    [ -f "$DLL" ] || { echo "Skipping $NAME — $DLL not found"; continue; }
    PARTIAL="$OUT_DIR/$NAME.cobertura.xml"
    echo "==> Coverage: $NAME"
    dotnet-coverage collect "dotnet exec $DLL" \
        --settings "$SETTINGS" \
        --output "$PARTIAL" \
        --output-format cobertura
    PARTIALS+=("$PARTIAL")
done

echo "==> Merging partials into $COBERTURA..."
REPORTS_ARG=$(IFS=';'; echo "${PARTIALS[*]}")
reportgenerator -reports:"$REPORTS_ARG" -targetdir:"$OUT_DIR" -reporttypes:Cobertura -verbosity:Warning > /dev/null
[ -f "$OUT_DIR/Cobertura.xml" ] && mv "$OUT_DIR/Cobertura.xml" "$COBERTURA"

if [ "$BASELINE" -eq 1 ]; then
    cp "$COBERTURA" "$REPO_ROOT/tools/coverage/baseline.cobertura.xml"
    echo "==> Baseline updated"
fi

if [ "$REPORT" -eq 1 ]; then
    reportgenerator -reports:"$COBERTURA" -targetdir:"$OUT_DIR/report" -reporttypes:"Html_Light;TextSummary" -verbosity:Warning > /dev/null
    head -25 "$OUT_DIR/report/Summary.txt"
    if command -v xdg-open > /dev/null; then xdg-open "$OUT_DIR/report/index.html" &
    elif command -v open > /dev/null; then open "$OUT_DIR/report/index.html" &
    fi
fi

if [ "$CHECK" -eq 1 ]; then
    pwsh "$REPO_ROOT/tools/coverage/check-thresholds.ps1" -Cobertura "$COBERTURA"
fi

echo "==> Done. Cobertura at: $COBERTURA"
