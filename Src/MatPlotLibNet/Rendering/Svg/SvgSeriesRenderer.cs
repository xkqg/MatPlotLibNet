// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

internal sealed class SvgSeriesRenderer : ISeriesVisitor
{
    private readonly DataTransform _transform;
    private readonly IRenderContext _ctx;
    private readonly Color _seriesColor;

    public SvgSeriesRenderer(DataTransform transform, IRenderContext ctx, Color seriesColor)
    {
        _transform = transform;
        _ctx = ctx;
        _seriesColor = seriesColor;
    }

    public void Visit(LineSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        var points = new List<Point>(series.XData.Length);
        for (int i = 0; i < series.XData.Length; i++)
            points.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));

        _ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);

        if (series.Marker is not null && series.Marker != MarkerStyle.None)
        {
            foreach (var pt in points)
                _ctx.DrawCircle(pt, series.MarkerSize / 2, color, null, 0);
        }
    }

    public void Visit(ScatterSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.XData.Length; i++)
        {
            var pt = _transform.DataToPixel(series.XData[i], series.YData[i]);
            double size = series.Sizes is not null ? Math.Sqrt(series.Sizes[i]) : Math.Sqrt(series.MarkerSize);
            var c = series.Colors is not null ? series.Colors[i] : color;
            _ctx.DrawCircle(pt, size / 2, c, null, 0);
        }
    }

    public void Visit(BarSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        int count = series.Categories.Length;

        for (int i = 0; i < count; i++)
        {
            double catPos = i;
            double value = series.Values[i];
            double halfWidth = series.BarWidth / 2;

            if (series.Orientation == BarOrientation.Vertical)
            {
                var topLeft = _transform.DataToPixel(catPos - halfWidth, Math.Max(value, 0));
                var bottomRight = _transform.DataToPixel(catPos + halfWidth, Math.Min(value, 0));
                var rect = new Rect(topLeft.X, topLeft.Y,
                    bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                _ctx.DrawRectangle(rect, color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
            else
            {
                var topLeft = _transform.DataToPixel(Math.Min(value, 0), catPos + halfWidth);
                var bottomRight = _transform.DataToPixel(Math.Max(value, 0), catPos - halfWidth);
                var rect = new Rect(topLeft.X, topLeft.Y,
                    bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                _ctx.DrawRectangle(rect, color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
        }
    }

    public void Visit(HistogramSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        if (series.Data.Length == 0) return;

        var bins = series.ComputeBins();

        for (int i = 0; i < bins.Counts.Length; i++)
        {
            double x0 = bins.Min + i * bins.BinWidth;
            double x1 = x0 + bins.BinWidth;
            var topLeft = _transform.DataToPixel(x0, bins.Counts[i]);
            var bottomRight = _transform.DataToPixel(x1, 0);
            var rect = new Rect(topLeft.X, topLeft.Y,
                bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            _ctx.DrawRectangle(rect, color, series.EdgeColor ?? Color.White, 0.5);
        }
    }

    public void Visit(PieSeries series, RenderArea area)
    {
        double total = series.Sizes.Sum();
        if (total == 0) return;

        double cx = area.PlotBounds.X + area.PlotBounds.Width / 2;
        double cy = area.PlotBounds.Y + area.PlotBounds.Height / 2;
        double radius = Math.Min(area.PlotBounds.Width, area.PlotBounds.Height) / 2 * 0.8;

        double startAngle = series.StartAngle * Math.PI / 180;
        for (int i = 0; i < series.Sizes.Length; i++)
        {
            double sweep = series.Sizes[i] / total * 2 * Math.PI;
            double endAngle = startAngle + sweep;

            double x1 = cx + radius * Math.Cos(startAngle);
            double y1 = cy - radius * Math.Sin(startAngle);
            double x2 = cx + radius * Math.Cos(endAngle);
            double y2 = cy - radius * Math.Sin(endAngle);

            var sliceColor = series.Colors is not null && i < series.Colors.Length
                ? series.Colors[i]
                : _seriesColor;

            var segments = new PathSegment[]
            {
                new MoveToSegment(new Point(cx, cy)),
                new LineToSegment(new Point(x1, y1)),
                new ArcSegment(new Point(x2, y2), radius, radius, startAngle, endAngle),
                new CloseSegment()
            };
            _ctx.DrawPath(segments, sliceColor, Color.White, 1);

            startAngle = endAngle;
        }
    }

    public void Visit(HeatmapSeries series, RenderArea area)
    {
        int rows = series.Data.GetLength(0);
        int cols = series.Data.GetLength(1);
        if (rows == 0 || cols == 0) return;

        double cellW = area.PlotBounds.Width / cols;
        double cellH = area.PlotBounds.Height / rows;

        double min = double.MaxValue, max = double.MinValue;
        foreach (double v in series.Data) { min = Math.Min(min, v); max = Math.Max(max, v); }
        double range = max - min;
        if (range == 0) range = 1;

        var cmap = series.ColorMap ?? Styling.ColorMaps.ColorMaps.Viridis;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            double norm = (series.Data[r, c] - min) / range;
            var color = cmap.GetColor(norm);
            var rect = new Rect(
                area.PlotBounds.X + c * cellW,
                area.PlotBounds.Y + r * cellH,
                cellW, cellH);
            _ctx.DrawRectangle(rect, color, null, 0);
        }
    }

    public void Visit(BoxSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i].OrderBy(v => v).ToArray();
            if (data.Length == 0) continue;

            double q1 = Percentile(data, 25);
            double median = Percentile(data, 50);
            double q3 = Percentile(data, 75);
            double whiskerLo = data[0];
            double whiskerHi = data[^1];

            double catPos = i;
            double halfW = 0.35;

            var tl = _transform.DataToPixel(catPos - halfW, q3);
            var br = _transform.DataToPixel(catPos + halfW, q1);
            _ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), null, color, 1.5);

            var ml = _transform.DataToPixel(catPos - halfW, median);
            var mr = _transform.DataToPixel(catPos + halfW, median);
            _ctx.DrawLine(ml, mr, series.MedianColor ?? Color.Red, 2, LineStyle.Solid);

            var wt = _transform.DataToPixel(catPos, whiskerHi);
            var wb = _transform.DataToPixel(catPos, whiskerLo);
            _ctx.DrawLine(_transform.DataToPixel(catPos, q3), wt, color, 1, LineStyle.Solid);
            _ctx.DrawLine(_transform.DataToPixel(catPos, q1), wb, color, 1, LineStyle.Solid);
        }
    }

    public void Visit(ViolinSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i].OrderBy(v => v).ToArray();
            if (data.Length == 0) continue;

            double catPos = i;
            double min = data[0], max = data[^1];

            int steps = 20;
            var leftPoints = new List<Point>();
            var rightPoints = new List<Point>();
            double range = max - min;
            if (range == 0) range = 1;

            double bandwidth = range / steps;
            for (int s = 0; s <= steps; s++)
            {
                double y = min + range * s / steps;
                // Binary search for points within bandwidth instead of O(n) scan
                int lo = BisectLeft(data, y - bandwidth);
                int hi = BisectRight(data, y + bandwidth);
                double density = (hi - lo) / (double)data.Length;
                double halfW = density * 2;
                leftPoints.Add(_transform.DataToPixel(catPos - halfW, y));
                rightPoints.Add(_transform.DataToPixel(catPos + halfW, y));
            }

            rightPoints.Reverse();
            var outline = new List<Point>();
            outline.AddRange(leftPoints);
            outline.AddRange(rightPoints);

            _ctx.DrawPolygon(outline, color.WithAlpha(180), color, 1);
        }
    }

    public void Visit(ContourSeries series, RenderArea area)
    {
        // Simplified: draw as colored rects (heatmap-like)
        var heatmap = new HeatmapSeries(series.ZData) { ColorMap = series.ColorMap };
        Visit(heatmap, area);
    }

    public void Visit(StemSeries series, RenderArea area)
    {
        var stemColor = series.StemColor ?? _seriesColor;
        var markerColor = series.MarkerColor ?? _seriesColor;

        for (int i = 0; i < series.XData.Length; i++)
        {
            var top = _transform.DataToPixel(series.XData[i], series.YData[i]);
            var bottom = _transform.DataToPixel(series.XData[i], 0);
            _ctx.DrawLine(bottom, top, stemColor, 1, LineStyle.Solid);
            _ctx.DrawCircle(top, 4, markerColor, null, 0);
        }

        // Baseline
        if (series.XData.Length > 0)
        {
            var baseLeft = _transform.DataToPixel(series.XData.Min(), 0);
            var baseRight = _transform.DataToPixel(series.XData.Max(), 0);
            _ctx.DrawLine(baseLeft, baseRight, series.BaselineColor ?? Color.Gray, 1, LineStyle.Solid);
        }
    }

    private static double Percentile(double[] sorted, double p)
    {
        double idx = (sorted.Length - 1) * p / 100.0;
        int lower = (int)Math.Floor(idx);
        int upper = Math.Min(lower + 1, sorted.Length - 1);
        double frac = idx - lower;
        return sorted[lower] + frac * (sorted[upper] - sorted[lower]);
    }

    private static int BisectLeft(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (sorted[mid] < value) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }

    private static int BisectRight(double[] sorted, double value)
    {
        int lo = 0, hi = sorted.Length;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (sorted[mid] <= value) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }
}
