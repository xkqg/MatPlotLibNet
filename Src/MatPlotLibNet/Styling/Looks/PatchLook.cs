// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The edge, opacity and hatch of a patch, a fill and a 3-D surface where their author names nothing: matplotlib's
/// <c>patch.*</c> settings and how it draws a collection, under the matplotlib themes. The one owner of those rules.</summary>
internal sealed record PatchLook : IThemeKeys<PatchLook>
{
    /// <summary>The library's own patches: no rule for a patch edge (each kind keeps its own), surfaces outlined at rest.</summary>
    internal static PatchLook Library { get; } = new();

    /// <inheritdoc />
    /// <remarks>A look that fills like a collection says whether it forces an edge; the library's look writes neither.</remarks>
    public void WriteKeys(ThemeDto keys)
    {
        keys.PatchEdgeColor = EdgeColor;
        keys.SurfaceAlpha = Alpha;
        keys.SurfaceOutlined = LikeCollections ? ForceEdge : null;
        keys.SurfaceEdgeWidth = EdgeWidth;
        keys.HatchFollowsEdge = (Hatch is { FollowsEdge: true }).OrNullAtRest(false);
        keys.HatchFallback = Hatch?.Fallback;
    }

    /// <inheritdoc />
    public static PatchLook ReadKeys(ThemeDto keys) => new()
    {
        LikeCollections = keys.SurfaceOutlined is not null,
        ForceEdge = keys.SurfaceOutlined ?? Library.ForceEdge,
        EdgeColor = keys.PatchEdgeColor,
        EdgeWidth = keys.SurfaceEdgeWidth,
        Alpha = keys.SurfaceAlpha,
        Hatch = keys.HatchFallback is { } fallback ? new HatchColorRule(keys.HatchFollowsEdge ?? false, fallback) : null,
    };

    /// <summary>The alpha a 3-D surface reads, and is drawn at under a look that names none, when its author set none.</summary>
    internal const double RestingAlpha = 0.8;

    /// <summary>The edge of a 3-D face the library draws where no edge colour was given (a surface's, a voxel's): black at 80 of 255.</summary>
    internal static readonly Color RestingSurfaceEdgeColor = Colors.Black.WithAlpha(80);

    private const double RestingEdgeWidth = 0.5;

    /// <summary>Whether a fill that names no alpha, line or edge of its own is drawn as matplotlib draws a collection: opaque
    /// (<see cref="Alpha"/>), edged by <see cref="EdgeOver"/>, with no line of its own. Read by an area (<c>fill_between</c>), a
    /// contour, and every bar-like patch's edge.</summary>
    public bool LikeCollections { get; init; }

    /// <summary>Whether a patch and a surface are edged at rest: matplotlib's <c>patch.force_edgecolor</c>, off under the default
    /// style, on under the classic one. The library outlines a surface at rest.</summary>
    public bool ForceEdge { get; init; } = true;

    /// <summary>The colour of that edge: matplotlib's <c>patch.edgecolor</c>, black in both its styles, the one value
    /// <see cref="Theme.PatchEdgeColor"/> reads and sets. Unset: black at alpha 80 on a surface.</summary>
    public Color? EdgeColor { get; init; }

    /// <summary>How wide that edge is, in pixels: matplotlib's <c>patch.linewidth</c>, 1 pt in both its styles. Unset: <see cref="RestingEdgeWidth"/> px.</summary>
    public double? EdgeWidth { get; init; }

    /// <summary>How opaque a 3-D surface (and a fill drawn as a collection) is when its author set no alpha: 1 under the matplotlib
    /// themes, whose <c>plot_surface</c> gives its faces none. Unset: <see cref="RestingAlpha"/>. Refused outside 0 to 1
    /// (<see cref="UnitIntervalExtensions.ThrowIfNotOpacity(double, string)"/>), as every opacity is, so a theme document that carries one is refused when it
    /// is read.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double? Alpha { get; init => field = value.ThrowIfNotOpacity(nameof(Alpha)); }

    /// <summary>The colour a hatch is drawn in where its author names none (matplotlib's <c>hatch.color</c>): the edge colour of its
    /// shape under the default style, black under the classic one. Unset: the backend draws the hatch in the colour that reads best
    /// against its fill.</summary>
    public HatchColorRule? Hatch { get; init; }

    /// <summary>The width a patch's outline drawn as a line is drawn at (a step histogram's, which matplotlib draws as an unfilled
    /// <c>Polygon</c>): the rule of its kind over <see cref="EdgeWidth"/>, the width this look gives a patch's edge, with no width given
    /// and none cycled. Under the library's own look, which names no edge width, the kind's own.</summary>
    /// <param name="rule">The rule of the kind.</param>
    /// <returns>The width, in pixels.</returns>
    internal double OutlineWidthOf(LineWidthRule rule) => rule.WidthOf(null, null, EdgeWidth);

