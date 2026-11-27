// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The canvas is told the math texts its figure keeps, as it is told the looks, and measures the box of a text by the installed
/// metrics: a picture drawn again reads the trees its draw before read.</summary>
[Collection("SkiaFontsGlobalState")]
public sealed class SkiaKeptMathTextsTests
{
    [Fact]
    public void TheCanvas_TakesTheMathTextsOfAStage_AndMeasuresByTheInstalledMetrics()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);
        var texts = new KeptMathTexts();

        Assert.Null(ctx.MathTextsTold);
        ctx.UseStageOf(texts, Theme.MatplotlibClassic);

        Assert.Same(texts, ctx.MathTextsTold);
        Assert.Same(FontMetricsDefault.Current, ctx.BoxMetricsTold);
    }

    /// <summary>The canvas says the opacity it was set to, which a scope of opacity reads to put it back.</summary>
    [Fact]
    public void TheCanvas_SaysTheOpacityItWasSetTo()
    {
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        IRenderContext ctx = new SkiaRenderContext(canvas);

        ctx.SetOpacity(0.4);

        Assert.Equal(0.4, ctx.OpacityTold);
    }

    [Fact]
    public void APictureDrawnAgain_ReadsTheTreesItsDrawBeforeRead()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).WithTitle("$\\alpha_i^2$")
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [1.0, 0.0]).WithTitle("$\\frac{a}{b}$"))
            .Build();

        _ = figure.ToPng();
        Assert.True(figure.MathTexts.TryTake("$\\alpha_i^2$", out var title));
        Assert.True(figure.MathTexts.TryTake("$\\frac{a}{b}$", out var axes));
        _ = figure.ToPng();

        Assert.True(figure.MathTexts.TryTake("$\\alpha_i^2$", out var titleAgain));
        Assert.True(figure.MathTexts.TryTake("$\\frac{a}{b}$", out var axesAgain));
        Assert.Same(title, titleAgain);
        Assert.Same(axes, axesAgain);
    }
}
