// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Phase L.1a (v1.7.2, 2026-04-21) — TDD red phase for
/// <see cref="CircularRenderer{TSeries}"/>.
/// BuildWedgePath is a pure static geometry helper; PlaceOuterLabels is an instance
/// method that drives LabelLayoutEngine + CalloutBoxRenderer + DrawText.</summary>
public class CircularRendererTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 400);

    private static SeriesRenderContext NewContext(RecordingRenderContext ctx) =>
        new(new DataTransform(0, 100, 0, 100, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx));

    // Minimal concrete subclass so we can construct an instance for PlaceOuterLabels tests.
    private sealed class TestCircularRenderer : CircularRenderer<PieSeries>
    {
        public TestCircularRenderer(SeriesRenderContext ctx) : base(ctx) { }
        public override void Render(PieSeries series) { }
    }

    // ── BuildWedgePath ────────────────────────────────────────────────────────

    [Fact]
    public void BuildWedgePath_SolidSlice_FirstSegmentIsMoveToCentre()
    {
        // innerR == 0 → solid wedge starts from the centre of the circle.
        var segments = CircularRenderer<PieSeries>.BuildWedgePath(100, 150, 0, 60, 0, 90);

        var move = Assert.IsType<MoveToSegment>(segments[0]);
        Assert.Equal(100, move.Point.X, 1e-6);
        Assert.Equal(150, move.Point.Y, 1e-6);
    }

    [Fact]
    public void BuildWedgePath_RingWedge_FirstSegmentIsOnInnerArc()
    {
        // innerR > 0 → ring wedge starts from the inner-arc start point, NOT the centre.
        double cx = 200, cy = 200, innerR = 30;
        var segments = CircularRenderer<PieSeries>.BuildWedgePath(cx, cy, innerR, 60, 0, 90);

        var move = Assert.IsType<MoveToSegment>(segments[0]);
        // At startAngle 0°: inner arc start = (cx + innerR, cy)
        Assert.NotEqual(cx, move.Point.X, 1.0);  // must not be the centre
        Assert.Equal(cx + innerR, move.Point.X, 1e-6);
        Assert.Equal(cy, move.Point.Y, 1e-6);
    }

    [Fact]
    public void BuildWedgePath_HalfCircle_LineToInnerArcEndMatchesEndAngle()
    {
        // startAngle=0°, endAngle=180°, innerR=30
        // Ring path: [MoveToSegment, LineToSegment(outerStart), ArcSegment(outer),
        //             LineToSegment(innerEnd), ArcSegment(inner back), CloseSegment]
        // The 4th segment (index 3) is LineToSegment at inner arc end.
        // innerR * cos(180°) = -30, innerR * sin(180°) ≈ 0  →  (cx - 30, cy)
        double cx = 100, cy = 100, innerR = 30;
        var segments = CircularRenderer<PieSeries>.BuildWedgePath(cx, cy, innerR, 60, 0, 180);

        var line = Assert.IsType<LineToSegment>(segments[3]);
        Assert.Equal(cx - innerR, line.Point.X, 1e-6);
        Assert.Equal(cy, line.Point.Y, 1e-6);
    }

    // ── PlaceOuterLabels ──────────────────────────────────────────────────────

    [Fact]
    public void PlaceOuterLabels_EmptyCandidates_DrawsNothing()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new TestCircularRenderer(NewContext(ctx));

        renderer.PlaceOuterLabels([], Bounds);

        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void PlaceOuterLabels_SingleCandidate_DrawsOneLabel()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new TestCircularRenderer(NewContext(ctx));
        var font = new Font { Size = 11 };
        var candidates = new List<LabelCandidate>
        {
            new(new Point(200, 100), "Slice A", font, TextAlignment.Center)
        };

        renderer.PlaceOuterLabels(candidates, Bounds);

        Assert.Equal(1, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void PlaceOuterLabels_OverlappingCandidates_DrawsLeaderLine()
    {
        // Two labels placed at the same point force a collision-resolution displacement
        // of more than the default 6px threshold, so LeaderLineStart becomes non-null
        // and CalloutBoxRenderer.DrawLeaderLine emits a DrawLine call.
        var ctx = new RecordingRenderContext();
        var renderer = new TestCircularRenderer(NewContext(ctx));
        var font = new Font { Size = 11 };
        var candidates = new List<LabelCandidate>
        {
            new(new Point(200, 200), "Slice A", font, TextAlignment.Center),
            new(new Point(200, 200), "Slice B", font, TextAlignment.Center)
        };

        renderer.PlaceOuterLabels(candidates, Bounds);

        Assert.Equal(2, ctx.CountOf("DrawText"));
        Assert.True(ctx.CountOf("DrawLine") > 0, "Expected at least one leader line for displaced labels");
    }
}
