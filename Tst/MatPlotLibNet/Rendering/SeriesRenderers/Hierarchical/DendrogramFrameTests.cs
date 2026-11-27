// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers.Hierarchical;

/// <summary>Where a dendrogram stands in its plot: one <see cref="DendrogramFrame"/> for each
/// <see cref="DendrogramOrientation"/>, chosen in one place, that maps a leaf and a merge distance to pixels, turns a pair of them
/// into a point, and places a leaf's label.</summary>
public class DendrogramFrameTests
{
    private static readonly Rect Bounds = new(100, 50, 400, 200);   // right 500, bottom 250
    private static readonly Font LabelFont = new() { Family = "sans-serif", Size = 10 };

    [Theory]
    [InlineData(DendrogramOrientation.Top, typeof(TopDendrogramFrame))]
    [InlineData(DendrogramOrientation.Bottom, typeof(BottomDendrogramFrame))]
    [InlineData(DendrogramOrientation.Left, typeof(LeftDendrogramFrame))]
    [InlineData(DendrogramOrientation.Right, typeof(RightDendrogramFrame))]
    [InlineData((DendrogramOrientation)99, typeof(RightDendrogramFrame))]
    public void EveryOrientation_HasItsFrame(DendrogramOrientation orientation, Type frame) =>
        Assert.IsType(frame, DendrogramFrame.Of(orientation));

    [Fact]
    public void AnOrientationIsResolvedToOneFrame_TheSameEveryTimeItIsAskedFor()
    {
        foreach (DendrogramOrientation orientation in Enum.GetValues<DendrogramOrientation>())
            Assert.Same(DendrogramFrame.Of(orientation), DendrogramFrame.Of(orientation));
    }

    // ── The leaves spread along X for an upright tree, along Y for a tree on its side ──

    [Theory]
    [InlineData(DendrogramOrientation.Top, 150.0)]       // x + (0 + 0.5) / 4 * 400
    [InlineData(DendrogramOrientation.Bottom, 150.0)]
    [InlineData(DendrogramOrientation.Left, 75.0)]       // y + (0 + 0.5) / 4 * 200
    [InlineData(DendrogramOrientation.Right, 75.0)]
    public void TheFirstOfFourLeaves_StandsAnEighthAlongTheLeafAxis(DendrogramOrientation orientation, double expected) =>
        Assert.Equal(expected, DendrogramFrame.Of(orientation).LeafPixel(0, Bounds, 4));

    [Theory]
    [InlineData(DendrogramOrientation.Top, 300.0)]
    [InlineData(DendrogramOrientation.Left, 150.0)]
    public void ALoneLeaf_StandsInTheMiddleOfTheLeafAxis(DendrogramOrientation orientation, double expected) =>
        Assert.Equal(expected, DendrogramFrame.Of(orientation).LeafPixel(0, Bounds, 1));

    [Theory]
    [InlineData(DendrogramOrientation.Top, 200.0)]       // bottom - 0.25 * height: the root rises to the top
    [InlineData(DendrogramOrientation.Bottom, 100.0)]    // y + 0.25 * height: the root hangs to the bottom
    [InlineData(DendrogramOrientation.Left, 400.0)]      // right - 0.25 * width
    [InlineData(DendrogramOrientation.Right, 200.0)]     // x + 0.25 * width
    public void AMergeAQuarterOfTheHighest_StandsAQuarterAlongTheMergeAxis_FromWhereTheLeavesAre(
        DendrogramOrientation orientation, double expected) =>
        Assert.Equal(expected, DendrogramFrame.Of(orientation).MergePixel(1, Bounds, 4));

    [Fact]
    public void AnUprightTreeTakesTheLeafAsX_ATreeOnItsSideTakesItAsY()
    {
        Assert.Equal(new Point(7, 3), DendrogramFrame.Of(DendrogramOrientation.Top).At(mergePx: 3, leafPx: 7));
        Assert.Equal(new Point(7, 3), DendrogramFrame.Of(DendrogramOrientation.Bottom).At(mergePx: 3, leafPx: 7));
        Assert.Equal(new Point(3, 7), DendrogramFrame.Of(DendrogramOrientation.Left).At(mergePx: 3, leafPx: 7));
        Assert.Equal(new Point(3, 7), DendrogramFrame.Of(DendrogramOrientation.Right).At(mergePx: 3, leafPx: 7));
    }

    // ── The label of a leaf stands beyond the leaf, away from the tree ─────────

    [Theory]
    [InlineData(DendrogramOrientation.Top, 40.0, 76.0, TextAlignment.Center, 0.0)]    // below: baseline + 6 + font size
    [InlineData(DendrogramOrientation.Bottom, 40.0, 54.0, TextAlignment.Center, 0.0)] // above: baseline - 6
    [InlineData(DendrogramOrientation.Left, 66.0, 40.0, TextAlignment.Left, 90.0)]    // right of it, turned
    [InlineData(DendrogramOrientation.Right, 54.0, 40.0, TextAlignment.Right, 90.0)]  // left of it, turned
    public void ALeafLabel_StandsSixPixelsBeyondTheLeaves(DendrogramOrientation orientation, double x, double y,
        TextAlignment alignment, double rotation)
    {
        var ctx = new RecordingRenderContext();

        DendrogramFrame.Of(orientation).DrawLabel(ctx, "leaf", leafPx: 40, mergeBaselinePx: 60, LabelFont);

        var call = Assert.IsType<DrawTextCall>(Assert.Single(ctx.Calls));
        Assert.Equal("leaf", call.Text);
        Assert.Equal(new Point(x, y), call.Position);
        Assert.Equal(alignment, call.Alignment);
        Assert.Equal(rotation, call.Rotation);
    }
}
