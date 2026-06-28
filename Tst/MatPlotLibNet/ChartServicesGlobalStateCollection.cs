// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Xunit;

namespace MatPlotLibNet.Tests;

/// <summary>
/// Serializes every test that MUTATES a process-global <c>ChartServices</c> static (<c>GlyphPathProvider</c>,
/// <c>FontMetrics</c>, <c>Renderer</c>, <c>SvgRenderer</c>, <c>Serializer</c>). Those statics are read by every
/// render test, so a writer running CONCURRENTLY with a render test makes the render briefly observe the swapped
/// value — e.g. a path-returning <c>GlyphPathProvider</c> makes <c>SvgRenderContext.DrawText</c> emit text as
/// <c>&lt;path&gt;</c> glyph outlines instead of <c>&lt;text&gt;</c> elements, so a label-substring assertion
/// (<c>"&gt;X axis&lt;"</c>) fails. That cross-thread race is non-deterministic: it never hit on Windows but
/// surfaced on the Linux CI's slower, coverage-instrumented scheduling (the 3D axis-label tests). Tagging the
/// writer classes into this <see cref="CollectionDefinitionAttribute.DisableParallelization"/> collection makes
/// them run ALONE — no render test ever runs concurrently with a global-state mutation. (Mirrors the
/// Ait.Observability <c>SqlIsolated</c> serialization pattern for shared process-global / external state.)
/// </summary>
[CollectionDefinition("ChartServicesGlobalState", DisableParallelization = true)]
public sealed class ChartServicesGlobalStateCollection
{
}
