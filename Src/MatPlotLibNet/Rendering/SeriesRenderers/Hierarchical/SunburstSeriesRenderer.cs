// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SunburstSeries"/> as concentric ring segments.</summary>
internal sealed class SunburstSeriesRenderer : CircularRenderer<SunburstSeries>
{
    /// <inheritdoc />
    public SunburstSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SunburstSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bounds.Height / 2;
        double maxRadius = Math.Min(bounds.Width, bounds.Height) / 2 - HierarchicalLayout.Sunburst.OuterRingInsetPx;

        // Count max depth
        int maxDepth = GetMaxDepth(series.Root);
        if (maxDepth == 0) return;

        double ringWidth = (maxRadius - series.InnerRadius * maxRadius) / maxDepth;

        // Collected label candidates — placed in one collision-resolution batch after
        // all ring segments have drawn, so labels on dense rings don't overlap.
        var labelCandidates = series.ShowLabels ? new List<LabelCandidate>() : null;

        RenderRing(series.Root, cx, cy, 0, 360, 0, series, ringWidth, maxRadius, labelCandidates);

        if (labelCandidates is { Count: > 0 })
            PlaceOuterLabels(labelCandidates, bounds);
    }

    private void RenderRing(TreeNode node, double cx, double cy,
        double startAngle, double sweepAngle, int depth,
        SunburstSeries series, double ringWidth, double maxRadius,
        List<LabelCandidate>? labelCandidates)
    {
        if (node.Children.Count == 0) return;

        double innerR = series.InnerRadius * maxRadius + depth * ringWidth;
        double outerR = innerR + ringWidth;
        double total = node.TotalValue;
        if (total <= 0) return;

        // Reuse the cached label font so every ring uses the same metrics, and so the
        // text-size measurement inside the LabelLayoutEngine matches what the renderer
        // will eventually draw (both go through ChartServices.FontMetrics).
        Font? labelFont = null;
        if (labelCandidates is not null)
        {
            var f = Context.Theme.DefaultFont;
            labelFont = new Font { Family = f.Family, Size = Math.Max(9, f.Size - 1), Color = Colors.White };
        }

        double currentAngle = startAngle;
        foreach (var child in node.Children)
        {
            double childSweep = sweepAngle * (child.TotalValue / total);
            var color = child.Color ?? ResolveColor(null);

            Ctx.DrawPath(BuildWedgePath(cx, cy, innerR, outerR, currentAngle, currentAngle + childSweep), color, Colors.White, 1);

            // Collect a label for this ring segment when labels are enabled and the wedge
            // is wide enough to plausibly host one. Anchor at the radial+angular midpoint.
            if (labelCandidates is not null && labelFont is not null
                && childSweep >= series.MinLabelSweepDegrees
                && !string.IsNullOrEmpty(child.Label))
            {
                double midAngle = (currentAngle + childSweep / 2) * Math.PI / 180.0;
                double midR = (innerR + outerR) / 2.0;
                var anchor = new Point(cx + midR * Math.Cos(midAngle), cy + midR * Math.Sin(midAngle));
                labelCandidates.Add(new LabelCandidate(anchor, child.Label, labelFont, TextAlignment.Center));
            }

            // Recurse
            RenderRing(child, cx, cy, currentAngle, childSweep, depth + 1, series, ringWidth, maxRadius, labelCandidates);
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
