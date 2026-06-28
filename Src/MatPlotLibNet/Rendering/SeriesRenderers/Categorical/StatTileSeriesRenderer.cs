// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StatTileSeries"/> instances onto an <see cref="IRenderContext"/> — a big centred
/// headline number with the series label beneath it, filling the plot area.</summary>
internal sealed class StatTileSeriesRenderer : SeriesRenderer<StatTileSeries>
{
    /// <inheritdoc />
    public StatTileSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StatTileSeries series)
    {
        var bounds = Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bounds.Height / 2;
        var color = ResolveColor(series.AccentColor);

        Ctx.DrawText(series.FormattedValue, new Point(cx, cy),
            new Font { Size = 44, Weight = FontWeight.Bold, Color = color }, TextAlignment.Center);

        if (!string.IsNullOrEmpty(series.Label))
        {
            Ctx.DrawText(series.Label, new Point(cx, cy + 30), new Font { Size = 14 }, TextAlignment.Center);
        }
    }
}
