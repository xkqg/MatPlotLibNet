// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The ticks of a colour bar stand at round numbers, asked of a tick locator as an axis asks one, and each stands
/// where its value is along the gradient. The numbers are worked out by hand for a plot of 400 by 300 at (100, 50) under
/// <see cref="Theme.Default"/>, whose tick font is 13 px: a vertical bar is 300 px long and a horizontal one 400 px, so
/// the locator may return up to 9 ticks (300 / (2 · 13) and 400 / (3 · 13) both come to more than 9).</summary>
public class ColorBarTickTests
{
    private static readonly IColorMap Map = ColorMaps.Viridis;
    private static readonly Theme DefaultTheme = Theme.Default;
    private static readonly Rect Plot = new(100, 50, 400, 300);

    private static RecordingRenderContext Draw(ColorBar cb, double min, double max, Rect? plot = null)
    {
        var ctx = new RecordingRenderContext();
        ColorBarRendererFactory.Create(cb, Map, min, max, plot ?? Plot, ctx, DefaultTheme).Render();
        return ctx;
    }

    /// <summary>The tick labels in drawing order: the text and where it stands, the bar's own label left out.</summary>
    private static List<(string Text, Point At)> Ticks(RecordingRenderContext ctx, string? barLabel = null) =>
        [.. ctx.OfKind("DrawText")
            .Select(c => (Text: (string)((dynamic)c.Args!).text, At: (Point)((dynamic)c.Args!).position))
            .Where(t => t.Text != barLabel)];

