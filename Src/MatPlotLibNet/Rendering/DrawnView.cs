// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>What one render of an axes drew: where, and the range it showed. <see cref="ChartRenderer.RenderAxes"/> returns it,
/// so the pass that drew an inset hands its parent the inset's frame and the limits it really drew (its own limits, or the
/// range it scaled to its data, margins included), and the parent's zoom indicator spans exactly those: one rule, the one
/// that drew the inset, never worked out a second time.</summary>
/// <param name="PlotArea">The pixel rectangle the axes was drawn in: an inset's frame.</param>
/// <param name="Transform">The transform the primary series were drawn with (<see cref="AxesRenderer.DrawnTransform"/>): the
/// range the axes showed, in data units; <see langword="null"/> for axes that draw no Cartesian range (polar, 3-D,
/// ternary).</param>
internal readonly record struct DrawnView(Rect PlotArea, DataTransform? Transform);
