// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders one left-side outward-stacked secondary Y axis (ROADMAP.md row 101 — the mirror of
/// <see cref="CartesianSecondaryYAxisPart"/> on the other side of the primary Y axis): this slot's own series,
/// left-edge tick marks + optional label, past the primary Y axis and every left slot nearer the plot.
/// </summary>
/// <remarks>Each series is drawn inside the series group the primary pass opens for a primary one
/// (<see cref="SeriesGroupExtensions.BeginSeriesGroup"/>, keyed by the series' slot, the index its legend entry carries),
/// and its end label is drawn inside that group, so the legend toggle and the hover highlight reach the line and its
/// label together.</remarks>
internal sealed class CartesianSecondaryLeftYAxisPart : CartesianAxesPart
{
    private readonly SecondaryAxisSlot _slot;
    private readonly int _startingIndexInPair;
    private readonly double _outwardOffset;
    private readonly DataRange _secRange;
    private readonly DataTransform _secTransform;
    private readonly CartesianDirectLabelsPart _directLabels;

    /// <summary>Constructs a secondary-left-Y-axis part for one outward-stacked left-side slot.</summary>
    /// <param name="axes">The axes model.</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="slot">The left-side slot this instance draws — its own axis and its own series.</param>
    /// <param name="startingIndexInPair">How many left-side series stand before this slot's own, across every slot
    /// nearer the plot — numbered as its own running sequence, kept apart from the right side's and the primary's,
    /// the same way <see cref="SeriesAxisPair.SecondaryY"/> keeps the right side apart from the primary.</param>
    /// <param name="outwardOffset">This slot's own pixel offset past the plot's left edge — 0 for the first left
    /// slot, the sum of every left slot's band before it otherwise
    /// (<see cref="Layout.AxisStackingExtensions.StackedOffsets"/>).</param>
    /// <param name="secRange">The computed range for <paramref name="slot"/>.</param>
    /// <param name="secTransform">The data→pixel transform of <paramref name="slot"/>'s axis.</param>
    /// <param name="directLabels">The end labels of the axes.</param>
    public CartesianSecondaryLeftYAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                             DataTransform primaryTransform, SecondaryAxisSlot slot, int startingIndexInPair,
                                             double outwardOffset, DataRange secRange, DataTransform secTransform,
                                             CartesianDirectLabelsPart directLabels)
        : base(axes, plotArea, ctx, theme, primaryTransform)
    {
        _slot = slot;
        _startingIndexInPair = startingIndexInPair;
        _outwardOffset = outwardOffset;
        _secRange = secRange;
        _secTransform = secTransform;
        _directLabels = directLabels;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var secTransform = _secTransform;
        var secYTicks = AxesRenderer.ComputeTickValuesInternal(_secRange.YMin, _secRange.YMax);

        var colorSlots = Axes.ColorSlots();
        for (int i = 0; i < _slot.Series.Count; i++)
        {
            var series = _slot.Series[i];
            if (!series.Visible) continue;
            int indexInPair = _startingIndexInPair + i;
            var colourSlot = Theme.SeriesSlot(colorSlots[Axes.SlotIndex(SeriesAxisPair.SecondaryLeftY, indexInPair)]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, Axes.SlotIndex(SeriesAxisPair.SecondaryLeftY, indexInPair), secTransform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(secTransform, Ctx, colourSlot.CycleColor, Theme, colourSlot.CycledProperties,
                Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (clipped) Ctx.PopClip();
            _directLabels.DrawLabelOf(SeriesAxisPair.SecondaryLeftY, indexInPair);
            if (openedGroup) Ctx.EndGroup();
        }

        // Left-edge ticks, past this slot's own outward offset (0 for the first left slot, past every left slot
        // before it otherwise), labels growing further left (Alignment = Right).
        var axis = _slot.Axis;
        var left = PlotArea.X - _outwardOffset;
        double widestTick = Ctx.RenderAxisTicks(secYTicks, _secRange.XMax, secTransform, axis.MajorTicks,
            new TickDrawContext
            {
                Side          = TickSide.Left,
                AxisEdge      = left,
                SpineHalf     = Axes.Spines.Left.LineWidth / 2.0,
                BaseFont      = TickFont(),
                Formatter     = axis.TickFormatter,
                UniformFormat = AxesRenderer.BuildUniformTickFormatter(secYTicks),
                Foreground    = Theme.ForegroundText,
                LabelRotation = axis.MajorTicks.LabelRotation,
                Alignment     = TextAlignment.Right,
            });

        if (axis.Label is not null)
        {
            var font = axis.LabelStyle?.ApplyTo(LabelFont()) ?? LabelFont();
            var offset = axis.MajorTicks.AxisLabelOffset(widestTick) + (axis.LabelStyle?.Pad ?? 0);
            Ctx.DrawText(axis.Label, new Point(left - offset, PlotArea.Y + PlotArea.Height / 2),
                font, TextAlignment.Center, 90);
        }
    }
}
