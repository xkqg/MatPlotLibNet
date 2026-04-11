// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class AreaSeriesRenderer : SeriesRenderer<AreaSeries>
{
    public AreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) return;

        // Apply step interpolation to the top edge if requested
        var (topX, topY) = ApplyStepMode(data.X, data.Y, series.StepMode);

        // Batch-transform the top edge; reuse pixel X for the reverse bottom edge
        var topPts = Transform.TransformBatch(topX, topY);
        var pxX = Transform.TransformX(topX);
        var polygon = new List<Point>(topPts.Length * 2);
        polygon.AddRange(topPts);

        if (series.YData2 is not null)
        {
            var (bot2X, bot2Y) = ApplyStepMode(data.X, series.YData2, series.StepMode);
            var botPxX = Transform.TransformX(bot2X);
            var botPxY = Transform.TransformY(bot2Y);
            for (int i = bot2X.Length - 1; i >= 0; i--) polygon.Add(new Point(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = Transform.TransformY([0.0])[0];
            for (int i = pxX.Length - 1; i >= 0; i--) polygon.Add(new Point(pxX[i], pyZero));
        }

        Ctx.DrawPolygon(polygon, fillColor, null, 0);

        // Draw boundary with EdgeColor override if set
        var edgeColor = series.EdgeColor ?? color;
        Ctx.DrawLines(new List<Point>(topPts), edgeColor, series.LineWidth, series.LineStyle);
    }

    private static (double[] x, double[] y) ApplyStepMode(double[] x, double[] y, DrawStyle style)
    {
        if (style is DrawStyle.Default || x.Length < 2) return (x, y);

        var newX = new List<double>(x.Length * 2);
        var newY = new List<double>(y.Length * 2);

        switch (style)
        {
            case DrawStyle.StepsPre:
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0) { newX.Add(x[i]); newY.Add(y[i - 1]); }
                    newX.Add(x[i]); newY.Add(y[i]);
                }
                break;

            case DrawStyle.StepsPost:
                for (int i = 0; i < x.Length; i++)
                {
                    newX.Add(x[i]); newY.Add(y[i]);
                    if (i < x.Length - 1) { newX.Add(x[i + 1]); newY.Add(y[i]); }
                }
                break;

            case DrawStyle.StepsMid:
                for (int i = 0; i < x.Length; i++)
                {
                    if (i > 0)
                    {
                        var midX = (x[i - 1] + x[i]) / 2;
                        newX.Add(midX); newY.Add(y[i - 1]);
                        newX.Add(midX); newY.Add(y[i]);
                    }
                    newX.Add(x[i]); newY.Add(y[i]);
                }
                break;
        }

        return ([.. newX], [.. newY]);
    }
}
