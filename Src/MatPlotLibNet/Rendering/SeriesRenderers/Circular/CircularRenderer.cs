// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Abstract base for circular series renderers (Donut, Sunburst) that share arc-wedge
/// path construction and outer-label placement logic.</summary>
/// <typeparam name="TSeries">The concrete series type rendered by the subclass.</typeparam>
internal abstract class CircularRenderer<TSeries> : SeriesRenderer<TSeries>
    where TSeries : ISeries
{
    /// <inheritdoc />
    protected CircularRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Builds a ring-wedge or solid-slice path using ArcSegment commands.
    /// When <paramref name="innerR"/> is 0 the result is a solid slice (first segment moves to
    /// the centre); when positive the result is a ring wedge (first segment moves to the inner
    /// arc start).  Angles follow the standard math convention: Y = cy + r·sin(angle).</summary>
    /// <param name="cx">Circle centre X.</param>
    /// <param name="cy">Circle centre Y.</param>
    /// <param name="innerR">Inner radius (0 for a solid slice).</param>
    /// <param name="outerR">Outer radius.</param>
    /// <param name="startAngleDeg">Start angle in degrees.</param>
    /// <param name="endAngleDeg">End angle in degrees.</param>
    internal static IReadOnlyList<PathSegment> BuildWedgePath(
        double cx, double cy,
        double innerR, double outerR,
        double startAngleDeg, double endAngleDeg)
    {
        double startRad = startAngleDeg * Math.PI / 180;
        double endRad   = endAngleDeg   * Math.PI / 180;

        if (innerR <= 0)
        {
            return new PathSegment[]
            {
                new MoveToSegment(new Point(cx, cy)),
                new LineToSegment(new Point(cx + outerR * Math.Cos(startRad), cy + outerR * Math.Sin(startRad))),
                new ArcSegment(new Point(cx, cy), outerR, outerR, startAngleDeg, endAngleDeg),
                new CloseSegment()
            };
        }

        return new PathSegment[]
        {
            new MoveToSegment(new Point(cx + innerR * Math.Cos(startRad), cy + innerR * Math.Sin(startRad))),
            new LineToSegment(new Point(cx + outerR * Math.Cos(startRad), cy + outerR * Math.Sin(startRad))),
            new ArcSegment(new Point(cx, cy), outerR, outerR, startAngleDeg, endAngleDeg),
            new LineToSegment(new Point(cx + innerR * Math.Cos(endRad), cy + innerR * Math.Sin(endRad))),
            new ArcSegment(new Point(cx, cy), innerR, innerR, endAngleDeg, startAngleDeg),
            new CloseSegment()
        };
    }

    /// <summary>Runs <see cref="LabelLayoutEngine.Place"/> on <paramref name="candidates"/>, draws
    /// a leader line for any displaced label, then draws each label text via <see cref="SeriesRenderer.Ctx"/>.</summary>
    internal void PlaceOuterLabels(IReadOnlyList<LabelCandidate> candidates, Rect bounds)
    {
        if (candidates.Count == 0) return;
        var placements = LabelLayoutEngine.Place(candidates, bounds, ChartServices.FontMetrics);
        var leaderColor = Context.Theme.ForegroundText;
        foreach (var p in placements)
        {
            if (p.LeaderLineStart is { } anchor)
                CalloutBoxRenderer.DrawLeaderLine(Ctx, anchor, p.FinalPoint, leaderColor);
            Ctx.DrawText(p.Text, p.FinalPoint, p.Font, p.Alignment);
        }
    }
}
