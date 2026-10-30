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

    /// <summary>The fill pattern painted over <see cref="Fill"/>, or <see cref="HatchPattern.None"/> for a plain
    /// fill (the default). A pattern is a fill property, so it travels with the fill through every shape-drawing
    /// operation and every backend resolves it on the one path they already share.</summary>
    /// <remarks>Declared as an <c>init</c> property rather than a fourth positional parameter: a positional
    /// addition would break every construction site of this record struct across the renderers.</remarks>
    public HatchPattern Hatch { get; init; }

    /// <summary>The colour of the hatch strokes. When <see langword="null"/> the backend falls back to a
    /// contrasting shade of <see cref="Fill"/>, so a caller never has to supply two colours to get a visible hatch.</summary>
    public Color? HatchColor { get; init; }

    /// <summary>A blend from one colour to another that replaces the flat <see cref="Fill"/>, or <see langword="null"/>
    /// for a flat fill (the default). Like a hatch it is a fill property, so it travels with the fill through every
    /// shape-drawing operation and each backend resolves it on the one path it already shares. <see cref="Fill"/> stays the
    /// colour of the shape wherever a backend cannot paint a gradient.</summary>
    /// <remarks>An <c>init</c> property for the reason <see cref="Hatch"/> is one. A hatch together with a gradient is laid
    /// over the gradient. The SVG backend writes a <c>linearGradient</c> in user space (shapes that carry the same gradient
    /// share it), the Skia backend paints a shader (a vector shading in a PDF), and the MAUI backend, which this repository
    /// cannot build or test, paints the flat <see cref="Fill"/>.</remarks>
    public LinearGradient? Gradient { get; init; }

    /// <summary>Gets a value indicating whether a gradient must be painted: one is set AND there is a fill, which is the
    /// colour of the shape wherever a gradient cannot be painted.</summary>
    public bool HasVisibleGradient => Gradient.HasValue && Fill.HasValue;

    /// <summary>Gets a value indicating whether a hatch pattern must be painted: a pattern is selected AND there is
    /// a fill for it to sit on. A hatch without a fill has nothing to hatch.</summary>
    public bool HasVisibleHatch => Hatch != HatchPattern.None && Fill.HasValue;

    /// <summary>
    /// Gets a value indicating whether the stroke should be painted: a stroke colour is present
    /// and the thickness is positive. A zero or negative thickness yields no visible stroke.
    /// </summary>
    public bool HasVisibleStroke => Stroke.HasValue && StrokeThickness > 0;
}
