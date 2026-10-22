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
    private readonly DataRange _secRange;
    private readonly DataTransform _secTransform;
    private readonly CartesianDirectLabelsPart _directLabels;

    /// <summary>Constructs a secondary-Y-axis part.</summary>
    /// <param name="axes">The axes model (reads secondary series + secondary axis config from here).</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="secRange">The computed secondary-axis range (via the orchestrator's
    /// <c>ComputeSecondaryDataRanges</c>).</param>
    /// <param name="secTransform">The data→pixel transform of the secondary axis, built once from
    /// <paramref name="secRange"/> by the orchestrator, which hands the same value to the end-label part so a
    /// secondary label sits on the line this part draws.</param>
    /// <param name="directLabels">The end labels of the axes; this part asks it for a secondary series' label while that
    /// series' group is open.</param>
    public CartesianSecondaryYAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                         DataTransform primaryTransform, DataRange secRange, DataTransform secTransform,
                                         CartesianDirectLabelsPart directLabels)
        : base(axes, plotArea, ctx, theme, primaryTransform)
    {
        _secRange = secRange;
        _secTransform = secTransform;
        _directLabels = directLabels;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var secTransform = _secTransform;
        var secYTicks = AxesRenderer.ComputeTickValuesInternal(_secRange.YMin, _secRange.YMax);

        for (int i = 0; i < Axes.SecondarySeries.Count; i++)
        {
            var series = Axes.SecondarySeries[i];
            if (!series.Visible) continue;
            // The same slot, cycled properties, tooltip switch and theme the primary series pass hands over, so a
            // secondary line wears the colour its legend swatch shows.
            var slot = Axes.SeriesSlot(Theme, SeriesAxisPair.SecondaryY, i);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, Axes.SlotIndex(SeriesAxisPair.SecondaryY, i), secTransform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(secTransform, Ctx, slot.CycleColor, Theme, slot.CycledProperties,
                Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (clipped) Ctx.PopClip();
            _directLabels.DrawLabelOf(SeriesAxisPair.SecondaryY, i);
            if (openedGroup) Ctx.EndGroup();
        }

        // Right-edge ticks, drawn by the axis' own TickConfig through the one tick drawing the primary axes use.
        var axis = Axes.SecondaryYAxis!;
        var right = PlotArea.X + PlotArea.Width;
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
