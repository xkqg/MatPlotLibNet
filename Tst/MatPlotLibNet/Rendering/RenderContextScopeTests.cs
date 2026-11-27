// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What is set on a drawing surface for a group of marks, an opacity or a clip, is set by a scope that puts it back:
/// <c>using (ctx.WithOpacity(a))</c> and <c>using (ctx.ClippedTo(region))</c>. The reset is not typed beside every set, so a mark that
/// throws, or a path that leaves early, cannot leave a surface at half opacity or inside a clip.</summary>
public class RenderContextScopeTests
{
    private static readonly Rect Box = new(10, 20, 100, 50);
    private static readonly Point[] Triangle = [new(0, 0), new(5, 5), new(10, 0)];

    /// <summary>A mark that cannot be drawn, inside <paramref name="scope"/>.</summary>
    private static void DrawAMarkThatThrows(IRenderContext ctx, IDisposable scope)
    {
        using (scope)
        {
            throw new InvalidOperationException("a mark that cannot be drawn on " + ctx);
        }
    }

    private static List<string> Kinds(RecordingRenderContext ctx) => [.. ctx.Calls.Select(c => c.Kind)];

    // ── an opacity ──────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnOpacity_IsSetAtOnce_AndPutBackToFullWhenTheScopeEnds()
    {
        var ctx = new RecordingRenderContext();

        using (ctx.WithOpacity(0.4))
        {
            ctx.DrawPolygon(Triangle, ShapeStyle.Filled(Colors.Red));
        }

        Assert.Equal(["SetOpacity", "DrawPolygon", "SetOpacity"], Kinds(ctx));
        Assert.Equal(0.4, ((SetOpacityCall)ctx.Calls[0]).Opacity);
        Assert.Equal(1.0, ((SetOpacityCall)ctx.Calls[2]).Opacity);
    }

    /// <summary>A scope puts back the opacity it replaced, not full opacity: a group drawn at an opacity inside a group drawn at another
    /// leaves the outer group at its own when it ends, on a surface that tells its opacity (the library's own surfaces). A surface of
    /// your own, which tells none, is put back to full, as before.</summary>
    [Fact]
    public void AnInnerOpacity_PutsBackTheOuterOne_NotFullOpacity()
    {
        var ctx = new SvgRenderContext();

        using (ctx.WithOpacity(0.5))
        {
            using (ctx.WithOpacity(0.3)) ctx.DrawPolygon(Triangle, ShapeStyle.Filled(Colors.Red));
            Assert.Equal(0.5, ctx.OpacityTold);
            ctx.DrawPolygon(Triangle, ShapeStyle.Filled(Colors.Blue));
        }

        Assert.Equal(1.0, ctx.OpacityTold);
        string svg = ctx.GetOutput();
        int blue = svg.IndexOf("#0000FF", StringComparison.Ordinal);
        Assert.True(svg.LastIndexOf("<g opacity=\"0.5\">", blue, StringComparison.Ordinal) > svg.LastIndexOf("<g opacity=\"0.3\">", blue, StringComparison.Ordinal));
    }

    [Fact]
    public void ASurfaceThatTellsNoOpacity_IsPutBackToFull()
    {
        var ctx = new RecordingRenderContext();

        using (ctx.WithOpacity(0.5))
        {
            using (ctx.WithOpacity(0.3)) { }
        }

        Assert.Equal(1.0, ctx.OpacityTold);
        Assert.Equal(1.0, ((SetOpacityCall)ctx.Calls[2]).Opacity);
    }

    [Fact]
    public void AnOpacity_IsPutBack_WhenAMarkThrows()
    {
        var ctx = new RecordingRenderContext();

        Assert.Throws<InvalidOperationException>(() => DrawAMarkThatThrows(ctx, ctx.WithOpacity(0.4)));

        Assert.Equal(1.0, ((SetOpacityCall)ctx.Calls[^1]).Opacity);
    }

    [Fact]
    public void AScopeThatWasNeverOpened_PutsNothingBack()
    {
        OpacityScope none = default;

        Assert.Null(Record.Exception(none.Dispose));
    }

    [Fact]
    public void AnOpacityOfFull_IsStillSetAndPutBack_AsTheSurfaceHasAlwaysBeenTold()
    {
        // An SVG surface lifts the clips it has applied at every SetOpacity, so a call left out would move bytes.
        var ctx = new RecordingRenderContext();

        using (ctx.WithOpacity(1.0))
        {
        }

        Assert.Equal(["SetOpacity", "SetOpacity"], Kinds(ctx));
    }

    /// <summary>A group drawn at an opacity only below full (an image's cells, a filled contour's bands) asks one member: below full it is
    /// the scope, at full it tells the surface nothing at all.</summary>
    [Theory]
    [InlineData(0.4, 2)]
    [InlineData(1.0, 0)]
    public void AnOpacityBelowFull_OpensTheScope_AndFullOpacityTellsNothing(double opacity, int calls)
    {
        var ctx = new RecordingRenderContext();

        using (ctx.WithOpacityBelowFull(opacity))
        {
        }

        Assert.Equal(calls, Kinds(ctx).Count(kind => kind == "SetOpacity"));
    }

    /// <summary>A stage of a figure starts at full opacity whatever the stage before it left on the surface: one member says so.</summary>
    [Fact]
    public void AStage_StartsAtFullOpacity()
    {
        var ctx = new RecordingRenderContext();
        ctx.SetOpacity(0.3);

        ctx.AtFullOpacity();

        Assert.Equal(1.0, ((SetOpacityCall)ctx.Calls[^1]).Opacity);
    }

    // ── a clip ──────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARectangleClip_IsPushedAtOnce_AndPoppedWhenTheScopeEnds()
    {
        var ctx = new RecordingRenderContext();

        using (ctx.ClippedTo(Box))
        {
            ctx.DrawPolygon(Triangle, ShapeStyle.Filled(Colors.Red));
        }

        Assert.Equal(["PushClip", "DrawPolygon", "PopClip"], Kinds(ctx));
        Assert.Equal(Box, ((PushClipCall)ctx.Calls[0]).ClipRect);
    }

    [Fact]
    public void ARegionWithAnOutline_ClipsToTheOutlineOnASurfaceThatCan()
    {
        var ctx = new RecordingRenderContext();
        PathSegment[] outline = [new MoveToSegment(new(60, 20)), new LineToSegment(new(10, 70)), new CloseSegment()];

        using (ctx.ClippedTo(new ClipRegion(Box, outline)))
        {
        }

        Assert.Equal(["PushClipOutline", "PopClip"], Kinds(ctx));
    }

    [Fact]
    public void AClip_IsPopped_WhenAMarkThrows()
    {
        var ctx = new RecordingRenderContext();

        Assert.Throws<InvalidOperationException>(() => DrawAMarkThatThrows(ctx, ctx.ClippedTo(Box)));

        Assert.Equal(["PushClip", "PopClip"], Kinds(ctx));
    }

    [Fact]
    public void AClipThatWasNeverOpened_PopsNothing()
    {
        var ctx = new RecordingRenderContext();
        ClipScope none = default;

        none.Dispose();

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void TheSvg_ClosesTheGroupOfAClip_WhenTheScopeEnds()
    {
        var ctx = new SvgRenderContext();

        using (ctx.ClippedTo(Box))
        {
        }

        Assert.Equal(
            "<defs><clipPath id=\"clip-0\"><rect x=\"10\" y=\"20\" width=\"100\" height=\"50\" /></clipPath></defs>\n<g clip-path=\"url(#clip-0)\">\n</g>\n",
            ctx.GetOutput());
    }
}
