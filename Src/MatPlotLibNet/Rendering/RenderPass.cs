// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Bundles the three values that travel together through the internal per-axes render
/// plumbing: the pixel-space plot area, the drawing surface, and the active theme.
/// </summary>
/// <param name="PlotArea">The pixel-space rectangle that bounds the plot.</param>
/// <param name="Context">The drawing surface to emit primitives onto.</param>
/// <param name="Theme">The active visual theme.</param>
/// <remarks>
/// Deliberately <see langword="internal"/>: it threads only through internal render plumbing
/// (e.g. <see cref="ChartRenderer"/>) and must not widen the public API surface. Public
/// factory seams such as <c>AxesRenderer.Create</c> keep their loose
/// <c>(Axes, Rect, IRenderContext, Theme)</c> shape and unwrap a <see cref="RenderPass"/>
/// at the call boundary.
/// </remarks>
internal readonly record struct RenderPass(Rect PlotArea, IRenderContext Context, Theme Theme);
