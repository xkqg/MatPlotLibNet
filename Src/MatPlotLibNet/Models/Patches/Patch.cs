// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Patches;

/// <summary>A shape drawn on an axes in data coordinates, as matplotlib's patches are: made by the caller, styled through its
/// own properties and added with <c>AddPatch</c> (<see cref="Axes.AddPatch{TPatch}"/>, <c>AxesBuilder.AddPatch</c>,
/// <c>FigureBuilder.AddPatch</c>, matplotlib's <c>add_patch</c>). Every patch has an edge; the shapes that enclose an area
/// derive from <see cref="FilledPatch"/>, which adds the face, and an <see cref="Arc"/>, the one patch matplotlib refuses to
/// fill, has none.</summary>
/// <remarks>
/// <para>The style a patch rests at is matplotlib's patch style: an edge 1 unit wide that is solid, opaque, without a hatch.
/// A colour of none is <see cref="Colors.Transparent"/>, as matplotlib's <c>'none'</c> is the colour (0, 0, 0, 0).</para>
/// <para>The geometry refuses no number, as matplotlib's does not; a patch whose outline would have a vertex that is not a
/// finite number (a NaN or an infinity anywhere in it) has no outline and draws nothing. The style refuses what it cannot
/// draw: an <see cref="Alpha"/> outside 0 to 1 and an <see cref="EdgeWidth"/> below 0, each as the value is set, so every
/// property can be set on its own and in any order; the rules between properties apply when the outline is drawn.</para>
/// </remarks>
/// <example>
/// A turned rectangle, a triangle and an arrow on one subplot:
/// <code>
/// Plt.Create().AddSubPlot(1, 1, 1, ax => ax
///     .AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, FaceColor = Colors.Tab10Orange })
///     .AddPatch(new Polygon([new(4, 0), new(6, 0), new(5, 2)]) { EdgeColor = Colors.Black, Hatch = HatchPattern.Cross })
///     .AddPatch(new Arrow(new(0, 3), 4, 0) { Width = 0.1 }));
/// </code>
/// </example>
public abstract class Patch
{
    private double _edgeWidth = 1.0;
    private double _alpha = 1.0;

    /// <summary>Only the library derives a patch: a kind of patch is drawn, written and read by the library, so it is one of
    /// the leaves it ships.</summary>
    private protected Patch() { }

    /// <summary>The colour of the outline. <see cref="Colors.Transparent"/> (none) by default for a shape that encloses an area,
    /// as matplotlib draws no edge on one; black for an <see cref="Arrow"/>.</summary>
    public Color EdgeColor { get; set; } = Colors.Transparent;

    /// <summary>The width of the outline, in pixels. 1 by default, matplotlib's <c>patch.linewidth</c>; 0 draws no outline.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is negative, NaN or infinite.</exception>
    public double EdgeWidth
    {
        get => _edgeWidth;
        set => _edgeWidth = double.IsFinite(value) && value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The edge width of a patch (EdgeWidth) must be a finite number of pixels, 0 or more.");
    }

    /// <summary>The dash pattern of the outline. Solid by default; <see cref="LineStyle.None"/> draws no outline.</summary>
    public LineStyle EdgeLineStyle { get; set; } = LineStyle.Solid;

    /// <summary>The opacity of the whole patch, from 0 (clear) to 1 (as its colours are, the default). It scales the opacity of the
    /// face, the edge and the hatch alike, so a colour that is already translucent becomes more so and a colour of none stays
    /// none. matplotlib's alpha replaces the opacity of each colour instead; the two draw the same for opaque colours.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below 0, above 1 or not a number.</exception>
    public double Alpha
    {
        get => _alpha;
        set => _alpha = value is >= 0 and <= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The opacity of a patch (Alpha) must be between 0 and 1.");
    }

    /// <summary>The name a legend lists the patch under, as matplotlib's legend does: a row with a rectangle in the patch's own style,
    /// after the rows of the series, which a click does not toggle. <see langword="null"/> (the default), an empty label or one that
    /// starts with an underscore (<c>"_hidden"</c>) gives it no row, matplotlib's rule. Unlike the label of a span, it is not drawn on
    /// the patch itself.</summary>
    /// <remarks>Only an axes that draws its patches lists them: a Cartesian axes and an inset do, a polar, 3-D or ternary axes does
    /// not. matplotlib lists the series and the patches in the order they were added; here the series come first.</remarks>
    public string? Label { get; set; }

