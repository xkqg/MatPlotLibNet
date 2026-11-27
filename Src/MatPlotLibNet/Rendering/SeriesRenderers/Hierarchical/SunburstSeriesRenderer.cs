// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SunburstSeries"/> as concentric ring segments.</summary>
internal sealed class SunburstSeriesRenderer : SeriesRenderer<SunburstSeries>
{

    /// <inheritdoc />
    public SunburstSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(SunburstSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(SunburstSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        var frame = RoundFrame.In(bounds, RoundFrame.FullShare, OuterRingInsetPx);
        double cx = frame.Centre.X, cy = frame.Centre.Y;
        double maxRadius = frame.Radius;

        // Count max depth
        int maxDepth = series.Root.Levels();
        if (maxDepth == 0) return;

        double ringWidth = (maxRadius - series.InnerRadius * maxRadius) / maxDepth;

        // Collected label candidates — placed in one collision-resolution batch after
        // all ring segments have drawn, so labels on dense rings don't overlap.
        var labelCandidates = series.ShowLabels ? new List<LabelCandidate>() : null;

        RenderRing(series.Root, cx, cy, 0, 360, 0, series, ringWidth, maxRadius, labelCandidates);

        if (labelCandidates is { Count: > 0 })
            DrawPlacedLabels(PlaceLabels(labelCandidates, bounds));
    }

    private void RenderRing(TreeNode node, double cx, double cy,
        double startAngle, double sweepAngle, int depth,
        SunburstSeries series, double ringWidth, double maxRadius,
        List<LabelCandidate>? labelCandidates)
    {
        // The children and the share of this ring each takes: the one rule every hierarchical layout partitions by.
        var shares = node.Shares();
        if (shares.Count == 0) return;

        double innerR = series.InnerRadius * maxRadius + depth * ringWidth;
        double outerR = innerR + ringWidth;

        // Every label has the same size, so the text-size measurement inside the LabelLayoutEngine matches what the renderer
        // will eventually draw (both go through the surface); its ink is the one that reads against its segment.
        double labelSize = series.LabelSizeUnder(Context.Theme);

        double currentAngle = startAngle;
        foreach (var (child, share) in shares)
        {
            double childSweep = sweepAngle * share;
            var color = child.Color ?? SeriesColor;

            Ctx.DrawPath(new RingWedge(cx, cy, innerR, outerR, currentAngle, currentAngle + childSweep).Path(), ShapeStyle.Edged(color, Colors.White, Slot.LineWidthOf(LineWidthRule.OwnPixel)));

            // Collect a label for this ring segment when labels are enabled and the wedge
            // is wide enough to plausibly host one. Anchor at the radial+angular midpoint.
            if (labelCandidates is not null
                && childSweep >= series.MinLabelSweepDegrees
                && !string.IsNullOrEmpty(child.Label))
            {
                double midAngle = (currentAngle + childSweep / 2).ToRadians();
                double midR = (innerR + outerR) / 2.0;
                var anchor = new Point(cx, cy).AtAngle(midR, midAngle);
                labelCandidates.Add(new LabelCandidate(anchor, child.Label, Context.Theme.DataTextFontOn(color, labelSize, GroundInk.Readable), TextAlignment.Center) { VerticalAlignment = TextVerticalAlignment.Center });
            }

            // Recurse
            RenderRing(child, cx, cy, currentAngle, childSweep, depth + 1, series, ringWidth, maxRadius, labelCandidates);
            currentAngle += childSweep;
        }
    }

    /// <summary>Pixel inset subtracted from the plot's half-extent before computing
    /// the outermost ring radius — leaves visible clearance for outer labels.</summary>
    private const double OuterRingInsetPx = 10.0;
}
