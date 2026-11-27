// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The clip of a region on any surface (<see cref="RenderContextScopeExtensions"/>): a surface that clips to an outline is
/// handed the outline, any other surface the box around it, and a region with no outline is its rectangle everywhere.</summary>
public class RenderContextClipScopeTests
{
    private static readonly Rect Box = new(10, 20, 100, 50);
    private static readonly PathSegment[] Triangle =
        [new MoveToSegment(new(60, 20)), new LineToSegment(new(10, 70)), new LineToSegment(new(110, 70)), new CloseSegment()];

    [Fact]
    public void ASurfaceThatClipsToAnOutline_IsHandedTheOutline()
    {
        var ctx = new RecordingRenderContext();

        using (ctx.ClippedTo(new ClipRegion(Box, Triangle)))
        {
        }

        var call = Assert.IsType<PushClipOutlineCall>(ctx.Calls[0]);
        Assert.Equal(Triangle, call.Outline);
    }

    [Fact]
    public void ARegionWithNoOutline_IsItsRectangle()
    {
        var ctx = new RecordingRenderContext();

        using (ctx.ClippedTo(new ClipRegion(Box)))
        {
        }

        Assert.Equal(Box, (Rect)Assert.Single(ctx.Of<PushClipCall>()).ClipRect);
        Assert.Equal(0, ctx.Count<PushClipOutlineCall>());
    }

    [Fact]
    public void ASurfaceOfYourOwn_ClipsToTheBoxAroundTheOutline()
    {
        var ctx = DispatchProxy.Create<IRenderContext, ClipLog>();

        using (ctx.ClippedTo(new ClipRegion(Box, Triangle)))
        {
        }

        Assert.Equal([Box], ((ClipLog)(object)ctx).Rects);
    }

    [Fact]
    public void TheSvg_WritesTheOutlineAsTheClipPath_AndOpensAGroupInIt()
    {
        var ctx = new SvgRenderContext();

        using (ctx.ClippedTo(new ClipRegion(Box, Triangle)))
        {
        }

        Assert.Equal(
            "<defs><clipPath id=\"clip-0\"><path d=\"M 60 20 L 10 70 L 110 70 Z \" /></clipPath></defs>\n<g clip-path=\"url(#clip-0)\">\n</g>\n",
            ctx.GetOutput());
    }

    [Fact]
    public void TheSvg_WritesARectangleClipAsItAlwaysHas()
    {
        var ctx = new SvgRenderContext();

        using var clip = ctx.ClippedTo(new ClipRegion(Box));

        Assert.StartsWith("<defs><clipPath id=\"clip-0\"><rect x=\"10\" y=\"20\" width=\"100\" height=\"50\" /></clipPath></defs>\n",
            ctx.GetOutput());
    }

    /// <summary>A surface of your own is not told to cut its texts (it draws a text as it draws it), and its scope puts nothing back.</summary>
    [Fact]
    public void ASurfaceOfYourOwn_IsNotToldToCutItsTexts()
    {
        var ctx = DispatchProxy.Create<IRenderContext, ClipLog>();

        var scope = ctx.CuttingTexts();
        scope.Dispose();

        Assert.Equal(default, scope);
    }

    /// <summary>A surface cuts its texts while the scope lasts and as it did before once the scope ends, so a scope inside another leaves
    /// the outer one cutting.</summary>
    [Fact]
    public void AScopeThatEnds_PutsBackWhetherTheSurfaceCutItsTexts()
    {
        var ctx = new SvgRenderContext();
        ITextClipRenderContext texts = ctx;

        using (ctx.CuttingTexts())
        {
            using (ctx.CuttingTexts())
            {
                Assert.True(texts.CutsTexts);
            }

            Assert.True(texts.CutsTexts);
        }

        Assert.False(texts.CutsTexts);
    }

    /// <summary>A surface that clips to rectangles alone, recording each.</summary>
    public class ClipLog : DispatchProxy
    {
        public List<Rect> Rects { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IRenderContext.PushClip) && args is [Rect rect]) Rects.Add(rect);
            return null;
        }
    }
}
