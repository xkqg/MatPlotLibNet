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
    private readonly bool _tooltipsEnabled;

    public SvgSeriesRenderer(DataTransform transform, IRenderContext ctx, Color seriesColor, bool tooltipsEnabled = false)
    {
        _transform = transform;
        _ctx = ctx;
        _seriesColor = seriesColor;
        _tooltipsEnabled = tooltipsEnabled;
    }

    /// <summary>Opens a tooltip wrapper around the next drawn elements when tooltips are enabled.</summary>
    /// <remarks>Only effective when the context is <see cref="SvgRenderContext"/>; wraps elements in
    /// <c>&lt;g&gt;&lt;title&gt;text&lt;/title&gt;...&lt;/g&gt;</c> for native browser hover tooltips.</remarks>
    private void BeginTooltip(string text)
    {
        if (_tooltipsEnabled && _ctx is SvgRenderContext svgCtx)
            svgCtx.BeginTooltipGroup(text);
    }

    /// <summary>Closes the tooltip wrapper opened by <see cref="BeginTooltip"/>.</summary>
    private void EndTooltip()
    {
        if (_tooltipsEnabled && _ctx is SvgRenderContext svgCtx)
            svgCtx.EndTooltipGroup();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Visit(ScatterSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.XData.Length; i++)
        {
            BeginTooltip($"x={series.XData[i]:G5}, y={series.YData[i]:G5}");
            var pt = _transform.DataToPixel(series.XData[i], series.YData[i]);
            double size = series.Sizes is not null ? Math.Sqrt(series.Sizes[i]) : Math.Sqrt(series.MarkerSize);
            var c = series.Colors is not null ? series.Colors[i] : color;
            _ctx.DrawCircle(pt, size / 2, c, null, 0);
            EndTooltip();
        }
    }

    /// <inheritdoc />
    public void Visit(BarSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        int count = series.Categories.Length;

        for (int i = 0; i < count; i++)
        {
            double catPos = i;
            double value = series.Values[i];
            double halfWidth = series.BarWidth / 2;
            double baseline = series.StackBaseline is not null ? series.StackBaseline[i] : 0;

            if (series.Orientation == BarOrientation.Vertical)
            {
                var topLeft = _transform.DataToPixel(catPos - halfWidth, baseline + Math.Max(value, 0));
                var bottomRight = _transform.DataToPixel(catPos + halfWidth, baseline + Math.Min(value, 0));
                var rect = new Rect(topLeft.X, topLeft.Y,
                    bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                _ctx.DrawRectangle(rect, color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
            else
            {
                var topLeft = _transform.DataToPixel(baseline + Math.Min(value, 0), catPos + halfWidth);
                var bottomRight = _transform.DataToPixel(baseline + Math.Max(value, 0), catPos - halfWidth);
                var rect = new Rect(topLeft.X, topLeft.Y,
                    bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
                _ctx.DrawRectangle(rect, color, series.EdgeColor, series.EdgeColor.HasValue ? 1 : 0);
            }
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Visit(ContourSeries series, RenderArea area)
    {
        // Simplified: draw as colored rects (heatmap-like)
        var heatmap = new HeatmapSeries(series.ZData) { ColorMap = series.ColorMap };
        Visit(heatmap, area);
    }

    /// <inheritdoc />
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

    /// <summary>Renders a radar (spider) chart with its own polar coordinate system inside the plot bounds.</summary>
    /// <remarks>Draws concentric web polygons at 20% intervals, radial axis lines, category labels,
    /// and a filled data polygon. Bypasses the standard <see cref="DataTransform"/> since radar charts
    /// use polar coordinates computed directly from <see cref="RenderArea.PlotBounds"/>.</remarks>
    public void Visit(RadarSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        var fillColor = series.FillColor ?? color.WithAlpha((byte)(series.Alpha * 255));
        int n = series.Categories.Length;
        if (n < 3) return;

        var bounds = area.PlotBounds;
        double centerX = bounds.X + bounds.Width / 2;
        double centerY = bounds.Y + bounds.Height / 2;
        double radius = Math.Min(bounds.Width, bounds.Height) / 2 * 0.75;

        double maxVal = series.MaxValue ?? series.Values.Max();
        if (maxVal <= 0) maxVal = 1;

        // Draw concentric web polygons (20%, 40%, 60%, 80%, 100%)
        var webColor = Color.FromHex("#CCCCCC");
        for (int ring = 1; ring <= 5; ring++)
        {
            double frac = ring / 5.0;
            var ringPoints = new List<Point>(n);
            for (int i = 0; i < n; i++)
            {
                double angle = 2 * Math.PI * i / n - Math.PI / 2;
                ringPoints.Add(new Point(
                    centerX + radius * frac * Math.Cos(angle),
                    centerY + radius * frac * Math.Sin(angle)));
            }
            _ctx.DrawPolygon(ringPoints, null, webColor, 0.5);
        }

        // Draw radial lines and category labels
        var labelFont = new Font { Size = 10 };
        for (int i = 0; i < n; i++)
        {
            double angle = 2 * Math.PI * i / n - Math.PI / 2;
            var endpoint = new Point(
                centerX + radius * Math.Cos(angle),
                centerY + radius * Math.Sin(angle));
            _ctx.DrawLine(new Point(centerX, centerY), endpoint, webColor, 0.5, LineStyle.Solid);

            // Label slightly outside the web
            var labelPos = new Point(
                centerX + (radius + 15) * Math.Cos(angle),
                centerY + (radius + 15) * Math.Sin(angle));
            _ctx.DrawText(series.Categories[i], labelPos, labelFont, TextAlignment.Center);
        }

        // Draw data polygon
        var dataPoints = new List<Point>(n);
        for (int i = 0; i < n; i++)
        {
            double normalized = Math.Min(series.Values[i] / maxVal, 1.0);
            double angle = 2 * Math.PI * i / n - Math.PI / 2;
            dataPoints.Add(new Point(
                centerX + radius * normalized * Math.Cos(angle),
                centerY + radius * normalized * Math.Sin(angle)));
        }

        _ctx.DrawPolygon(dataPoints, fillColor, color, series.LineWidth);
    }

    /// <summary>Renders a vector field as arrows with shaft lines and V-shaped arrowheads.</summary>
    /// <remarks>Each arrow starts at (X, Y) and extends by (U, V) scaled by <see cref="QuiverSeries.Scale"/>.
    /// Arrowheads are drawn at approximately 143 degrees from the shaft direction.</remarks>
    public void Visit(QuiverSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.XData.Length; i++)
        {
            double x0 = series.XData[i], y0 = series.YData[i];
            double x1 = x0 + series.UData[i] * series.Scale;
            double y1 = y0 + series.VData[i] * series.Scale;

            var start = _transform.DataToPixel(x0, y0);
            var end = _transform.DataToPixel(x1, y1);

            // Shaft
            _ctx.DrawLine(start, end, color, 1.5, LineStyle.Solid);

            // Arrowhead
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) continue;

            double headLen = len * series.ArrowHeadSize;
            double angle = Math.Atan2(dy, dx);
            double a1 = angle + 2.5; // ~143 degrees
            double a2 = angle - 2.5;

            var h1 = new Point(end.X + headLen * Math.Cos(a1), end.Y + headLen * Math.Sin(a1));
            var h2 = new Point(end.X + headLen * Math.Cos(a2), end.Y + headLen * Math.Sin(a2));
            _ctx.DrawLine(end, h1, color, 1.5, LineStyle.Solid);
            _ctx.DrawLine(end, h2, color, 1.5, LineStyle.Solid);
        }
    }

    /// <summary>Renders OHLC candlesticks with wicks (high-low lines) and filled bodies (open-close rectangles).</summary>
    /// <remarks>Candles where close &gt;= open are colored with <see cref="CandlestickSeries.UpColor"/>;
    /// candles where close &lt; open use <see cref="CandlestickSeries.DownColor"/>.</remarks>
    public void Visit(CandlestickSeries series, RenderArea area)
    {
        double halfW = series.BodyWidth / 2;
        for (int i = 0; i < series.Open.Length; i++)
        {
            bool isUp = series.Close[i] >= series.Open[i];
            var color = isUp ? series.UpColor : series.DownColor;
            double bodyTop = Math.Max(series.Open[i], series.Close[i]);
            double bodyBottom = Math.Min(series.Open[i], series.Close[i]);

            // Wick
            var wickTop = _transform.DataToPixel(i, series.High[i]);
            var wickBottom = _transform.DataToPixel(i, series.Low[i]);
            _ctx.DrawLine(wickBottom, wickTop, color, 1, LineStyle.Solid);

            // Body
            var topLeft = _transform.DataToPixel(i - halfW, bodyTop);
            var bottomRight = _transform.DataToPixel(i + halfW, bodyBottom);
            var bodyRect = new Rect(
                topLeft.X, topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);
            _ctx.DrawRectangle(bodyRect, color, color, 1);
        }
    }

    /// <summary>Renders vertical error bars with end caps and center markers at each data point.</summary>
    /// <remarks>When <see cref="ErrorBarSeries.XErrorLow"/> and <see cref="ErrorBarSeries.XErrorHigh"/>
    /// are provided, horizontal error bars with caps are also drawn.</remarks>
    public void Visit(ErrorBarSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        for (int i = 0; i < series.XData.Length; i++)
        {
            double x = series.XData[i], y = series.YData[i];
            var center = _transform.DataToPixel(x, y);
            var top = _transform.DataToPixel(x, y + series.YErrorHigh[i]);
            var bottom = _transform.DataToPixel(x, y - series.YErrorLow[i]);

            // Vertical error bar
            _ctx.DrawLine(bottom, top, color, series.LineWidth, LineStyle.Solid);

            // Caps
            _ctx.DrawLine(
                new Point(top.X - series.CapSize, top.Y),
                new Point(top.X + series.CapSize, top.Y),
                color, series.LineWidth, LineStyle.Solid);
            _ctx.DrawLine(
                new Point(bottom.X - series.CapSize, bottom.Y),
                new Point(bottom.X + series.CapSize, bottom.Y),
                color, series.LineWidth, LineStyle.Solid);

            // Optional horizontal error bars
            if (series.XErrorLow is not null && series.XErrorHigh is not null)
            {
                var left = _transform.DataToPixel(x - series.XErrorLow[i], y);
                var right = _transform.DataToPixel(x + series.XErrorHigh[i], y);
                _ctx.DrawLine(left, right, color, series.LineWidth, LineStyle.Solid);
                _ctx.DrawLine(
                    new Point(left.X, left.Y - series.CapSize),
                    new Point(left.X, left.Y + series.CapSize),
                    color, series.LineWidth, LineStyle.Solid);
                _ctx.DrawLine(
                    new Point(right.X, right.Y - series.CapSize),
                    new Point(right.X, right.Y + series.CapSize),
                    color, series.LineWidth, LineStyle.Solid);
            }

            // Center marker
            _ctx.DrawCircle(center, 3, color, null, 0);
        }
    }

    /// <summary>Renders a step-function line by generating intermediate horizontal and vertical segments.</summary>
    /// <remarks>The step position determines where the transition occurs:
    /// <see cref="StepPosition.Post"/> steps after each point (matplotlib default),
    /// <see cref="StepPosition.Pre"/> steps before, and <see cref="StepPosition.Mid"/> steps at the midpoint.</remarks>
    public void Visit(StepSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        int n = series.XData.Length;
        if (n == 0) return;

        var stepPoints = new List<Point>();

        switch (series.StepPosition)
        {
            case StepPosition.Post:
                for (int i = 0; i < n - 1; i++)
                {
                    stepPoints.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));
                    stepPoints.Add(_transform.DataToPixel(series.XData[i + 1], series.YData[i]));
                }
                stepPoints.Add(_transform.DataToPixel(series.XData[n - 1], series.YData[n - 1]));
                break;

            case StepPosition.Pre:
                stepPoints.Add(_transform.DataToPixel(series.XData[0], series.YData[0]));
                for (int i = 1; i < n; i++)
                {
                    stepPoints.Add(_transform.DataToPixel(series.XData[i - 1], series.YData[i]));
                    stepPoints.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));
                }
                break;

            case StepPosition.Mid:
                for (int i = 0; i < n - 1; i++)
                {
                    double midX = (series.XData[i] + series.XData[i + 1]) / 2;
                    stepPoints.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));
                    stepPoints.Add(_transform.DataToPixel(midX, series.YData[i]));
                    stepPoints.Add(_transform.DataToPixel(midX, series.YData[i + 1]));
                }
                stepPoints.Add(_transform.DataToPixel(series.XData[n - 1], series.YData[n - 1]));
                break;
        }

        _ctx.DrawLines(stepPoints, color, series.LineWidth, series.LineStyle);
    }

    /// <summary>Renders a filled area between the top line and a baseline (y=0 or a second Y dataset).</summary>
    /// <remarks>The fill region is drawn as a polygon with alpha transparency, followed by a solid top-edge line.
    /// When <see cref="AreaSeries.YData2"/> is set, fills between the two curves instead of to y=0.</remarks>
    public void Visit(AreaSeries series, RenderArea area)
    {
        var color = series.Color ?? _seriesColor;
        var fillColor = series.FillColor ?? color.WithAlpha((byte)(series.Alpha * 255));
        int n = series.XData.Length;
        if (n == 0) return;

        // Build polygon: top line forward, then baseline backward
        var polygon = new List<Point>(n * 2);
        for (int i = 0; i < n; i++)
            polygon.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));

        if (series.YData2 is not null)
        {
            for (int i = n - 1; i >= 0; i--)
                polygon.Add(_transform.DataToPixel(series.XData[i], series.YData2[i]));
        }
        else
        {
            for (int i = n - 1; i >= 0; i--)
                polygon.Add(_transform.DataToPixel(series.XData[i], 0));
        }

        _ctx.DrawPolygon(polygon, fillColor, null, 0);

        // Draw top edge line
        var topPoints = new List<Point>(n);
        for (int i = 0; i < n; i++)
            topPoints.Add(_transform.DataToPixel(series.XData[i], series.YData[i]));
        _ctx.DrawLines(topPoints, color, series.LineWidth, series.LineStyle);
    }

    /// <summary>Computes the <paramref name="p"/>-th percentile from a pre-sorted array using linear interpolation.</summary>
    private static double Percentile(double[] sorted, double p)
    {
        double idx = (sorted.Length - 1) * p / 100.0;
        int lower = (int)Math.Floor(idx);
        int upper = Math.Min(lower + 1, sorted.Length - 1);
        double frac = idx - lower;
        return sorted[lower] + frac * (sorted[upper] - sorted[lower]);
    }

    /// <summary>Returns the leftmost insertion index for <paramref name="value"/> in a sorted array (binary search).</summary>
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

    /// <summary>Returns the rightmost insertion index for <paramref name="value"/> in a sorted array (binary search).</summary>
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
