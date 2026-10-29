// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders the secondary X-axis (twin-Y — top edge of the plot): secondary
/// X-axis series + top-edge tick marks + optional secondary X-axis label.
/// </summary>
/// <remarks>Each series is drawn inside the series group the primary pass opens for a primary one
/// (<see cref="SeriesGroupExtensions.BeginSeriesGroup"/>, keyed by the series' slot), so the hover highlight and a
/// streaming diff reach a top-axis line as they reach any other. The axis owns the top edge: its title stands above its
/// tick labels (<see cref="AxisTickExtensions.TopAxisLabelOffset"/>), and the band it takes (<see cref="Band"/>) is what
/// the axes title rises by and what the layout reserves (<see cref="MeasureBand"/>), one rule for all three.</remarks>
internal sealed class CartesianSecondaryXAxisPart : CartesianAxesPart
{
    private readonly DataRange _secRange;

    /// <summary>Constructs a secondary-X-axis part.</summary>
    /// <param name="axes">The axes model (reads X-secondary series + secondary X-axis config from here).</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="secRange">The computed secondary-X-axis range.</param>
    public CartesianSecondaryXAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                        DataTransform primaryTransform, DataRange secRange)
        : base(axes, plotArea, ctx, theme, primaryTransform)
    {
        _secRange = secRange;
    }

    /// <summary>What this part drew above the top spine, set by <see cref="Render"/>: from the spine to the top of the
    /// axis title when there is one, otherwise to the top of the tallest tick label; 0 when nothing was drawn there.
    /// The axes title rises above it, and the layout reserves it with <see cref="MeasureBand"/>.</summary>
    internal double Band { get; private set; }

    /// <summary>The band <see cref="Render"/> will record (<see cref="Band"/>), measured before anything is drawn: the
    /// same tick values, formatter, fonts and placement rule.</summary>
    /// <param name="axes">The axes model; its secondary X axis is measured.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="theme">The active theme (fonts).</param>
    /// <param name="secRange">The secondary X axis range, as the renderer computes it.</param>
    /// <returns>The band in pixels.</returns>
    internal static double MeasureBand(Axes axes, IRenderContext ctx, Theme theme, DataRange secRange)
    {
        var ticks = AxesRenderer.ComputeTickValuesInternal(secRange.XMin, secRange.XMax);
        var axis = axes.SecondaryXAxis!;
        return BandAbove(axes, ctx, theme, ctx.MeasureAxisTicks(ticks, axis.MajorTicks, TickDraw(axes, theme, ticks, 0)));
    }

    /// <summary>How the top-edge ticks are drawn: by the axis' own <see cref="TickConfig"/>, through the one tick drawing
    /// the primary axes use. A rotated label is anchored at its start so it rises away from the axis.</summary>
    private static TickDrawContext TickDraw(Axes axes, Theme theme, double[] ticks, double edge)
    {
        var axis = axes.SecondaryXAxis!;
        var rotation = axis.MajorTicks.LabelRotation;
        return new TickDrawContext
        {
            Side          = TickSide.Top,
            AxisEdge      = edge,
            SpineHalf     = axes.Spines.Top.LineWidth / 2.0,
            BaseFont      = ThemedFontProvider.TickFont(theme),
            Formatter     = axis.TickFormatter,
            UniformFormat = AxesRenderer.BuildUniformTickFormatter(ticks),
            Foreground    = theme.ForegroundText,
            LabelRotation = rotation,
            Alignment     = rotation == 0 ? TextAlignment.Center : TextAlignment.Left,
        };
    }

    /// <summary>The axis title's font: the theme's label font with the label style applied, as on the primary axes.</summary>
    private static Font TitleFont(Axis axis, Theme theme)
    {
        var font = ThemedFontProvider.LabelFont(theme);
        return axis.LabelStyle?.ApplyTo(font) ?? font;
    }

    /// <summary>From the top spine to the axis title's baseline: <see cref="AxisTickExtensions.TopAxisLabelOffset"/> past
    /// the tallest tick label, plus the label style's pad.</summary>
    private static double TitleOffset(Axis axis, double tallestTickLabel, Font font) =>
        axis.MajorTicks.TopAxisLabelOffset(tallestTickLabel, font.Size) + (axis.LabelStyle?.Pad ?? 0);

    /// <summary>The one rule for the band: the tick label band, or, with a title, the title's offset plus its height.</summary>
    private static double BandAbove(Axes axes, IRenderContext ctx, Theme theme, double tallestTickLabel)
    {
        var axis = axes.SecondaryXAxis!;
        if (axis.Label is null) return axis.MajorTicks.TickLabelBand(tallestTickLabel);
        var font = TitleFont(axis, theme);
        return TitleOffset(axis, tallestTickLabel, font) + ctx.MeasureText(axis.Label, font).Height;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var secXTransform = new DataTransform(_secRange.XMin, _secRange.XMax, _secRange.YMin, _secRange.YMax, PlotArea);
        var secXTicks = AxesRenderer.ComputeTickValuesInternal(_secRange.XMin, _secRange.XMax);

        var colorSlots = Axes.ColorSlots();
        for (int i = 0; i < Axes.XSecondarySeries.Count; i++)
        {
            var series = Axes.XSecondarySeries[i];
            if (!series.Visible) continue;
            // The slot after the primary and the secondary-Y series, handed over the way the primary pass does.
            int firstSlot = colorSlots[Axes.SlotIndex(SeriesAxisPair.SecondaryX, i)];
            var slot = Theme.SeriesSlot(firstSlot);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, Axes.SlotIndex(SeriesAxisPair.SecondaryX, i), secXTransform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(secXTransform, Ctx, slot.CycleColor, Theme, slot.CycledProperties,
                Axes.EnableTooltips, PlotArea) { FirstColorSlot = firstSlot };
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (clipped) Ctx.PopClip();
            if (openedGroup) Ctx.EndGroup();
        }

        // Top-edge ticks, then the title above their labels (TitleOffset); the band they take is recorded for the
        // axes title, by the rule the layout reserves it with (BandAbove).
        var axis = Axes.SecondaryXAxis!;
        double tallest = Ctx.RenderAxisTicks(secXTicks, _secRange.YMax, secXTransform, axis.MajorTicks,
            TickDraw(Axes, Theme, secXTicks, PlotArea.Y));

        if (axis.Label is not null)
        {
            var font = TitleFont(axis, Theme);
            Ctx.DrawText(axis.Label,
                new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y - TitleOffset(axis, tallest, font)),
                font, TextAlignment.Center);
        }
        Band = BandAbove(Axes, Ctx, Theme, tallest);
    }
}
