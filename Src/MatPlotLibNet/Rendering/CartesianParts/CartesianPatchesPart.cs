// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Draws the patches of the axes (<see cref="Axes.Patches"/>), in the order they were added, one path each: its outline
/// in data units mapped through the transform the primary series are drawn with (<see cref="PathSegmentExtensions.ToPixels"/>:
/// a vertex a log axis cannot place pinned to where the axis begins, a vertex inside an axis break leaving the outline open there),
/// in the style the patch gives itself (<see cref="Patch.DrawnStyle"/>). A patch that cannot be drawn is skipped.</summary>
/// <remarks>
/// <para>The part stands in the spans' layer, after them and before the reference lines, so above the grid and under every series:
/// matplotlib draws a patch and an <c>axhspan</c> at zorder 1, a line at 2. matplotlib's default draws its grid over both (zorder
/// 1.5); here the grid stays under them, as under a span. The automatic range takes every patch in
/// (<c>AxesRangeExtensions.PatchContributions</c>), so a patch reaches past the plot only under a limit the caller set: then the
/// patches are clipped to the plot area by the rule the series are clipped by (<see cref="SeriesGroupExtensions"/>'
/// <c>BeginSeriesClip</c>), one clip around all of them.</para>
/// <para>Only a Cartesian axes draws patches, an inset included; a polar, ternary or 3-D axes draws none, as it draws no span,
/// reference line or annotation.</para>
/// </remarks>
internal sealed class CartesianPatchesPart : CartesianAxesPart
{
    /// <summary>Constructs the part for one render of <paramref name="axes"/>.</summary>
    /// <param name="axes">The axes whose patches are drawn.</param>
    /// <param name="plotArea">The plot area, which the patches are clipped to under a set limit.</param>
    /// <param name="ctx">The surface drawn on.</param>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="transform">The transform the primary series are drawn with.</param>
    public CartesianPatchesPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
        : base(axes, plotArea, ctx, theme, transform) { }

    /// <summary>A patch as it is drawn: its path in pixels and its style.</summary>
    private readonly record struct PlacedPatch(IReadOnlyList<PathSegment> Path, ShapeStyle Style);

    /// <inheritdoc />
    public override void Render()
    {
        var placed = new List<PlacedPatch>(Axes.Patches.Count);
        foreach (var patch in Axes.Patches)
        {
            var path = patch.Outline().ToPixels(Transform);
            if (path.Count > 0) placed.Add(new PlacedPatch(path, patch.DrawnStyle));
        }

        if (placed.Count == 0) return;

        bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
        foreach (var patch in placed)
            Ctx.DrawPath(patch.Path, patch.Style);
        if (clipped) Ctx.PopClip();
    }
}
