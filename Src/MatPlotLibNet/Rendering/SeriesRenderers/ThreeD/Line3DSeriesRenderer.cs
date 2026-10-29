// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Line3DSeries"/> as depth-sorted projected polyline segments.</summary>
internal sealed class Line3DSeriesRenderer : SeriesRenderer<Line3DSeries>
{
    /// <inheritdoc />
    public Line3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Line3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Line3DSeries series)
    {
        if (series.X.Length < 2) return;

        var color = ResolveColor(series.Color);

        // Fitted to the values that are there: a missing value (NaN) and an infinity are left out.
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(), series.Z.Data.DrawableRange());

        // Build segments with average depth for painter's algorithm sorting
        int segCount = series.X.Length - 1;
        var segments = new List<DepthSegment>(segCount);

        for (int i = 0; i < segCount; i++)
        {
            double x0 = series.X.Data[i], y0 = series.Y.Data[i], z0 = series.Z.Data[i];
            double x1 = series.X.Data[i + 1], y1 = series.Y.Data[i + 1], z1 = series.Z.Data[i + 1];

            // A segment with an end that cannot be placed is skipped: the line breaks there, as a 2-D line does.
            var p0 = proj.Project(x0, y0, z0);
            var p1 = proj.Project(x1, y1, z1);
            if (!p0.CanDrawLineTo(p1)) continue;

            double avgDepth = (proj.Depth(x0, y0, z0) + proj.Depth(x1, y1, z1)) / 2.0;
            segments.Add(new(avgDepth, p0, p1));
        }

        // Sort back-to-front (painter's algorithm)
        segments.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        var stroke = Slot.StrokeOf(color, series.LineWidth, series.LineStyle);
        foreach (var seg in segments)
            Ctx.DrawLine(seg.P1, seg.P2, stroke);
    }

    private readonly record struct DepthSegment(double Depth, Point P1, Point P2);
}
