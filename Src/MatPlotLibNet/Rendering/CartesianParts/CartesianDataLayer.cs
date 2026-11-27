// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>The data layer of one Cartesian render: what its steps that are not series are drawn with (the grid and the ticks it stands
/// at, the spans, each run of patches, the reference lines, the drawing tools, the tick marks a theme places among the data), and the host
/// of the primary series' pass (<see cref="ILayerHost"/>): the one loop of the pass (<see cref="SeriesPass.DrawSteps"/>) draws each series
/// and hands every other step here. A value on the stack of the render, so a draw allocates nothing for it.</summary>
/// <param name="renderer">The renderer the layer belongs to: the colour slots, what is drawn inside a series' group and the tick passes.</param>
/// <param name="context">What the render draws with, through the transform the primary series are drawn with.</param>
/// <param name="grid">The grid the axes draws.</param>
/// <param name="xGridTicks">The X ticks the grid stands at.</param>
/// <param name="yGridTicks">The Y ticks the grid stands at.</param>
/// <param name="range">The range the axes shows.</param>
/// <param name="clipsPatches">Whether the patches are clipped to the plot area (<see cref="AxesRange.PatchesMayReachPastPlot"/>).</param>
internal readonly struct CartesianDataLayer(CartesianAxesRenderer renderer, CartesianPartContext context, GridStyle grid, double[] xGridTicks,
    double[] yGridTicks, DataRange range, bool clipsPatches) : ILayerHost
{
    /// <summary>The part that draws the primary X axis.</summary>
    internal CartesianAxisPart X { get; init; }

    /// <summary>The part that draws the primary Y axis, left slot 0 of the container.</summary>
    internal CartesianAxisPart Y { get; init; }

    /// <summary>What the edges opposite the primary axes draw (<see cref="CartesianEdges"/>).</summary>
    internal CartesianEdges Edges { get; init; }

    /// <inheritdoc />
    public int[] ColorSlots => context.ColorSlots;

    /// <inheritdoc />
    public void DrawInsideSeriesGroup(int seriesIndex) => renderer.DrawInsideSeriesGroup(seriesIndex);

    /// <inheritdoc />
    /// <remarks>Each layer of the data that is not a series, through its own part. Patches that follow one another are one run, drawn in
    /// one clip, as they were when the patches were one layer.</remarks>
    public int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at)
    {
        switch (steps[at].Layer)
        {
            case DataLayer.Grid:
                context.Ctx.RenderGrid(grid, context.Axes, xGridTicks, yGridTicks, pass.Transform, context.Theme);
                break;
            case DataLayer.Spans:
                new CartesianSpansPart(context, range).Render();
                break;
            case DataLayer.Patch:
                int end = at + 1;
                while (end < steps.Length && steps[end].Layer == DataLayer.Patch) end++;
                new CartesianPatchesPart(context, clipsPatches).Render(steps[at..end]);
                return end - 1;
            case DataLayer.ReferenceLines:
                new CartesianReferenceLinesPart(context, range).Render();
                break;
            case DataLayer.Tools:
                new CartesianToolsPart(context).Render();
                break;
            case DataLayer.TickMarks:
                renderer.RenderTicks(this, TickInk.Marks);
                break;
        }
        return at;
    }
}