    // ---- the ticks of 0..11 and 0..10 ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(ColorBarOrientation.Horizontal)]
    [InlineData(ColorBarOrientation.Vertical)]
    public void ARangeOf0To11_HasTheTicks0To10InTwos_NotFifthsOfItsRange(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Orientation = orientation }, 0, 11);

        Assert.Equal(["0", "2", "4", "6", "8", "10"], Ticks(ctx).Select(t => t.Text).OrderBy(double.Parse).ToArray());
    }

    [Theory]
    [InlineData(ColorBarOrientation.Horizontal)]
    [InlineData(ColorBarOrientation.Vertical)]
    public void ARangeThatIsRoundToBeginWith_KeepsItsTicks(ColorBarOrientation orientation)
    {
        var ctx = Draw(new ColorBar { Orientation = orientation }, 0, 10);

        Assert.Equal(["0", "2", "4", "6", "8", "10"], Ticks(ctx).Select(t => t.Text).OrderBy(double.Parse).ToArray());
    }

    // ---- where a tick stands -------------------------------------------------------------------------------------------

    [Fact]
    public void Horizontally_ATickStandsAtItsValueAlongTheBar()
    {
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Horizontal }, 0, 11));

        // The bar runs from x = 100 over 400 px; the tick of v stands at 100 + 400 · v / 11.
        foreach (var (text, at) in ticks)
            Assert.Equal(100 + 400 * double.Parse(text) / 11, at.X, 9);
    }

    [Fact]
    public void Vertically_ATickStandsAtItsValueAlongTheBar_TheLargestAtTheTop()
    {
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical }, 0, 11));

        // The bar runs from y = 50 over 300 px with the largest value on top; a label's baseline is 4 px below its mark.
        foreach (var (text, at) in ticks)
            Assert.Equal(50 + 300 * (1 - double.Parse(text) / 11) + 4, at.Y, 9);
    }

    [Theory]
    [InlineData(ColorBarExtend.Min)]
    [InlineData(ColorBarExtend.Max)]
    [InlineData(ColorBarExtend.Both)]
    public void WithAWedgeAtAnEnd_TheTicksStandOverTheGradientAndNotOverTheWholeBar_Vertically(ColorBarExtend extend)
    {
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical, Extend = extend }, 0, 11));

        // A wedge is a tenth of the bar (30 px), and the gradient is what is left; the value 0 stands at its lower end.
        double gradientTop = 50 + (extend is ColorBarExtend.Max or ColorBarExtend.Both ? 30 : 0);
        double gradientHeight = 300 - (extend is ColorBarExtend.Min or ColorBarExtend.Both ? 30 : 0) - (extend is ColorBarExtend.Max or ColorBarExtend.Both ? 30 : 0);
        foreach (var (text, at) in ticks)
            Assert.Equal(gradientTop + gradientHeight * (1 - double.Parse(text) / 11) + 4, at.Y, 9);
        Assert.Equal(gradientTop + gradientHeight + 4, ticks.Single(t => t.Text == "0").At.Y, 9);
    }

    [Theory]
    [InlineData(ColorBarExtend.Min)]
    [InlineData(ColorBarExtend.Max)]
    [InlineData(ColorBarExtend.Both)]
    public void WithAWedgeAtAnEnd_TheTicksStandOverTheGradientAndNotOverTheWholeBar_Horizontally(ColorBarExtend extend)
    {
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Horizontal, Extend = extend }, 0, 11));

        // The bar is 400 px; a wedge is 40 px of it.
        double gradientLeft = 100 + (extend is ColorBarExtend.Min or ColorBarExtend.Both ? 40 : 0);
        double gradientWidth = 400 - (extend is ColorBarExtend.Min or ColorBarExtend.Both ? 40 : 0) - (extend is ColorBarExtend.Max or ColorBarExtend.Both ? 40 : 0);
        foreach (var (text, at) in ticks)
            Assert.Equal(gradientLeft + gradientWidth * double.Parse(text) / 11, at.X, 9);
    }

    [Fact]
    public void ARangeFromHighToLow_PutsTheLowValueAtTheEndOfTheBarWhereTheColourMapEnds()
    {
        // Min 11 and Max 0: the gradient still runs from Min (the bar's start, the bottom) to Max (its end, the top).
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical }, 11, 0));

        Assert.Equal(["0", "2", "4", "6", "8", "10"], ticks.Select(t => t.Text).OrderBy(double.Parse).ToArray());
        Assert.Equal(50 + 4, ticks.Single(t => t.Text == "0").At.Y, 9);            // 0 is the Max, on top
        Assert.Equal(50 + 300 * (1 - 1.0 / 11) + 4, ticks.Single(t => t.Text == "10").At.Y, 9);
    }

    // ---- a short bar and a range with no ticks -------------------------------------------------------------------------

    [Fact]
    public void AShortBar_HasFewerTicks_AndNoTwoLabelsTouch()
    {
        // 15 % of 300 px = 45 px: 45 / (2 · 13) is 1, held up to 2 ticks.
        var ticks = Ticks(Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical, Shrink = 0.15 }, 0, 11));

        Assert.InRange(ticks.Count, 2, 3);
        var ys = ticks.Select(t => t.At.Y).Order().ToArray();
        for (int i = 1; i < ys.Length; i++) Assert.True(ys[i] - ys[i - 1] >= 13, $"labels {ys[i - 1]} and {ys[i]} are closer than a label is tall");
    }

    [Theory]
    [InlineData(3.0, 3.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.NegativeInfinity, double.PositiveInfinity)]
    public void ARangeThatCannotBeDrawn_HasNoTick_AndNothingUndrawable(double min, double max)
    {
        var ctx = Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical }, min, max);

        Assert.Empty(Ticks(ctx));
    }

    // ---- the label of the bar stands past the ticks ---------------------------------------------------------------------

    [Fact]
    public void TheBarsOwnLabel_StandsPastTheWidestTick()
    {
        var ctx = Draw(new ColorBar { Orientation = ColorBarOrientation.Vertical, Label = "Intensity" }, 0, 11);

        var all = ctx.OfKind("DrawText").Select(c => (Text: (string)((dynamic)c.Args!).text, At: (Point)((dynamic)c.Args!).position)).ToList();
        var label = all.Single(t => t.Text == "Intensity");
        double widest = all.Where(t => t.Text != "Intensity").Max(t => ctx.MeasureText(t.Text, ThemedFontProvider.TickFont(DefaultTheme)).Width);
        Assert.Equal(100 + 400 + 10 + 20 + 4 + widest + 8, label.At.X, 9);
    }

    // ---- the colour bar of a figure --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(ColorBarOrientation.Vertical)]
    [InlineData(ColorBarOrientation.Horizontal)]
    public void TheFigureColorBar_HasTheTicksOfTheAxesColorBar(ColorBarOrientation orientation)
    {
        double[,] data = { { 0, 11 }, { 5, 8 } };
        var figure = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => ax.Heatmap(data)).Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().RenderFigureColorBar(figure, [new Rect(80, 40, 600, 400)],
            new ColorBar { Visible = true, Orientation = orientation }, ctx);

        var texts = ctx.OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text).ToArray();
        Assert.Equal(["0", "2", "4", "6", "8", "10"], texts.OrderBy(double.Parse).ToArray());
    }
}