    /// <summary>The outline of a 3-D surface: drawn when its author turned the wireframe on, not when they turned it off, and
    /// otherwise when they named an edge colour or this look edges at rest (<see cref="ForceEdge"/>); in the colour named, else
    /// <see cref="EdgeColor"/>, else black at alpha 80; as wide as <see cref="EdgeWidth"/>, else <see cref="RestingEdgeWidth"/> px.</summary>
    /// <param name="shown">Whether its author turned the wireframe on or off, <see langword="null"/> when neither.</param>
    /// <param name="given">The edge colour its author named, <see langword="null"/> when none.</param>
    /// <returns>The stroke, or <see langword="null"/> for no outline.</returns>
    internal StrokeStyle? SurfaceEdgeOver(bool? shown, Color? given)
    {
        if (!(shown ?? (given is not null || ForceEdge))) return null;
        return StrokeStyle.Solid(given ?? EdgeColor ?? RestingSurfaceEdgeColor, EdgeWidth ?? RestingEdgeWidth);
    }

    /// <summary>The edge of a patch under a look that draws patches as matplotlib does (<see cref="LikeCollections"/>):
    /// matplotlib's <c>Patch._set_edgecolor</c>, the colour given, else <see cref="EdgeColor"/> when <see cref="ForceEdge"/>, else
    /// none, at the opacity given to the whole patch (<c>to_rgba(color, alpha)</c>); as wide as given, else <see cref="EdgeWidth"/>. A
    /// width of 0 draws none. The one owner of that rule for every bar-like patch, a fill and their legend handles.</summary>
    /// <param name="given">The edge colour and width the author gave and the opacity of the patch
    /// (<see cref="IEdgedPatch.GivenEdge"/>), each <see langword="null"/> when none.</param>
    /// <returns>The edge; <see langword="null"/> under the library's look, where the series' own rule decides.</returns>
    internal PatchEdge? EdgeOver(GivenPatchEdge given)
    {
        if (!LikeCollections) return null;
        var color = given.Color ?? (ForceEdge ? EdgeColor : null);
        double width = given.Width ?? EdgeWidth ?? RestingEdgeWidth;
        return color is { } c && width > 0 ? new PatchEdge(c.WithOpacityGiven(given.Alpha), width) : PatchEdge.None;
    }

    /// <summary>The colour a hatch is drawn in on <paramref name="ground"/>: the colour its author named, else this look's rule
    /// (<see cref="Hatch"/>, matplotlib's <c>hatch.color</c>), else the library's (<see cref="LibraryHatchColorOver"/>); at the opacity
    /// given to the shape, as matplotlib's <c>alpha</c> reaches the hatch colour too (<c>to_rgba(color, alpha)</c>).</summary>
    /// <param name="given">The hatch colour the author named, <see langword="null"/> when none.</param>
    /// <param name="ground">What the hatch is laid on.</param>
    /// <returns>The colour, or <see langword="null"/> to leave it to the backend.</returns>
    private Color? HatchColorOver(Color? given, HatchGround ground) =>
        (given ?? (Hatch is { } rule
            ? rule.FollowsEdge && ground.Edge is { } drawn && !drawn.IsNone ? drawn : rule.Fallback
            : LibraryHatchColorOver(ground)))?.WithOpacityGiven(ground.Alpha);

    /// <summary>The colour of a hatch its author named none for under the library's own look: the edge its author gave, else none
    /// over a fill that can be seen (the backend draws it in the colour that reads best against the fill), else black, matplotlib's
    /// <c>patch.edgecolor</c>, which reads over a fill no one can see.</summary>
    private static Color? LibraryHatchColorOver(HatchGround ground) =>
        ground.GivenEdge is { } given && !given.IsNone ? given : ground.Fill is { } fill && !fill.IsNone ? null : Colors.Black;

    /// <summary>The hatch a shape is drawn with where its author gave <paramref name="given"/>: the same pattern, its colour the one named,
    /// else this look's rule (<see cref="Hatch"/>), else the library's, both over what the hatch is laid on. The one place the colour of
    /// a hatch of any patch-like shape is decided, a bar's and a patch's alike, as matplotlib has one rule for every patch.</summary>
    /// <param name="given">The pattern and the colour the author named.</param>
    /// <param name="ground">What the hatch is laid on: the edge drawn, the edge given, the fill and the opacity given.</param>
    /// <returns>The hatch to draw.</returns>
    internal HatchFill HatchOver(HatchFill given, HatchGround ground) => given with { Color = HatchColorOver(given.Color, ground) };

    /// <summary>Whether a filled contour draws its lines over the fills: the choice given, else unless this look draws patches as collections.</summary>
    /// <param name="given">The value its author gave, <see langword="null"/> when none.</param>
    /// <returns>The value drawn.</returns>
    internal bool ContourLinesOver(bool? given) => given ?? !LikeCollections;
}
