// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A figure keeps the trees of the math texts its draws read (<see cref="KeptMathTexts"/>), so a label drawn again is not parsed
/// again: within a draw a text is parsed once, a text the draw before read is taken over, and a text no draw read any more is let go of
/// after one draw. The drawing sequence tells the store to each surface with the looks, and a label reads it when it is parsed.</summary>
public sealed class KeptMathTextsTests
{
    [Fact]
    public void ATextReadTwiceInADraw_IsParsedOnce()
    {
        var texts = new KeptMathTexts();
        texts.StartDraw();

        var first = TextLabel.Of("$x^2$").Parsed(texts).Tree;

        Assert.NotNull(first);
        Assert.Same(first, TextLabel.Of("$x^2$").Parsed(texts).Tree);
    }

    [Fact]
    public void ATextTheDrawBeforeRead_IsTheSameTreeInTheNextDraw()
    {
        var texts = new KeptMathTexts();
        texts.StartDraw();
        var first = TextLabel.Of("$\\alpha$").Parsed(texts).Tree;

        texts.StartDraw();
        var again = TextLabel.Of("$\\alpha$").Parsed(texts).Tree;
        texts.StartDraw();

        Assert.Same(first, again);
        Assert.Same(first, TextLabel.Of("$\\alpha$").Parsed(texts).Tree);
    }

    /// <summary>A text one whole draw did not read (a title that changed, a tick label gone) is parsed anew when it comes back: the store
    /// holds no more than the texts of two draws.</summary>
    [Fact]
    public void ATextADrawDidNotRead_IsLetGoOfAfterThatDraw()
    {
        var texts = new KeptMathTexts();
        texts.StartDraw();
        var first = TextLabel.Of("$\\beta$").Parsed(texts).Tree;

        texts.StartDraw();
        texts.StartDraw();

        Assert.NotSame(first, TextLabel.Of("$\\beta$").Parsed(texts).Tree);
    }

    [Fact]
    public void APlainText_IsNeitherParsedNorKept()
    {
        var texts = new KeptMathTexts();
        texts.StartDraw();

        Assert.Null(TextLabel.Of("price").Parsed(texts).Tree);
        Assert.False(texts.TryTake("price", out _));
    }

    /// <summary>Two parses of one text that meet in the store (two draws of one figure at once) keep the first: every reader after
    /// them reads one tree.</summary>
    [Fact]
    public void ATreeKeptTwice_KeepsTheFirst()
    {
        var texts = new KeptMathTexts();
        var first = MathTextCursor.Parse("$x$");

        Assert.Same(first, texts.Keep("$x$", first));
        Assert.Same(first, texts.Keep("$x$", MathTextCursor.Parse("$x$")));
    }

    /// <summary>The repro of the frame that parsed every label it measured (23 parses for 17 labels, every frame): a figure drawn again
    /// reads the trees its draw before read, whichever surface it is drawn on.</summary>
    [Fact]
    public void AFigureDrawnAgain_ReadsTheTreesItsDrawBeforeRead()
    {
        double[] x = [0, 1, 2], y = [1, 2, 0];
        var figure = Plt.Create().WithTitle("$\\alpha_i^2$")
            .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y, line => line.Label = "$\\sqrt{x}$").WithTitle("$\\frac{a}{b}$").SetXLabel("$\\beta$").WithLegend())
            .Build();
        string[] labels = ["$\\alpha_i^2$", "$\\sqrt{x}$", "$\\frac{a}{b}$", "$\\beta$"];

        _ = figure.ToSvg();
        var first = labels.Select(text => figure.MathTexts.TryTake(text, out var tree) ? tree : null).ToArray();
        _ = figure.ToSvg();
        var again = labels.Select(text => figure.MathTexts.TryTake(text, out var tree) ? tree : null).ToArray();

        Assert.All(first, Assert.NotNull);
        for (int i = 0; i < labels.Length; i++) Assert.Same(first[i], again[i]);
    }

    /// <summary>Every stage of a draw tells its surface the texts its figure keeps, as it tells the looks.</summary>
    [Fact]
    public void EveryStageOfADraw_TellsTheSurfaceTheTextsTheFigureKeeps()
    {
        var figure = Plt.Create().WithTitle("$x$").Build();
        var ctx = new SvgRenderContext();

        new ChartRenderer().Render(figure, ctx);

        Assert.Same(figure.MathTexts, ctx.MathTextsTold);
    }

    /// <summary>The mark collector measures as the surface it collects for, so it reads the texts that surface was told.</summary>
    [Fact]
    public void TheMarkCollector_ReadsTheTextsOfTheSurfaceItMeasuresWith()
    {
        var surface = new SvgRenderContext();
        var collector = new MarkCollectingContext(surface, new MarkObstacles.Builder());
        var texts = new KeptMathTexts();

        collector.UseStageOf(texts, Theme.Default);

        Assert.Same(texts, surface.MathTextsTold);
        Assert.Same(texts, collector.MathTextsTold);
    }
}
