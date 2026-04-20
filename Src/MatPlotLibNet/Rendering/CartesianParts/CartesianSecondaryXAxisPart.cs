// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders the secondary X-axis (twin-Y — top edge of the plot): secondary
/// X-axis series + top-edge tick marks + optional secondary X-axis label.
/// Extracted verbatim from <c>CartesianAxesRenderer.Render</c> lines 240-278
/// (pre-B.8).
/// </summary>
internal sealed class CartesianSecondaryXAxisPart : CartesianAxesPart
{
    private readonly DataRange _secRange;
    private readonly int _colorOffset;

    /// <summary>Constructs a secondary-X-axis part.</summary>
    /// <param name="axes">The axes model (reads X-secondary series + secondary X-axis config from here).</param>
    /// <param name="plotArea">Pixel-space plot-area rectangle.</param>
    /// <param name="ctx">Render context to draw into.</param>
    /// <param name="theme">Active theme (for colors, default font).</param>
    /// <param name="primaryTransform">Data→pixel transform for the primary axes; passed to the base class.</param>
    /// <param name="secRange">The computed secondary-X-axis range.</param>
    /// <param name="colorOffset">Offset for color-cycle indexing; equals
    /// <c>primarySeriesCount + secondaryYSeriesCount</c> so the color cycle
    /// continues past both primary and secondary-Y series.</param>
    public CartesianSecondaryXAxisPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                                        DataTransform primaryTransform, DataRange secRange,
                                        int colorOffset)
        : base(axes, plotArea, ctx, theme, primaryTransform)
    {
        _secRange = secRange;
        _colorOffset = colorOffset;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var secXTransform = new DataTransform(_secRange.XMin, _secRange.XMax, _secRange.YMin, _secRange.YMax, PlotArea);
        var secXTicks = AxesRenderer.ComputeTickValuesInternal(_secRange.XMin, _secRange.XMax);

        for (int i = 0; i < Axes.XSecondarySeries.Count; i++)
        {
            var series = Axes.XSecondarySeries[i];
            if (!series.Visible) continue;
            int colorIndex = _colorOffset + i;
            var seriesColor = Theme.CycleColors[colorIndex % Theme.CycleColors.Length];
            var renderer = new SvgSeriesRenderer(secXTransform, Ctx, seriesColor, plotArea: PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
        }

        // Top-edge X-axis ticks
        var tickFont = TickFont();
        var secXUniformFormat = AxesRenderer.BuildUniformTickFormatter(secXTicks);
        foreach (var tick in secXTicks)
        {
            var pt = secXTransform.DataToPixel(tick, _secRange.YMax);
            Ctx.DrawLine(new Point(pt.X, PlotArea.Y),
                new Point(pt.X, PlotArea.Y - 5),
                Theme.ForegroundText, 1, LineStyle.Solid);
            Ctx.DrawText(Axes.SecondaryXAxis!.TickFormatter?.Format(tick) ?? secXUniformFormat(tick),
                new Point(pt.X, PlotArea.Y - 8),
                tickFont, TextAlignment.Center);
        }

        if (Axes.SecondaryXAxis!.Label is not null)
        {
            Ctx.DrawText(Axes.SecondaryXAxis.Label,
                new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y - 28),
                LabelFont(), TextAlignment.Center);
        }
    }
}
