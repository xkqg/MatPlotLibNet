// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Draws the patches of the axes (<see cref="Axes.Patches"/>), in the order they were added, one path each: its outline
/// in data units mapped through the transform the primary series are drawn with (<see cref="PathSegmentExtensions"/>' <c>ToPixels</c>:
/// an outline through a vertex a log axis cannot place cut where it leaves the plot, a vertex inside an axis break leaving the
/// outline open there),
/// in the style the patch gives itself, by the one drawing every axes draws its patches with (<see cref="PatchDrawing"/>: a hidden
/// patch skipped, the toggle group of a patch with a legend row in an interactive picture). A patch that cannot be drawn is skipped.</summary>
/// <remarks>
/// <para>Each patch stands at its own place in the axes' z-order (<see cref="CartesianAxesRenderer.DataLayers"/>, <see cref="Patch.ZOrder"/>):
/// under the matplotlib themes at matplotlib's 1, with the spans and under the grid of the default style (1.5) and every line (2);
/// under the library's own themes, given none, after the spans and under every series. Patches that follow one another in that
/// order are drawn by one call (<see cref="Render(ReadOnlySpan{LayerStep})"/>). The automatic range takes every patch in
/// (<c>AxesRangeExtensions.PatchContributions</c>) under the library's own themes, so a patch reaches past the plot only under a limit
/// the caller set; under the matplotlib themes <c>add_patch</c> asks for no range, so a patch can reach past the plot of an axes
/// that nothing else ranges. Where a patch can reach past the plot (<see cref="AxesRange.PatchesMayReachPastPlot"/>) the patches
/// are clipped to the plot area, one clip around the patches drawn by one call.</para>
/// <para>A polar, ternary or 3-D axes draws its patches in its series pass instead (<see cref="AxesRenderer"/>), each through its
/// own placement of its data.</para>
/// </remarks>
internal sealed class CartesianPatchesPart : CartesianAxesPart
{
    /// <summary>Constructs the part for one render of the context's axes.</summary>
    /// <param name="context">What the render draws with: the axes whose patches are drawn, the plot area they are clipped to under a
    /// set limit, the surface, the theme and the transform the primary series are drawn with.</param>
    /// <param name="clipped">Whether the patches are clipped to the plot area: the answer the range of the draw gave
    /// (<see cref="AxesRange.PatchesMayReachPastPlot"/>), asked once with the range and not again per layer.</param>
    internal CartesianPatchesPart(in CartesianPartContext context, bool clipped)
        : base(context) => _clipped = clipped;

    /// <summary>Whether the patches are clipped to the plot area.</summary>
    private readonly bool _clipped;

    /// <summary>A patch as it is drawn: its place in <see cref="Axes.Patches"/> and its path in pixels.</summary>
    private readonly record struct PlacedPatch(int Index, IReadOnlyList<PathSegment> Path);

    /// <inheritdoc />
    /// <remarks>Every patch, in the order they were added.</remarks>
    public override void Render()
    {
        var drawing = new PatchDrawing(Axes, Ctx, Theme);
        var placed = new List<PlacedPatch>(Axes.Patches.Count);
        for (int i = 0; i < Axes.Patches.Count; i++) Place(drawing, i, placed);
        Draw(drawing, placed);
    }

    /// <summary>Draws the patches of <paramref name="run"/>, steps of the axes' data layer that follow one another
    /// (<see cref="CartesianAxesRenderer.DataLayers"/>), in their order and in one clip.</summary>
    /// <param name="run">The steps, each naming a patch of the axes by its index.</param>
    internal void Render(ReadOnlySpan<LayerStep> run)
    {
        var drawing = new PatchDrawing(Axes, Ctx, Theme);
        var placed = new List<PlacedPatch>(run.Length);
        foreach (var step in run) Place(drawing, step.Index, placed);
        Draw(drawing, placed);
    }

    /// <summary>Adds the patch at <paramref name="index"/> as it is drawn to <paramref name="placed"/>, unless it is hidden or cannot
    /// be drawn.</summary>
    private void Place(PatchDrawing drawing, int index, List<PlacedPatch> placed)
    {
        var path = drawing.Placed(index, new TransformPlacement(Transform));
        if (path.Count > 0) placed.Add(new PlacedPatch(index, path));
    }

    /// <summary>Draws <paramref name="placed"/> in order (<see cref="PatchDrawing"/>), in one clip when the axes clips its series.</summary>
    private void Draw(PatchDrawing drawing, List<PlacedPatch> placed)
    {
        if (placed.Count == 0) return;

        using var clip = _clipped ? Ctx.ClippedTo(PlotArea) : default;
        foreach (var one in placed) drawing.Draw(one.Index, one.Path);
    }
}
