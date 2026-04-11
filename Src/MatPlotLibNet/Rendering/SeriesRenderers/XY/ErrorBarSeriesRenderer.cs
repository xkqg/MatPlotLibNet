// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
        double eLineWidth = series.ELineWidth ?? series.LineWidth;
        double capThick = series.CapThick ?? series.LineWidth;

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

            // Draw the data point marker always
            Ctx.DrawCircle(center, 3, color, null, 0);

            // Honor ErrorEvery: only draw error bars at every N-th point
            if (series.ErrorEvery <= 1 || i % series.ErrorEvery == 0)
            {
                var top = new Point(cx, pyTop[i]);
                var bottom = new Point(cx, pyBot[i]);
                Ctx.DrawLine(bottom, top, color, eLineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(top.X - series.CapSize, top.Y), new Point(top.X + series.CapSize, top.Y), color, capThick, LineStyle.Solid);
                Ctx.DrawLine(new Point(bottom.X - series.CapSize, bottom.Y), new Point(bottom.X + series.CapSize, bottom.Y), color, capThick, LineStyle.Solid);
                if (series.XErrorLow is not null && series.XErrorHigh is not null)
                {
                    var left = Transform.DataToPixel(series.XData[i] - series.XErrorLow[i], series.YData[i]);
                    var right = Transform.DataToPixel(series.XData[i] + series.XErrorHigh[i], series.YData[i]);
                    Ctx.DrawLine(left, right, color, eLineWidth, LineStyle.Solid);
                    Ctx.DrawLine(new Point(left.X, left.Y - series.CapSize), new Point(left.X, left.Y + series.CapSize), color, capThick, LineStyle.Solid);
                    Ctx.DrawLine(new Point(right.X, right.Y - series.CapSize), new Point(right.X, right.Y + series.CapSize), color, capThick, LineStyle.Solid);
                }
            }
        }
    }
}
