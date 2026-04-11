// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class LineSeriesRenderer : SeriesRenderer<LineSeries>
{
    public LineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(LineSeries series)
    {
        var color = ResolveColor(series.Color);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);

        // Apply DrawStyle step interpolation before transforming
        var (drawX, drawY) = ApplyDrawStyle(data.X, data.Y, series.DrawStyle);

        var points = new List<Point>(Transform.TransformBatch(drawX, drawY));
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);

        if (series.Marker is not null && series.Marker != MarkerStyle.None)
        {
            var markerFill = series.MarkerFaceColor ?? color;
            var markerStroke = series.MarkerEdgeColor;
            var markerStrokeWidth = series.MarkerEdgeColor is not null ? series.MarkerEdgeWidth : 0;

            // For step-interpolated data, draw markers only on original data points
            var markerPoints = series.DrawStyle is not null
                ? new List<Point>(Transform.TransformBatch(data.X, data.Y))
                : points;

            for (int i = 0; i < markerPoints.Count; i++)
            {
                if (series.MarkEvery is not null && i % series.MarkEvery.Value != 0) continue;
                Ctx.DrawCircle(markerPoints[i], series.MarkerSize / 2, markerFill, markerStroke, markerStrokeWidth);
            }
        }
    }

    private static (double[] x, double[] y) ApplyDrawStyle(double[] x, double[] y, DrawStyle? drawStyle)
    {
        if (drawStyle is null or DrawStyle.Default || x.Length < 2) return (x, y);

        var newX = new List<double>(x.Length * 2);
        var newY = new List<double>(y.Length * 2);

        switch (drawStyle)
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
