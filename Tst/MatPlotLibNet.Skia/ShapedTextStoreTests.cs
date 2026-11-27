// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A text is shaped once: the width the layout measures and the outline the vector writer draws are read from the one
/// shaping the store keeps of it, by the face and shaper the font resolved to, the size and the text. A layout that measures a
/// label in several passes, and a draw that measures it again, shape it once between them.</summary>
public sealed class ShapedTextStoreTests
{
    private static readonly Font Font13 = new() { Family = "DejaVu Sans", Size = 13 };

    [Fact]
    public void ATextMeasuredTwice_IsShapedOnce()
    {
        var store = new ShapedTextStore();
        var metrics = new SkiaFontMetrics(store);

        var first = metrics.Measure("Latency (ms)", Font13);
        var second = metrics.Measure("Latency (ms)", Font13);

        Assert.Equal(first, second);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void TheWidthMeasured_IsTheWidthOfAShapingMadeAfresh()
    {
        var metrics = new SkiaFontMetrics(new ShapedTextStore());
        _ = metrics.Measure("Ta", Font13);

        var kept = metrics.Measure("Ta", Font13);

        Assert.Equal(SkiaGlyphPathProvider.MeasureAdvance("Ta", Font13), kept.Width);
        Assert.Equal(13 * 1.2, kept.Height);
    }

    [Fact]
    public void TheOutline_ReadsTheShapingTheMeasureKept()
    {
        var store = new ShapedTextStore();
        _ = new SkiaFontMetrics(store).Measure("12:00:30", Font13);

        string? outline = new SkiaGlyphPathProvider(store).GetPathData("12:00:30", Font13);

        Assert.Equal(1, store.Count);
        Assert.Equal(new SkiaGlyphPathProvider().GetPathData("12:00:30", Font13), outline);
    }

    [Fact]
    public void AnEmptyText_IsNoWidth_AndIsNotKept()
    {
        var store = new ShapedTextStore();

        var size = new SkiaFontMetrics(store).Measure("", Font13);

        Assert.Equal(0, size.Width);
        Assert.Equal(13 * 1.2, size.Height);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void AnotherSizeOrFace_IsAnotherShaping()
    {
        var store = new ShapedTextStore();
        var metrics = new SkiaFontMetrics(store);

        _ = metrics.Measure("Ta", Font13);
        _ = metrics.Measure("Ta", Font13 with { Size = 26 });
        _ = metrics.Measure("Ta", Font13 with { Weight = FontWeight.Bold });
        _ = metrics.Measure("Ta", Font13 with { Color = Colors.Red });

        Assert.Equal(3, store.Count);
    }

    [Fact]
    public void TheMetricsAndTheOutlinesTheSkiaPackageInstalls_ShareTheStoreThePackageKeeps()
    {
        var metrics = Assert.IsType<SkiaFontMetrics>(MatPlotLibNet.FontMetricsDefault.Current);
        var outlines = Assert.IsType<SkiaGlyphPathProvider>(MatPlotLibNet.GlyphPathProviderDefault.Current);

        Assert.Same(ShapedTextStore.Shared, metrics.Shaped);
        Assert.Same(ShapedTextStore.Shared, outlines.Shaped);
    }

    [Fact]
    public void TheRenderContextAnApplicationMakes_DrawsFromTheStoreThePackageKeeps()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);

        Assert.Same(ShapedTextStore.Shared, new SkiaRenderContext(canvas).Shaped);
    }

    [Fact]
    public void ALabelDrawnTwice_IsShapedOnce()
    {
        var store = new ShapedTextStore();

        using var first = Draw(store, ctx => ctx.DrawText("Latency (ms)", new Point(4, 30), Font13, TextAlignment.Left));
        using var second = Draw(store, ctx => ctx.DrawText("Latency (ms)", new Point(4, 30), Font13, TextAlignment.Left));

        Assert.Equal(1, store.Count);
        Assert.Equal(first.Bytes, second.Bytes);
    }

    [Fact]
    public void ALabelMeasuredByTheLayout_IsNotShapedAgainWhenItIsDrawn()
    {
        var store = new ShapedTextStore();
        _ = new SkiaFontMetrics(store).Measure("12:00:30", Font13);

        using var drawn = Draw(store, ctx => ctx.DrawText("12:00:30", new Point(60, 30), Font13, TextAlignment.Center, rotation: 30));

        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void ARichTextDrawnTwice_ShapesEachSpanOnce_AtItsOwnSize()
    {
        var store = new ShapedTextStore();
        var text = MathTextParser.Parse("x$^{2}$");

        using var first = Draw(store, ctx => ctx.DrawRichText(text, new Point(10, 30), Font13, TextAlignment.Right));
        using var second = Draw(store, ctx => ctx.DrawRichText(text, new Point(10, 30), Font13, TextAlignment.Right));

        Assert.Equal(2, store.Count);
        Assert.Equal(first.Bytes, second.Bytes);
    }

    [Fact]
    public void ALabelDrawnFromTheKeptShaping_PaintsWhatAFreshShapingPaints()
    {
        var kept = new ShapedTextStore();
        using var warm = Draw(kept, ctx => ctx.DrawText("درجة Ta", new Point(60, 30), Font13, TextAlignment.Center));

        using var fromKept = Draw(kept, ctx => ctx.DrawText("درجة Ta", new Point(60, 30), Font13, TextAlignment.Center));
        using var fresh = Draw(new ShapedTextStore(), ctx => ctx.DrawText("درجة Ta", new Point(60, 30), Font13, TextAlignment.Center));

        Assert.Equal(fresh.Bytes, fromKept.Bytes);
    }

    private static SKBitmap Draw(ShapedTextStore store, Action<SkiaRenderContext> draw)
    {
        var bitmap = new SKBitmap(120, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        draw(new SkiaRenderContext(canvas, store));
        return bitmap;
    }

    [Fact]
    public void ManyThreadsMeasuringAtOnce_AllGetOneWidth_AndOneShapingIsKept()
    {
        var store = new ShapedTextStore();
        var metrics = new SkiaFontMetrics(store);
        var widths = new double[64];

        Parallel.For(0, widths.Length, i => widths[i] = metrics.Measure("concurrent", Font13).Width);

        Assert.All(widths, w => Assert.Equal(widths[0], w));
        Assert.Equal(1, store.Count);
    }
}
