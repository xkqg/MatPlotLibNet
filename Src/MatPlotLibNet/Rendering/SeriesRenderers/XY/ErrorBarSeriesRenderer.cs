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
        int n = series.XData.Length;

        // Batch-transform all center, top, and bottom Y coordinates (SIMD)
        var pxCenter = Transform.TransformX(series.XData);
        var pyCenterArr = Transform.TransformY(series.YData);
        var topYData = new double[n];
        var botYData = new double[n];
        for (int i = 0; i < n; i++) { topYData[i] = series.YData[i] + series.YErrorHigh[i]; botYData[i] = series.YData[i] - series.YErrorLow[i]; }
        var pyTop = Transform.TransformY(topYData);
        var pyBot = Transform.TransformY(botYData);

        for (int i = 0; i < n; i++)
        {
            double cx = pxCenter[i];
            var center = new Point(cx, pyCenterArr[i]);
            var top = new Point(cx, pyTop[i]);
            var bottom = new Point(cx, pyBot[i]);
            Ctx.DrawLine(bottom, top, color, series.LineWidth, LineStyle.Solid);
            Ctx.DrawLine(new Point(top.X - series.CapSize, top.Y), new Point(top.X + series.CapSize, top.Y), color, series.LineWidth, LineStyle.Solid);
            Ctx.DrawLine(new Point(bottom.X - series.CapSize, bottom.Y), new Point(bottom.X + series.CapSize, bottom.Y), color, series.LineWidth, LineStyle.Solid);
            if (series.XErrorLow is not null && series.XErrorHigh is not null)
            {
                var left = Transform.DataToPixel(series.XData[i] - series.XErrorLow[i], series.YData[i]);
                var right = Transform.DataToPixel(series.XData[i] + series.XErrorHigh[i], series.YData[i]);
                Ctx.DrawLine(left, right, color, series.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(left.X, left.Y - series.CapSize), new Point(left.X, left.Y + series.CapSize), color, series.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(right.X, right.Y - series.CapSize), new Point(right.X, right.Y + series.CapSize), color, series.LineWidth, LineStyle.Solid);
            }
            Ctx.DrawCircle(center, 3, color, null, 0);
        }
    }
}
