// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Numerics;
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
public abstract class Patch : IChangeTracked, IEdgedPatch, IHatched
{
    /// <summary>The width a patch given none reads (<see cref="EdgeWidth"/>) and is edged at under the library's own themes, in
    /// pixels.</summary>
    private const double RestingEdgeWidth = 1.0;

    /// <summary>The edge colour a patch given none reads (<see cref="EdgeColor"/>): none, as matplotlib draws no edge on a shape that
    /// encloses an area.</summary>
    private static readonly Color RestingEdgeColor = Colors.Transparent;

    /// <summary>The opacity a patch given none reads (<see cref="Alpha"/>): opaque, so each colour keeps its own.</summary>
    private const double RestingAlpha = 1.0;

    private long _stamp;

    /// <summary>Stores <paramref name="value"/> and marks this patch changed, unless it already holds an equal value. Every
    /// setter of every kind of patch writes through it.</summary>
    private protected void Change<T>(ref T slot, T value) => ChangeClock.Change(ref slot, value, ref _stamp);

    long IChangeTracked.LatestChange => _stamp;

    /// <summary>Only the library derives a patch: a kind of patch is drawn, written and read by the library, so it is one of
    /// the leaves it ships.</summary>
    private protected Patch() { }

    /// <summary>The colour of the outline. None (<see cref="RestingEdgeColor"/>) by default for a shape that encloses an area,
    /// as matplotlib draws no edge on one; black for an <see cref="Arrow"/>. Under the classic style a shape that was given no colour
    /// is edged in black, as its <c>patch.force_edgecolor</c> forces; one set to <see cref="Colors.Transparent"/> stays without an
    /// edge, as matplotlib's <c>edgecolor='none'</c> does.</summary>
    public Color EdgeColor
    {
        get => GivenEdgeColor ?? RestingEdgeColor;
        set => GivenEdgeColor = (Color?)value;
    }

    /// <summary>The edge colour the caller gave, <see langword="null"/> while none was. The JSON writes it, so a colour set to none is
    /// not read back as a colour never named.</summary>
    internal Color? GivenEdgeColor { get; private set => Change(ref field, value); }

    /// <summary>The edge colour and width the caller gave, each none until it was set (<see cref="EdgeColor"/>, <see cref="EdgeWidth"/>),
    /// so the theme may force a colour and give its <c>patch.linewidth</c>, and the opacity given (<see cref="Alpha"/>), which the edge
    /// and the hatch take under every theme. The one thing <see cref="Theme.PatchEdgeOf"/> reads.</summary>
    GivenPatchEdge IEdgedPatch.GivenEdge => new(GivenEdgeColor, GivenEdgeWidth, GivenAlpha);

    /// <summary>Under the library's own themes a patch is edged as it was set: its colour at the opacity given, its width (a pixel when
    /// none was given).</summary>
    PatchEdge IEdgedPatch.LibraryEdge(Color? fill, Color? themeEdge) => new(WithTheOpacityGiven(EdgeColor), EdgeWidth);

    /// <summary>The width of the outline, in pixels; 0 draws no outline. Until it is set the theme decides, as matplotlib's
    /// <c>patch.linewidth</c> does: 1 point (1.389 pixels) under the matplotlib themes, <see cref="RestingEdgeWidth"/> pixel under
    /// the library's own, and this reads <see cref="RestingEdgeWidth"/>. A width set is drawn under every theme.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is negative, NaN or infinite.</exception>
    public double EdgeWidth
    {
        get => GivenEdgeWidth ?? RestingEdgeWidth;
        set => GivenEdgeWidth = value.ThrowIfNotFinite(nameof(EdgeWidth)).ThrowIfBelow(0, nameof(EdgeWidth));
    }

    /// <summary>The edge width the caller gave, <see langword="null"/> while none was. The JSON writes it, so a width given that equals
    /// the one a new patch reads is not read back as a width never named.</summary>
    internal double? GivenEdgeWidth { get; private set => Change(ref field, value); }

    /// <summary>The dash pattern of the outline. Solid by default; <see cref="LineStyle.None"/> draws no outline.</summary>
    public LineStyle EdgeLineStyle { get; set => Change(ref field, value); } = LineStyle.Solid;

    /// <summary>The opacity of the whole patch, from 0 (clear) to 1 (opaque). Once set it replaces the opacity of the face, the edge
    /// and the hatch alike, as matplotlib's <c>alpha</c> does: a translucent colour becomes as opaque as the value says. Until it is
    /// set each colour keeps its own opacity, and this reads <see cref="RestingAlpha"/>. A colour with no opacity at all (<see cref="Colors.Transparent"/>, none)
    /// stays none.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value set is below 0, above 1 or not a number.</exception>
    public double Alpha
    {
        get => GivenAlpha ?? RestingAlpha;
        set => GivenAlpha = value.ThrowIfNotOpacity(nameof(Alpha));
    }

    /// <summary>The opacity the caller gave, <see langword="null"/> while none was given. The JSON writes it, and the colours of the
    /// patch read it: with none given each colour keeps its own opacity.</summary>
    internal double? GivenAlpha { get; private set => Change(ref field, value); }

    /// <summary>The colour <paramref name="color"/> is drawn in: its own opacity, or the <see cref="Alpha"/> given in its place. A
    /// colour of none stays none. The one rule the face, the edge and the hatch share.</summary>
    /// <param name="color">The colour as the caller set it.</param>
    private protected Color WithTheOpacityGiven(Color color) => color.WithOpacityGiven(GivenAlpha);

