// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="PieSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class PieSeriesRenderer : CircularRenderer<PieSeries>
{
    // Number of line segments used to approximate each arc. 120 gives smooth circles at typical sizes.
    private const int ArcSteps = 120;

    /// <inheritdoc />
    public PieSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PieSeries series)
    {
        double total = series.Sizes.Sum();
        if (total == 0) return;
        double cx = Area.PlotBounds.X + Area.PlotBounds.Width / 2;
        double cy = Area.PlotBounds.Y + Area.PlotBounds.Height / 2;
        double baseRadius = Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2 * 0.8;
        double radius = series.Radius.HasValue
            ? series.Radius.Value * Math.Min(Area.PlotBounds.Width, Area.PlotBounds.Height) / 2
            : baseRadius;
        // matplotlib default: start at top (90°) sweeping clockwise in screen coords.
        // We store angles in radians, Y-axis flipped (screen Y grows downward).
        double startAngle = series.StartAngle * Math.PI / 180;
        double direction = series.CounterClockwise ? 1 : -1; // screen-Y: CW = negative-sin

        // Collect outer-label candidates during the slice pass so LabelLayoutEngine can
        // resolve collisions in one batch at the end. Small wedges would otherwise draw
        // their labels on top of each other — matplotlib pies have the same issue.
        var df = Context.Theme.DefaultFont;
        var labelFont = new Font { Family = df.Family, Size = df.Size, Color = df.Color };
        var outerCandidates = new List<LabelCandidate>();
        var outerAnchors = new List<Point>();
        var outerAlignments = new List<TextAlignment>();

        for (int i = 0; i < series.Sizes.Length; i++)
        {
            double sweep = direction * series.Sizes[i] / total * 2 * Math.PI;
            double midAngle = startAngle + sweep / 2;
            double explode = series.Explode is not null && i < series.Explode.Length ? series.Explode[i] : 0.0;
            double sliceCx = cx + explode * radius * Math.Cos(midAngle);
            double sliceCy = cy - explode * radius * Math.Sin(midAngle);
            double endAngle = startAngle + sweep;

            // Shadow: draw offset gray slice before the main slice
            if (series.Shadow)
            {
                double shadowOffset = radius * 0.03;
                double scx = sliceCx + shadowOffset;
                double scy = sliceCy + shadowOffset;
                Ctx.DrawPath(
                    BuildSlicePath(scx, scy, radius, startAngle, endAngle),
                    new Color(0, 0, 0, 80), null, 0);
            }

            var sliceColor = series.Colors is not null && i < series.Colors.Length ? series.Colors[i] : SeriesColor;
            Ctx.DrawPath(
                BuildSlicePath(sliceCx, sliceCy, radius, startAngle, endAngle),
                sliceColor, Colors.White, 1);

            // AutoPct: draw percentage text at the centroid of the slice (inside the wedge).
            // Not collision-handled — interior labels are constrained to their own wedges and
            // rarely collide; if a wedge is too small for its AutoPct, the label just clips.
            if (series.AutoPct is not null)
            {
                double pct = series.Sizes[i] / total * 100;
                string label = string.Format(series.AutoPct, pct);
                double textAngle = startAngle + sweep / 2;
                double textR = radius * 0.6;
                double tx = sliceCx + textR * Math.Cos(textAngle);
                double ty = sliceCy - textR * Math.Sin(textAngle);
                Ctx.DrawText(label, new Point(tx, ty), new Font { Size = 10, Color = Colors.White }, TextAlignment.Center);
            }

            // Collect the outer label for the collision pass — do not draw yet.
            if (series.Labels is not null && i < series.Labels.Length && series.Labels[i] is { } sliceLabel)
            {
                double textAngle = startAngle + sweep / 2;
                double textR = radius * 1.1;
                double tx = sliceCx + textR * Math.Cos(textAngle);
                double ty = sliceCy - textR * Math.Sin(textAngle);
                double cosA = Math.Cos(textAngle);
                var align = cosA > 0.1 ? TextAlignment.Left : cosA < -0.1 ? TextAlignment.Right : TextAlignment.Center;
                outerCandidates.Add(new LabelCandidate(new Point(tx, ty), sliceLabel, labelFont, align));
                outerAnchors.Add(new Point(tx, ty));
                outerAlignments.Add(align);
            }

            startAngle = endAngle;
        }

        if (outerCandidates.Count > 0)
            PlaceOuterLabels(outerCandidates, Area.PlotBounds);
    }

    /// <summary>Builds a pie-slice path using line-segment polygon approximation so it works correctly
    /// in both Skia (PNG) and SVG renderers regardless of how each backend handles arc commands.</summary>
    private static PathSegment[] BuildSlicePath(double cx, double cy, double radius,
        double startAngle, double endAngle)
    {
        double sweep = endAngle - startAngle;
        int steps = Math.Max(3, (int)Math.Ceiling(Math.Abs(sweep) / (2 * Math.PI) * ArcSteps));
        var segments = new PathSegment[2 + steps + 1]; // MoveTo + Line + arc steps + Close
        segments[0] = new MoveToSegment(new Point(cx, cy));
        segments[1] = new LineToSegment(new Point(
            cx + radius * Math.Cos(startAngle),
            cy - radius * Math.Sin(startAngle)));
        for (int s = 1; s <= steps; s++)
        {
            double a = startAngle + sweep * s / steps;
            segments[2 + s - 1] = new LineToSegment(new Point(
                cx + radius * Math.Cos(a),
                cy - radius * Math.Sin(a)));
        }
        segments[2 + steps] = new CloseSegment();
        return segments;
    }
}
