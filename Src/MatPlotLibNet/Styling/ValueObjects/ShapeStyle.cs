// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Bundles the optional fill and stroke properties that travel together through every
/// shape-drawing operation on <c>IRenderContext</c> (polygons, circles, rectangles,
/// ellipses, and paths).
/// </summary>
/// <param name="Fill">The fill colour, or <see langword="null"/> for no fill.</param>
/// <param name="Stroke">The stroke colour, or <see langword="null"/> for no stroke.</param>
/// <param name="StrokeThickness">The stroke width in pixels.</param>
/// <remarks>
/// The <see cref="HasVisibleFill"/> and <see cref="HasVisibleStroke"/> properties are the
/// single source of truth for the paint-visibility guard. Historically each render-context
/// backend re-implemented these checks and drifted: the Skia backend skipped a stroke when
/// <c>StrokeThickness &lt;= 0</c>, while the SVG and MAUI backends did not. Centralizing the
/// guard here makes all backends agree on the most-defensive semantic.
/// </remarks>
public readonly record struct ShapeStyle(Color? Fill, Color? Stroke, double StrokeThickness)
{
    /// <summary>Gets a value indicating whether the fill should be painted (a fill colour is present).</summary>
    public bool HasVisibleFill => Fill.HasValue;

    /// <summary>
    /// Gets a value indicating whether the stroke should be painted: a stroke colour is present
    /// and the thickness is positive. A zero or negative thickness yields no visible stroke.
    /// </summary>
    public bool HasVisibleStroke => Stroke.HasValue && StrokeThickness > 0;
}