    /// <summary>The name a legend lists the patch under, as matplotlib's legend does: a row with a rectangle in the patch's own style,
    /// among the rows of the series in the order they were added, which a click toggles as it toggles a series
    /// (<see cref="Visible"/>). <see langword="null"/> (the default), an empty label or one that starts with an underscore
    /// (<c>"_hidden"</c>) gives it no row, matplotlib's rule. Unlike the label of a span, it is not drawn on the patch itself.</summary>
    public string? Label { get; set => Change(ref field, value); }

    /// <summary>Whether the label gives the patch a row in a legend: matplotlib's rule (<c>_get_legend_handles_labels</c>), the
    /// label is neither missing nor empty and does not start with an underscore.</summary>
    internal bool HasLegendRow => Label is { Length: > 0 } label && label[0] != '_';

    /// <summary>Whether the patch is drawn, as matplotlib's <c>set_visible</c> says. <see langword="true"/> by default; a click on the
    /// patch's legend row turns it off and on, and a hidden patch keeps its row and its place in the automatic range.</summary>
    public bool Visible { get; set => Change(ref field, value); } = true;

    /// <summary>On a 3-D axes, how far along <see cref="ZDir"/> the plane of the patch stands, in data units: matplotlib's <c>z</c> of
    /// <c>pathpatch_2d_to_3d</c>. 0 by default. Any other axes ignores it.</summary>
    public double Z { get; set => Change(ref field, value); }

    /// <summary>On a 3-D axes, the axis the plane of the patch stands across: matplotlib's <c>zdir</c> of <c>pathpatch_2d_to_3d</c>.
    /// <see cref="ZDirection.Z"/> (the default) lays the patch in the X-Y plane at z = <see cref="Z"/>; <see cref="ZDirection.X"/>
    /// makes its x and y the 3-D y and z at x = <see cref="Z"/>; <see cref="ZDirection.Y"/> its x and z at y = <see cref="Z"/>. Any
    /// other axes ignores it.</summary>
    public ZDirection ZDir { get; set => Change(ref field, value); }

    /// <summary>Where the patch is drawn among the series and the other patches of its axes: one with a higher value is drawn over
    /// one with a lower value, as matplotlib's <c>zorder</c> orders them. By default matplotlib's zorder of a patch: under the
    /// matplotlib themes over an image (0), under the grid of the default style (1.5) and under a line (2). Under the library's own
    /// themes a patch given no z-order is drawn under every series, after the spans; one given a z-order stands among the series by
    /// their <see cref="Series.ISeries.ZOrder"/>.</summary>
    public int ZOrder
    {
        get => GivenZOrder ?? (int)Series.MatplotlibArtist.Patch.ZOrder;
        set => GivenZOrder = value;
    }

    /// <summary>The z-order the caller gave, <see langword="null"/> while none was given. The JSON writes it.</summary>
    internal int? GivenZOrder { get; private set => Change(ref field, value); }

    /// <summary>The pattern drawn over the face. <see cref="HatchPattern.None"/> (the default) draws none.</summary>
    public HatchPattern Hatch { get; set => Change(ref field, value); }

    /// <summary>The colour of the hatch lines. <see langword="null"/> (the default) draws them as every hatch of the theme is drawn:
    /// in the <see cref="EdgeColor"/>, and in black when the patch has no edge, under v2, as matplotlib's hatch colour <c>'edge'</c>
    /// does; in black under the classic theme, whose <c>hatch.color</c> is <c>'k'</c>; under the library's own themes in the edge
    /// colour given, else in the colour that contrasts with the face, black over a face of none. A colour given takes the
    /// <see cref="Alpha"/> given.</summary>
    public Color? HatchColor { get; set => Change(ref field, value); }

    /// <summary>The colour the face is drawn in under <paramref name="theme"/>: the face with the opacity given in place of its own
    /// (the first colour of the theme's cycle for a face the patch was given none), none for a patch without a face. The one rule the
    /// drawing and the legend's swatch read.</summary>
    /// <param name="theme">The theme the patch is drawn under.</param>
    /// <returns>The colour of the face, with its opacity.</returns>
    internal abstract Color DrawnFaceColorUnder(Theme theme);

    /// <summary>The style every backend draws the patch with under <paramref name="theme"/>, made where every patch-like shape's style is
    /// made (<see cref="ResolvedPatchEdge.StyleOf(Color, PatchEdge, HatchFill)"/>): the face (<see cref="DrawnFaceColorUnder"/>), the edge
    /// the theme's rule gives (<see cref="Theme.PatchEdgesOf"/>: the colour given, else matplotlib's forced one under the classic style;
    /// the width given, else <c>patch.linewidth</c>; under the library's own themes the edge as it was set) in
    /// <see cref="EdgeLineStyle"/>, and the hatch in the colour the one rule gives (<see cref="PatchLook.HatchOver"/>). Each colour
    /// carries its opacity in its own alpha: the one it was given, or the one <see cref="Alpha"/> gave in its place, as matplotlib
    /// applies a patch's alpha to each of its colours. A face of none is no fill and an edge of none is no stroke, except that a hatched
    /// patch keeps its face of none as a clear fill.</summary>
    /// <param name="theme">The theme the patch is drawn under.</param>
    /// <returns>The style the patch is drawn with.</returns>
    internal ShapeStyle DrawnStyleUnder(Theme theme)
    {
        var edges = theme.PatchEdgesOf(this);
        return edges.StyleOf(DrawnFaceColorUnder(theme), edges.Of(), this.Hatching) with { StrokeLineStyle = EdgeLineStyle };
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
