// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SunburstSeries"/> as concentric ring segments.</summary>
internal sealed class SunburstSeriesRenderer : SeriesRenderer<SunburstSeries>
{
    /// <inheritdoc />
    public SunburstSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SunburstSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bounds.Height / 2;
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2 - 10;

        // Count max depth
        int maxDepth = GetMaxDepth(series.Root);
        if (maxDepth == 0) return;

        double ringWidth = (maxRadius - series.InnerRadius * maxRadius) / maxDepth;
        RenderRing(series.Root, cx, cy, 0, 360, 0, series, ringWidth, maxRadius);
    }

    private void RenderRing(TreeNode node, double cx, double cy,
        double startAngle, double sweepAngle, int depth,
        SunburstSeries series, double ringWidth, double maxRadius)
    {
        if (node.Children.Count == 0) return;

        double innerR = series.InnerRadius * maxRadius + depth * ringWidth;
        double outerR = innerR + ringWidth;
        double total = node.TotalValue;
        if (total <= 0) return;

        double currentAngle = startAngle;
        foreach (var child in node.Children)
        {
            double childSweep = sweepAngle * (child.TotalValue / total);
            var color = child.Color ?? ResolveColor(null);

            // Draw arc segment
            double startRad = currentAngle * Math.PI / 180;
            double endRad = (currentAngle + childSweep) * Math.PI / 180;

            var segments = new List<PathSegment>
            {
                new MoveToSegment(new Point(cx + innerR * Math.Cos(startRad), cy + innerR * Math.Sin(startRad))),
                new LineToSegment(new Point(cx + outerR * Math.Cos(startRad), cy + outerR * Math.Sin(startRad))),
                new ArcSegment(new Point(cx, cy), outerR, outerR, currentAngle, currentAngle + childSweep),
                new LineToSegment(new Point(cx + innerR * Math.Cos(endRad), cy + innerR * Math.Sin(endRad))),
                new ArcSegment(new Point(cx, cy), innerR, innerR, currentAngle + childSweep, currentAngle),
                new CloseSegment()
            };
            Ctx.DrawPath(segments, color, Colors.White, 1);

            // Recurse
            RenderRing(child, cx, cy, currentAngle, childSweep, depth + 1, series, ringWidth, maxRadius);
            currentAngle += childSweep;
        }
    }

    private static int GetMaxDepth(TreeNode node)
    {
        if (node.Children.Count == 0) return 0;
        int max = 0;
        foreach (var child in node.Children)
            max = Math.Max(max, GetMaxDepth(child));
        return max + 1;
    }
}
