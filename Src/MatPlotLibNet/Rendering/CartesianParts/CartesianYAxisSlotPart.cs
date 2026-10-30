// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Draws ONE Y axis of the container, on either side of the plot and at any place in its stack, the primary included:
/// its series, its major and minor tick marks and labels, and its title. Every Y axis is drawn here by the same steps, so
/// what one axis can do (a locator, a log scale, minor ticks, turned tick labels, a title with math markup) every axis
/// does; where a slot stands and which way it faces come from its <see cref="PlacedYSlot"/> and the side terms derived
/// from its side (<see cref="YSideTerms"/>), so nothing here asks which side it is on.
/// </summary>
/// <remarks>
/// <para>The renderer draws a slot past the primary with <see cref="Render"/>. The primary Y axis, left slot 0, goes through
/// the same part in the steps the axes' own drawing order puts it in: its tick marks with the other axes' ticks
/// (<see cref="RenderMajorTicks"/>, <see cref="RenderMinorTicks"/>), its title with the X axis' title
/// (<see cref="RenderTitle"/>); its series are drawn by the axes' own series pass, which also groups and stacks bars.</para>
/// <para>Each series is drawn inside the series group keyed by the series' identity slot, and its end label is drawn
/// inside that group (the renderer's <c>DrawInsideSeriesGroup</c>), so the legend toggle and the hover highlight reach the
/// line and its label together.</para>
/// </remarks>
internal sealed class CartesianYAxisSlotPart : CartesianAxesPart
{
    private readonly PlacedYSlot _placed;
    private readonly Action<int> _insideSeriesGroup;

    /// <summary>Constructs the part that draws one placed Y axis slot.</summary>
    /// <param name="context">The axes, plot area, drawing context and theme of the render.</param>
    /// <param name="placed">The slot as this render places it: its range, transform, ticks, identity and offset.</param>
    /// <param name="insideSeriesGroup">What is drawn inside a series' group after the series itself, given the series'
    /// identity slot: its end label.</param>
    internal CartesianYAxisSlotPart(CartesianPartContext context, PlacedYSlot placed, Action<int> insideSeriesGroup)
        : base(context.Axes, context.PlotArea, context.Ctx, context.Theme, placed.Transform)
    {
        _placed = placed;
        _insideSeriesGroup = insideSeriesGroup;
        TickDraw = placed.Terms.Hang(placed.TickDraw(context.Theme), placed.TickEdgeX(context.PlotArea, context.Axes.Spines), context.Axes.Spines);
    }

    /// <summary>How this slot's major ticks are drawn: facing its side and hung from its edge.</summary>
    internal TickDrawContext TickDraw { get; }

    /// <summary>The major tick values this slot draws.</summary>
    internal double[] Ticks => _placed.Ticks;

    /// <summary>The slot's axis.</summary>
    private Axis Axis => _placed.Slot.Axis;

    /// <inheritdoc />
    /// <remarks>Its series, its major ticks, its minor ticks, then its title past the widest tick label drawn.</remarks>
    public override void Render()
    {
        RenderSeries();
        double widest = RenderMajorTicks();
        RenderMinorTicks();
        RenderTitle(widest);
    }

    /// <summary>Draws the slot's own series with its transform, each in the colour its identity slot holds, inside its own
    /// series group and clipped to the plot.</summary>
    internal void RenderSeries()
    {
        var colorSlots = Axes.ColorSlots();
        var series = _placed.Slot.Series;
        for (int i = 0; i < series.Count; i++)
        {
            var one = series[i];
            if (!one.Visible) continue;
            int index = Axes.SlotIndex(_placed.Pair, _placed.FirstIndexInPair + i);
            // The slot, cycled properties, tooltip switch and theme the primary series pass hands over, so a line on any
            // axis wears the colour its legend swatch shows.
            var colourSlot = Theme.SeriesSlot(colorSlots[index]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, one, index, Transform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(Transform, Ctx, colourSlot.CycleColor, Theme, colourSlot.CycledProperties,
                Axes.EnableTooltips, PlotArea) { FirstColorSlot = colorSlots[index] };
            one.Accept(renderer, new RenderArea(PlotArea, Ctx));
            if (clipped) Ctx.PopClip();
            _insideSeriesGroup(index);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Draws the slot's major tick marks and labels by its own <see cref="TickConfig"/>, through the one tick
    /// drawing every axis uses.</summary>
    /// <returns>The widest tick label drawn; 0 when none was.</returns>
    internal double RenderMajorTicks() =>
        Ctx.RenderAxisTicks(_placed.Ticks, _placed.Range.XMin, Transform, Axis.MajorTicks, TickDraw);

    /// <summary>Draws the slot's minor tick marks between its major ticks, when its minor ticks are visible.</summary>
    internal void RenderMinorTicks() =>
        Ctx.RenderMinorTicks(new TickRun(_placed.Ticks, _placed.Range.XMin, Transform), Axis.MinorTicks, TickDraw);

    /// <summary>Draws the slot's title by the one band rule every Y axis shares: the ink that faces the tick labels stands
    /// the container's gap past the widest of them, its style's pad on top; as rich text when it carries math markup.</summary>
    /// <param name="widestTickLabel">The widest tick label the slot drew; 0 when none was.</param>
    internal void RenderTitle(double widestTickLabel) =>
        Ctx.DrawYAxisTitle(Axis,
            new YAxisTitleInput(Axis.MajorTicks.TickLabelBand(widestTickLabel), _placed.Slot.Side, Axes.YAxes.TitleGap, LabelFont()),
            new Point(TickDraw.AxisEdge, PlotArea.Y + PlotArea.Height / 2));
}
