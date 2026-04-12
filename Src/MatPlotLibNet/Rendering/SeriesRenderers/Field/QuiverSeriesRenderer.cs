// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="QuiverSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class QuiverSeriesRenderer : SeriesRenderer<QuiverSeries>
{
    /// <inheritdoc />
    public QuiverSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(QuiverSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.XData.Length; i++)
        {
            var start = Transform.DataToPixel(series.XData[i], series.YData[i]);
            var end = Transform.DataToPixel(series.XData[i] + series.UData[i] * series.Scale, series.YData[i] + series.VData[i] * series.Scale);
            Ctx.DrawLine(start, end, color, 1.5, LineStyle.Solid);
            double dx = end.X - start.X, dy = end.Y - start.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) continue;
            double headLen = len * series.ArrowHeadSize, angle = Math.Atan2(dy, dx);
            Ctx.DrawLine(end, new Point(end.X + headLen * Math.Cos(angle + 2.5), end.Y + headLen * Math.Sin(angle + 2.5)), color, 1.5, LineStyle.Solid);
            Ctx.DrawLine(end, new Point(end.X + headLen * Math.Cos(angle - 2.5), end.Y + headLen * Math.Sin(angle - 2.5)), color, 1.5, LineStyle.Solid);
        }
    }
}
