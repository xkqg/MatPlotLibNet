// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders the secondary Y-axis (twin-X — right side of the plot): secondary
/// series + right-edge tick marks + optional secondary Y-axis label.
/// </summary>
/// <remarks>Each series is drawn inside the series group the primary pass opens for a primary one
/// (<see cref="SeriesGroupExtensions.BeginSeriesGroup"/>, keyed by the series' slot, the index its legend entry carries),
/// and its end label is drawn inside that group, so the legend toggle and the hover highlight reach the line and its
/// label together.</remarks>
internal sealed class CartesianSecondaryYAxisPart : CartesianAxesPart
{
    private readonly SecondaryAxisSlot _slot;
    private readonly int _startingIndexInPair;
    private readonly double _outwardOffset;
    private readonly DataRange _secRange;
    private readonly DataTransform _secTransform;
    private readonly CartesianDirectLabelsPart _directLabels;

    /// <summary>Constructs a secondary-Y-axis part for one outward-stacked right-side slot.</summary>
    /// <param name="axes">The axes model.</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="slot">The right-side slot this instance draws — its own axis and its own series, never another
    /// slot's (<see cref="Models.SecondaryAxisSlot"/>).</param>
    /// <param name="startingIndexInPair">How many right-side series stand before this slot's own, across every slot
    /// nearer the plot — the identity/colour numbering (<see cref="SeriesAxisPair.SecondaryY"/>) runs across every
    /// slot as one list, matching <see cref="Axes.AllSeries"/>' order, so slot 1's first series is never slot 0's.</param>
    /// <param name="outwardOffset">This slot's own pixel offset past the plot's right edge — 0 for slot 0, the sum of
    /// every slot's band before it otherwise (<see cref="Layout.AxisStackingExtensions.StackedOffsets"/>).</param>
    /// <param name="secRange">The computed range for <paramref name="slot"/> (via the orchestrator's
    /// <c>ComputeSecondaryDataRanges</c>).</param>
    /// <param name="secTransform">The data→pixel transform of <paramref name="slot"/>'s axis, built once from
    /// <paramref name="secRange"/> by the orchestrator, which hands the same value to the end-label part so a
    /// secondary label sits on the line this part draws.</param>
    /// <param name="directLabels">The end labels of the axes; this part asks it for a secondary series' label while that
    /// series' group is open.</param>
    public CartesianSecondaryYAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
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
            // The same slot, cycled properties, tooltip switch and theme the primary series pass hands over, so a
            // secondary line wears the colour its legend swatch shows.
            var colourSlot = Theme.SeriesSlot(colorSlots[Axes.SlotIndex(SeriesAxisPair.SecondaryY, indexInPair)]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, Axes.SlotIndex(SeriesAxisPair.SecondaryY, indexInPair), secTransform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(secTransform, Ctx, colourSlot.CycleColor, Theme, colourSlot.CycledProperties,
                Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (clipped) Ctx.PopClip();
            _directLabels.DrawLabelOf(SeriesAxisPair.SecondaryY, indexInPair);
            if (openedGroup) Ctx.EndGroup();
        }

        // Right-edge ticks, drawn by the axis' own TickConfig through the one tick drawing the primary axes use, past
        // this slot's own outward offset (0 for slot 0, past every slot before it otherwise).
        var axis = _slot.Axis;
        var right = PlotArea.X + PlotArea.Width + _outwardOffset;
        double widestTick = Ctx.RenderAxisTicks(secYTicks, _secRange.XMax, secTransform, axis.MajorTicks,
            new TickDrawContext
            {
                Side          = TickSide.Right,
                AxisEdge      = right,
                SpineHalf     = Axes.Spines.Right.LineWidth / 2.0,
                BaseFont      = TickFont(),
                Formatter     = axis.TickFormatter,
                UniformFormat = AxesRenderer.BuildUniformTickFormatter(secYTicks),
                Foreground    = Theme.ForegroundText,
                LabelRotation = axis.MajorTicks.LabelRotation,
                Alignment     = TextAlignment.Left,
            });

        if (axis.Label is not null)
        {
            // Measured clearance and a 90° rotation by the rule the primary Y label follows; the label style's
            // font and pad apply the same way.
            var font = axis.LabelStyle?.ApplyTo(LabelFont()) ?? LabelFont();
            var offset = axis.MajorTicks.AxisLabelOffset(widestTick) + (axis.LabelStyle?.Pad ?? 0);
            Ctx.DrawText(axis.Label, new Point(right + offset, PlotArea.Y + PlotArea.Height / 2),
                font, TextAlignment.Center, 90);
        }
    }
}