    /// <summary>The pattern drawn over the face. <see cref="HatchPattern.None"/> (the default) draws none.</summary>
    public HatchPattern Hatch { get; set; }

    /// <summary>The colour of the hatch lines. <see langword="null"/> (the default) draws them in the <see cref="EdgeColor"/>, and in
    /// black when the patch has no edge, as matplotlib's hatch colour <c>'edge'</c> does.</summary>
    public Color? HatchColor { get; set; }

    /// <summary>The colour the face is drawn in: the face with <see cref="Alpha"/> applied, none for a patch without a face. The
    /// one rule the drawing and the legend's swatch read.</summary>
    internal abstract Color DrawnFaceColor { get; }

    /// <summary>The colour the outline is drawn in: <see cref="EdgeColor"/> with <see cref="Alpha"/> applied.</summary>
    internal Color DrawnEdgeColor => EdgeColor.ScaleAlpha(Alpha);

    /// <summary>The colour the hatch is drawn in: <see cref="HatchColor"/>, else the edge colour, else black when there is no edge,
    /// with <see cref="Alpha"/> applied.</summary>
    internal Color DrawnHatchColor => (HatchColor ?? (EdgeColor.A == 0 ? Colors.Black : EdgeColor)).ScaleAlpha(Alpha);

    /// <summary>The style every backend draws the patch with: the face (<see cref="DrawnFaceColor"/>), the edge
    /// (<see cref="DrawnEdgeColor"/>) at <see cref="EdgeWidth"/> in <see cref="EdgeLineStyle"/>, and the hatch in
    /// <see cref="DrawnHatchColor"/>. Each colour carries the opacity in its own alpha, as matplotlib applies a patch's alpha to
    /// each of its colours. A face of none is no fill and an edge of none is no stroke, except that a hatched patch keeps its face
    /// of none as a clear fill: a hatch is laid over a fill, and matplotlib draws the hatch of a patch it does not fill.</summary>
    internal ShapeStyle DrawnStyle
    {
        get
        {
            var face = DrawnFaceColor;
            var edge = DrawnEdgeColor;
            return new ShapeStyle(face.A > 0 || Hatch != HatchPattern.None ? face : null, edge.A > 0 ? edge : null, EdgeWidth)
            {
                StrokeLineStyle = EdgeLineStyle,
                Hatch = Hatch,
                HatchColor = DrawnHatchColor,
            };
        }
    }

    /// <summary>The outline of the patch in data units: one segment per step of the path matplotlib builds for the same patch (a
    /// move, a line, a cubic Bézier curve, which is three of matplotlib's vertices, and a close where the shape is closed), every
    /// point a data value, a curve's control points included. Empty when it cannot be drawn. It needs no transform, so the
    /// automatic range and the drawing read the same outline.</summary>
    internal abstract IReadOnlyList<PathSegment> Outline();

    /// <summary>The description of the patch the figure JSON carries: its kind, the values it was made with, and every setting that
    /// differs from the one a new patch of its kind has. A kind of patch cannot be added without one, so every patch can be
    /// written.</summary>
    internal abstract PatchDto Describe();

    /// <summary>The outline through <paramref name="vertices"/> in order, closed back to the first when <paramref name="closed"/>:
    /// empty when a vertex is not a finite number.</summary>
    /// <param name="vertices">The vertices, at least one.</param>
    /// <param name="closed">Whether the outline closes back to its first vertex.</param>
    private protected static IReadOnlyList<PathSegment> OutlineThrough(IReadOnlyList<DataPoint> vertices, bool closed)
    {
        if (vertices.Any(vertex => !double.IsFinite(vertex.DataX) || !double.IsFinite(vertex.DataY)))
        {
            return [];
        }

        var segments = new List<PathSegment>(vertices.Count + 1) { new MoveToSegment(new Point(vertices[0].DataX, vertices[0].DataY)) };
        for (int i = 1; i < vertices.Count; i++)
        {
            segments.Add(new LineToSegment(new Point(vertices[i].DataX, vertices[i].DataY)));
        }

        if (closed)
        {
            segments.Add(new CloseSegment());
        }

        return segments;
    }
}
