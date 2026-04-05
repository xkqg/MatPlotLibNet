// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ErrorBarSeriesRenderer : SeriesRenderer<ErrorBarSeries>
{
    public ErrorBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ErrorBarSeries series)
    {
        var color = ResolveColor(series.Color);
        for (int i = 0; i < series.XData.Length; i++)
        {
            double x = series.XData[i], y = series.YData[i];
            var center = Transform.DataToPixel(x, y);
            var top = Transform.DataToPixel(x, y + series.YErrorHigh[i]);
            var bottom = Transform.DataToPixel(x, y - series.YErrorLow[i]);
            Ctx.DrawLine(bottom, top, color, series.LineWidth, LineStyle.Solid);
            Ctx.DrawLine(new Point(top.X - series.CapSize, top.Y), new Point(top.X + series.CapSize, top.Y), color, series.LineWidth, LineStyle.Solid);
            Ctx.DrawLine(new Point(bottom.X - series.CapSize, bottom.Y), new Point(bottom.X + series.CapSize, bottom.Y), color, series.LineWidth, LineStyle.Solid);
            if (series.XErrorLow is not null && series.XErrorHigh is not null)
            {
                var left = Transform.DataToPixel(x - series.XErrorLow[i], y);
                var right = Transform.DataToPixel(x + series.XErrorHigh[i], y);
                Ctx.DrawLine(left, right, color, series.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(left.X, left.Y - series.CapSize), new Point(left.X, left.Y + series.CapSize), color, series.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(right.X, right.Y - series.CapSize), new Point(right.X, right.Y + series.CapSize), color, series.LineWidth, LineStyle.Solid);
            }
            Ctx.DrawCircle(center, 3, color, null, 0);
        }
    }
}
