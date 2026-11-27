// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What a series renderer does with labels that collide: it places them in one batch
/// (<c>SeriesRenderer.PlaceLabels</c>), draws a leader line for any that moved, and draws each text
/// (<c>SeriesRenderer.DrawPlacedLabels</c>). The pie and the sunburst draw their outer labels this way.</summary>
public class PlacedLabelsTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 400);

    private static SeriesRenderContext NewContext(RecordingRenderContext ctx) =>
        new(new DataTransform(0, 100, 0, 100, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx));

    // A minimal concrete renderer so the protected members can be reached.
    private sealed class LabelRenderer : SeriesRenderer<PieSeries>
    {
        public LabelRenderer(SeriesRenderContext ctx) : base(ctx) { }
        public override void Render(PieSeries series) { }
        public void DrawOuterLabels(IReadOnlyList<LabelCandidate> candidates, Rect bounds) =>
            DrawPlacedLabels(PlaceLabels(candidates, bounds));
    }

    [Fact]
    public void EmptyCandidates_DrawNothing()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LabelRenderer(NewContext(ctx));

        renderer.DrawOuterLabels([], Bounds);

        Assert.Equal(0, ctx.Count<DrawTextCall>());
    }

    [Fact]
    public void SingleCandidate_DrawsOneLabel()
    {
        var ctx = new RecordingRenderContext();
        var renderer = new LabelRenderer(NewContext(ctx));
        var font = new Font { Size = 11 };
        var candidates = new List<LabelCandidate>
        {
            new(new Point(200, 100), "Slice A", font, TextAlignment.Center)
        };

        renderer.DrawOuterLabels(candidates, Bounds);

        Assert.Equal(1, ctx.Count<DrawTextCall>());
    }

    [Fact]
    public void OverlappingCandidates_DrawALeaderLine()
    {
        // Two labels placed at the same point force a collision-resolution displacement
        // of more than the default 6px threshold, so LeaderLineStart becomes non-null
        // and the leader line is drawn as a DrawLine call.
        var ctx = new RecordingRenderContext();
        var renderer = new LabelRenderer(NewContext(ctx));
        var font = new Font { Size = 14 };   // the recording surface measures a line as tall as its size: 7 px each way, past the 6 px threshold
        var candidates = new List<LabelCandidate>
        {
            new(new Point(200, 200), "Slice A", font, TextAlignment.Center),
            new(new Point(200, 200), "Slice B", font, TextAlignment.Center)
        };

        renderer.DrawOuterLabels(candidates, Bounds);

        Assert.Equal(2, ctx.Count<DrawTextCall>());
        Assert.True(ctx.Count<DrawLineCall>() > 0, "Expected at least one leader line for displaced labels");
    }
}
