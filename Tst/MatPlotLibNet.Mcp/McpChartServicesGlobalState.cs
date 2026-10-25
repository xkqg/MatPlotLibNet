// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>
/// This project's own copy of the core suite's <c>ChartServicesGlobalStateCollection</c> — xUnit resolves a
/// <see cref="CollectionDefinitionAttribute"/> only within the assembly that declares it, so the core one has no
/// effect here. <see cref="MatPlotLibNet.Mcp.ChartRendering"/> is the PNG backend every tool call goes through, and
/// loading it (any <c>Render</c>/<c>Save</c> call with <c>ChartFormat.Png</c> or <c>.Pdf</c>) runs
/// <c>MatPlotLibNet.Skia</c>'s <c>[ModuleInitializer]</c>, which installs a glyph-path
/// <c>GlyphPathProviderDefault.Current</c> process-wide and for good: there is no corresponding uninstall. A test
/// that reads SVG shaped by the no-Skia default (a literal <c>&lt;text&gt;</c>, or a round-trip equality between
/// two <c>ToSvg()</c> calls) must never run concurrently with one that triggers the load, or the two calls can
/// straddle the moment it installs — one render sees <c>&lt;text&gt;</c>, the next sees <c>&lt;path&gt;</c> glyph
/// outlines, mid-test. Measured 2026-10-22: <c>SpecFigureSettingsTests.ABuiltInThemeWrittenByAFigure_PassesTheSpecReader</c>
/// failed exactly this way in CI (release v1.20.0, run 39803519299) — the default-collection race xUnit runs
/// between test classes by default. Every class in this project that can trigger the load, and every class whose
/// assertions depend on the glyph mode staying put for the length of one test, joins this collection so none of
/// them ever runs next to another.
/// </summary>
[CollectionDefinition("ChartServicesGlobalState", DisableParallelization = true)]
public sealed class McpChartServicesGlobalState
{
}
