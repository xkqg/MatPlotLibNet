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
    /// <summary>A shape that is filled and has no outline: <paramref name="fill"/> over the shape, no stroke colour and a stroke of no
    /// width. The one spelling of "this mark has no outline", so a mark does not restate it. A hatch or a gradient is laid over it
    /// with a <c>with</c> expression, as it is over any shape.</summary>
    /// <param name="fill">The colour of the fill.</param>
    /// <returns>The fill-only shape.</returns>
    public static ShapeStyle Filled(Color fill) => Edged(fill, null, 0);

    /// <summary>A shape that is outlined and has no fill: <paramref name="stroke"/> at <paramref name="thickness"/> over nothing.</summary>
    /// <param name="stroke">The colour of the outline.</param>
    /// <param name="thickness">The width of the outline, in pixels.</param>
    /// <returns>The outline-only shape.</returns>
    public static ShapeStyle Outlined(Color stroke, double thickness) => Edged(null, stroke, thickness);

    /// <summary>The one place a shape is put together: <paramref name="fill"/> edged by <paramref name="edge"/> at <paramref name="width"/>.
    /// Either may be absent where its source names none (a patch edge of no opacity, a marker made of strokes, a map with no fill), and the
    /// shape then has no fill or no outline. Every source of a fill and an edge hands its values here, so no site constructs a shape.</summary>
    /// <param name="fill">The colour of the fill; <see langword="null"/> for none.</param>
    /// <param name="edge">The colour of the outline; <see langword="null"/> for none.</param>
    /// <param name="width">The width of the outline, in pixels.</param>
    /// <returns>The shape.</returns>
    internal static ShapeStyle Edged(Color? fill, Color? edge, double width) => new ShapeStyle(fill, edge, width);

    /// <summary>A shape as <see cref="Edged(Color?, Color?, double)"/> makes it, its corners joined by <paramref name="join"/> (the join its
    /// theme gives its artist, asked once for a loop of faces).</summary>
    /// <param name="fill">The colour of the fill; <see langword="null"/> for none.</param>
    /// <param name="edge">The colour of the outline; <see langword="null"/> for none.</param>
    /// <param name="width">The width of the outline, in pixels.</param>
    /// <param name="join">How the outline's segments meet at a corner.</param>
    /// <returns>The shape.</returns>
    internal static ShapeStyle Edged(Color? fill, Color? edge, double width, JoinStyle join) => new ShapeStyle(fill, edge, width) { StrokeJoin = join };

    /// <summary>The line this shape's outline is drawn with where a surface draws the outline as a line: the stroke colour, the width,
    /// the dash pattern and the join, read once here instead of by every surface.</summary>
    /// <returns>The outline as a stroke.</returns>
    /// <exception cref="InvalidOperationException">The shape has no stroke colour: it has no outline to draw.</exception>
    public StrokeStyle OutlineStroke() => StrokeStyle.Of(Stroke!.Value, StrokeThickness, StrokeLineStyle, StrokeJoin);

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
    /// share it), the Skia backend paints a shader (a vector shading in a PDF), and the MAUI backend paints a linear gradient
    /// paint whose ends are fractions of the shape's box.</remarks>
    public LinearGradient? Gradient { get; init; }

    /// <summary>Gets a value indicating whether a gradient must be painted: one is set AND there is a fill, which is the
    /// colour of the shape wherever a gradient cannot be painted.</summary>
    public bool HasVisibleGradient => Gradient.HasValue && Fill.HasValue;

    /// <summary>Gets a value indicating whether a hatch pattern must be painted: a pattern is selected AND there is
    /// a fill for it to sit on. A hatch without a fill has nothing to hatch.</summary>
    public bool HasVisibleHatch => Hatch != HatchPattern.None && Fill.HasValue;

    /// <summary>The dash pattern of the stroke: <see cref="LineStyle.Solid"/> for a continuous outline (the default), a dashed,
    /// dotted or dash-dot pattern, or <see cref="LineStyle.None"/> for no outline at all. It is the pattern a line of that
    /// style is drawn with, in every backend, so an outline and a line of one style look alike.</summary>
    /// <remarks>An <c>init</c> property for the reason <see cref="Hatch"/> is one: a positional addition would break every
    /// construction site of this record struct. A shape built without it strokes solid, as every shape did before the property
    /// existed. The pattern belongs to the stroke, so it has no effect on a shape without one.</remarks>
    /// <example>
    /// A dashed outline around a light fill:
    /// <code>
    /// var style = new ShapeStyle(Colors.LightGray, Colors.Black, 1.5) { StrokeLineStyle = LineStyle.Dashed };
    /// </code>
    /// </example>
    public LineStyle StrokeLineStyle { get; init; }

    /// <summary>How two segments of the stroke meet at a corner: <see cref="JoinStyle.Miter"/> (the default), as every shape's stroke was
    /// drawn before the join was a value. A shape that stands for a matplotlib collection (a scatter marker, a filled band, a barb)
    /// takes the join of its theme, which its producer asks once by its artist; a patch keeps the mitre
    /// matplotlib gives it. The pattern belongs to the stroke, so it has no effect
    /// on a shape without one.</summary>
    /// <remarks>An <c>init</c> property for the reason <see cref="Hatch"/> is one.</remarks>
    public JoinStyle StrokeJoin { get; init; }

    /// <summary>Whether the edge of the fill is drawn with no antialiasing, as matplotlib draws the bands of a filled contour
    /// (<c>antialiased=False</c>): a pixel the edge crosses takes one of the two colours it divides, never a blend of them.
    /// <see langword="false"/> (the default) antialiases the edge. The SVG backend writes <c>shape-rendering="crispEdges"</c> and the
    /// raster and native backends switch the fill's antialiasing off; a backend that cannot draws the shape as any other. The
    /// stroke is not affected.</summary>
    /// <remarks>An <c>init</c> property for the reason <see cref="Hatch"/> is one: a positional addition would break every
    /// construction site of this record struct.</remarks>
    public bool Aliased { get; init; }

    /// <summary>
    /// Gets a value indicating whether the stroke should be painted: a stroke colour is present,
    /// the thickness is positive and the line style is not <see cref="LineStyle.None"/>. A zero or negative
    /// thickness yields no visible stroke, and a style of none is no stroke, as it is for a
    /// <see cref="StrokeStyle"/>.
    /// </summary>
    public bool HasVisibleStroke => Stroke.HasValue && StrokeThickness > 0 && StrokeLineStyle != LineStyle.None;
}
