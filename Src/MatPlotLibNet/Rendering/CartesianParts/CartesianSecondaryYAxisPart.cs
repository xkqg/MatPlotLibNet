// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders the secondary Y-axis (twin-X — right side of the plot): secondary
/// series + right-edge tick marks + optional secondary Y-axis label.
/// Extracted verbatim from <c>CartesianAxesRenderer.Render</c> lines 201-238
/// (pre-B.7).
/// </summary>
internal sealed class CartesianSecondaryYAxisPart : CartesianAxesPart
{
    private readonly DataRange _secRange;
    private readonly int _primarySeriesCount;

    /// <summary>Constructs a secondary-Y-axis part.</summary>
    /// <param name="axes">The axes model (reads secondary series + secondary axis config from here).</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="secRange">The computed secondary-axis range (via the orchestrator's
    /// <c>ComputeSecondaryDataRanges</c>).</param>
    /// <param name="primarySeriesCount">Offset for color-cycle indexing so secondary
    /// series pick up where primary series left off.</param>
    public CartesianSecondaryYAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                         DataTransform primaryTransform, DataRange secRange,
                                         int primarySeriesCount)
        : base(axes, plotArea, ctx, theme, primaryTransform)
    {
        _secRange = secRange;
        _primarySeriesCount = primarySeriesCount;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var secTransform = new DataTransform(_secRange.XMin, _secRange.XMax, _secRange.YMin, _secRange.YMax, PlotArea);
        var secYTicks = AxesRenderer.ComputeTickValuesInternal(_secRange.YMin, _secRange.YMax);

        for (int i = 0; i < Axes.SecondarySeries.Count; i++)
        {
            var series = Axes.SecondarySeries[i];
            if (!series.Visible) continue;
            int colorIndex = _primarySeriesCount + i;
            var seriesColor = Theme.CycleColors[colorIndex % Theme.CycleColors.Length];
            var renderer = new SvgSeriesRenderer(secTransform, Ctx, seriesColor, plotArea: PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
        }

        // Right-side Y-axis ticks
        var tickFont = TickFont();
        var secYUniformFormat = AxesRenderer.BuildUniformTickFormatter(secYTicks);
        foreach (var tick in secYTicks)
        {
            var pt = secTransform.DataToPixel(_secRange.XMax, tick);
            Ctx.DrawLine(new Point(PlotArea.X + PlotArea.Width, pt.Y),
                new Point(PlotArea.X + PlotArea.Width + 5, pt.Y),
                Theme.ForegroundText, 1, LineStyle.Solid);
            Ctx.DrawText(Axes.SecondaryYAxis!.TickFormatter?.Format(tick) ?? secYUniformFormat(tick),
                new Point(PlotArea.X + PlotArea.Width + 8, pt.Y + 4),
                tickFont, TextAlignment.Left);
        }

        if (Axes.SecondaryYAxis!.Label is not null)
        {
            Ctx.DrawText(Axes.SecondaryYAxis.Label,
                new Point(PlotArea.X + PlotArea.Width + 45, PlotArea.Y + PlotArea.Height / 2),
                LabelFont(), TextAlignment.Center);
        }
    }
}
