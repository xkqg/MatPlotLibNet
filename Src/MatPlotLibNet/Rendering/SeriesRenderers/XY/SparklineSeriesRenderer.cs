// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class SparklineSeriesRenderer : SeriesRenderer<SparklineSeries>
{
    public SparklineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(SparklineSeries series)
    {
        if (series.Values.Length < 2) return;
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;
        double yMin = series.Values.Min(), yMax = series.Values.Max();
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }
        double xStep = bounds.Width / (series.Values.Length - 1);
        double yScale = bounds.Height / (yMax - yMin);
        var points = new List<Point>(series.Values.Length);
        for (int i = 0; i < series.Values.Length; i++)
            points.Add(new Point(bounds.X + i * xStep, bounds.Y + bounds.Height - (series.Values[i] - yMin) * yScale));
        Ctx.DrawLines(points, color, series.LineWidth, LineStyle.Solid);
    }
}
