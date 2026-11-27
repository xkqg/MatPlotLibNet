// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The colour bar of a filled contour is one block per band under the matplotlib themes, with a tick on the levels, as
/// matplotlib's: 3.11.2, <c>Colorbar.__init__</c> for a <c>ContourSet</c> takes <c>boundaries = cs._levels</c>, <c>values =
/// cs.cvalues</c> (the middle of each band) and <c>ticks = ticker.FixedLocator(cs.levels, nbins=10)</c>; the default spacing is
/// uniform, so every band is as tall as the next, and the blocks are one <c>pcolormesh</c> cell each. The library drew a gradient
/// with the ticks of a locator. <c>FixedLocator.tick_values</c> thins more than 10 levels to every <c>step</c>-th, taking the
/// offset whose lowest tick is nearest 0; its rows here are what matplotlib prints for the same levels.</summary>
public sealed class ColorBarContourBandsTests
{
    private static ContourfSeries Peaks()
    {
        var (x, y, z) = SampleSurface.Peaks();
        return new ContourfSeries(x, y, z);
    }

    private static List<CallRecord> BarCalls(Theme theme, ColorBarOrientation orientation = ColorBarOrientation.Vertical)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(Peaks()).WithColorBar(cb => cb with { Orientation = orientation })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return ctx.Calls.GetRange(start + 1, end - start - 1);
    }

    private static List<(Rect Rect, Color Fill)> Blocks(List<CallRecord> bar) =>
        [.. bar.OfType<DrawRectangleCall>().Where(k => k.Fill is not null)
            .Select(c => (c.Rect, c.Fill!.Value))];

    /// <summary>The steps of a gradient grouped by colour: each run of equal colours, from the first step on.</summary>
    private static List<(Color Fill, int Steps)> Runs(List<(Rect Rect, Color Fill)> blocks)
    {
        var runs = new List<(Color, int)>();
        foreach (var (_, fill) in blocks)
            if (runs.Count > 0 && runs[^1].Item1 == fill) runs[^1] = (fill, runs[^1].Item2 + 1);
            else runs.Add((fill, 1));
        return runs;
    }

    private static double[] TickValues(List<CallRecord> bar) =>
        [.. bar.OfType<DrawTextCall>().Select(c => c.Text)
            .Where(text => double.TryParse(text.Replace('−', '-'), out _)).Select(text => double.Parse(text.Replace('−', '-')))];

    /// <summary>The peaks surface asked for 10 levels (-0.8 to 1.2 in steps of 0.2, 11 levels, 10 bands): ten blocks of one colour
    /// each, the colour of the band the fill is painted in (viridis at the middle of its two levels over the levels), and ticks on
    /// every other level, -0.8, -0.4, 0, 0.4, 0.8 and 1.2 (ticker.FixedLocator(levels, nbins=10)).</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_TheBarOfAFilledContour_IsOneBlockPerBand_WithATickOnEveryOtherLevel()
    {
        var bar = BarCalls(Theme.MatplotlibV2);

        var runs = Runs(Blocks(bar));
        Assert.Equal(10, runs.Count);
        Assert.Single(runs.Select(r => r.Steps).Distinct());   // every band as tall as the next
        double[] levels = Peaks().LevelsUnder(Theme.MatplotlibV2)!;
        Assert.Equal(11, levels.Length);
        // The bar runs from the top down; the lowest band is at the bottom, so the runs read from the highest band to the lowest.
        for (int b = 0; b < 10; b++)
        {
            double mid = levels[b] + (levels[b + 1] - levels[b]) * 0.5;
            Assert.Equal(ColorMaps.Viridis.GetColor(LinearNormalizer.Instance.Normalize(mid, levels[0], levels[^1])), runs[9 - b].Fill);
        }

        double[] ticks = TickValues(bar);
        Assert.Equal(6, ticks.Length);
        for (int i = 0; i < 6; i++) Assert.Equal(new[] { -0.8, -0.4, 0.0, 0.4, 0.8, 1.2 }[i], ticks[^(i + 1)], 9);
    }

    /// <summary>Under the library's own theme the bar is what it was: a gradient of 50 steps, with the ticks a locator picks.</summary>
    [Fact]
    public void UnderTheLibrarysOwnTheme_TheBarOfAFilledContour_StaysAGradient()
    {
        var bar = BarCalls(Theme.Default);

        Assert.Equal(50, Blocks(bar).Count);
        Assert.True(Runs(Blocks(bar)).Count > 10);
    }

    /// <summary>A block of the bar is the colour its band is painted in on the plot: the bar and the fill read one colour for a band.</summary>
    [Fact]
    public void TheBlocksOfTheBar_AreTheColoursOfTheBandsOnThePlot()
    {
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(Peaks()).WithColorBar()).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);

        var bar = Runs(Blocks(ctx.Calls.GetRange(start + 1, end - start - 1))).Select(r => r.Fill).ToHashSet();
        var plot = ctx.Calls.Take(start).OfType<ShapeCall>().Where(c => c is DrawRectangleCall or DrawPathCall)
            .Select(c => c.Fill).Where(fill => fill is not null).Select(fill => fill!.Value).ToHashSet();

        Assert.Equal(10, bar.Count);
        Assert.True(bar.IsSubsetOf(plot), "a block of the bar has a colour no band of the plot is painted in");
    }

    [Fact]
    public void AHorizontalBar_IsOneBlockPerBandToo()
    {
        var runs = Runs(Blocks(BarCalls(Theme.MatplotlibClassic, ColorBarOrientation.Horizontal)));

        Assert.Equal(10, runs.Count);
    }

    // ---- the ticks: matplotlib's FixedLocator(levels, nbins=10) ----------------------------------------------------------

    private static ColorBarScale ScaleOf(double[] levels, INormalizer normalizer)
    {
        var ctx = new RecordingRenderContext();
        return ColorBarScale.For(levels[0], levels[^1], 4000, vertical: true, normalizer, ctx, Theme.MatplotlibV2.TickFont(), Theme.Default);
    }

    [Theory]
    [InlineData(new[] { -5.0, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 }, new[] { -4.0, -2, 0, 2, 4 }, new[] { 1, 3, 5, 7, 9 })]
    [InlineData(new[] { 0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new[] { 0.0, 2, 4, 6, 8, 10 }, new[] { 0, 2, 4, 6, 8, 10 })]
    [InlineData(new[] { 0.0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, new[] { 0.0, 2, 4, 6, 8, 10 }, new[] { 0, 2, 4, 6, 8, 10 })]
    [InlineData(new[] { 1.0, 10, 100, 1000, 10000 }, new[] { 1.0, 10, 100, 1000, 10000 }, new[] { 0, 1, 2, 3, 4 })]
    public void MoreThanTenLevels_AreThinnedAsFixedLocatorThinsThem(double[] levels, double[] expected, int[] boundaryIndices)
    {
        var scale = ScaleOf(levels, new ContourBandNormalizer(levels, LinearNormalizer.Instance));

        Assert.Equal(expected, scale.Ticks);
        Assert.Equal(boundaryIndices.Select(k => (double)k / (levels.Length - 1)), scale.Fractions);
        Assert.Equal(levels.Length - 1, scale.Bins);
    }

    /// <summary>21 levels from -10 to 10 are thinned to every third, the offset with the lowest tick nearest 0: -9, -6, ..., 9;
    /// 21 levels from 1 to 21 keep the first offset: 1, 4, ..., 19.</summary>
    [Fact]
    public void ThinningTakesTheOffsetWhoseLowestTickIsNearestZero()
    {
        double[] around = [.. Enumerable.Range(-10, 21).Select(k => (double)k)];
        double[] above = [.. Enumerable.Range(1, 21).Select(k => (double)k)];

        Assert.Equal([-9.0, -6, -3, 0, 3, 6, 9], ScaleOf(around, new ContourBandNormalizer(around, LinearNormalizer.Instance)).Ticks);
        Assert.Equal([1.0, 4, 7, 10, 13, 16, 19], ScaleOf(above, new ContourBandNormalizer(above, LinearNormalizer.Instance)).Ticks);
    }

    /// <summary>A bar of bands that is not a contour's names every boundary, as it did: only a contour's bar asks for the thinning.</summary>
    [Fact]
    public void ABoundaryNormalizersBar_NamesEveryBoundary()
    {
        double[] boundaries = [.. Enumerable.Range(0, 12).Select(k => (double)k)];

        var scale = ScaleOf(boundaries, new BoundaryNormalizer(boundaries));

        Assert.Equal(boundaries, scale.Ticks);
    }

    // ---- a block's colour --------------------------------------------------------------------------------------------------

    /// <summary>A band is drawn at the colour of the middle of its two levels over the levels, whatever the levels' spacing:
    /// <c>cs.cvalues</c> are the layers, <c>norm.autoscale_None(levels)</c> runs the map over the levels.</summary>
    [Fact]
    public void ABlock_IsTheColourOfTheMiddleOfItsBand_OverTheLevels()
    {
        double[] levels = [0, 1, 10];
        var normalizer = new ContourBandNormalizer(levels, LinearNormalizer.Instance);
        var discrete = (IDiscreteNormalizer)normalizer;

        Assert.Equal(0.05, discrete.MapFractionOf(0), 12);
        Assert.Equal(0.55, discrete.MapFractionOf(1), 12);
        Assert.Equal(levels, discrete.Boundaries);
        Assert.Equal(10, discrete.MostNamed);
    }

    /// <summary>A value is in the band whose two levels hold it; one below the first level is in the lowest band and one above the last
    /// in the highest.</summary>
    [Fact]
    public void AValue_NormalisesToTheColourOfItsBand()
    {
        double[] levels = [0, 1, 10];
        INormalizer normalizer = new ContourBandNormalizer(levels, LinearNormalizer.Instance);

        Assert.Equal(0.05, normalizer.Normalize(0.5, 0, 10), 12);
        Assert.Equal(0.55, normalizer.Normalize(1.0, 0, 10), 12);
        Assert.Equal(0.55, normalizer.Normalize(9.9, 0, 10), 12);
        Assert.Equal(0.05, normalizer.Normalize(-3, 0, 10), 12);
        Assert.Equal(0.55, normalizer.Normalize(25, 0, 10), 12);
    }

    /// <summary>A band bar draws a whole number of steps for each band, so a band's edge falls on a step's edge and the block is as
    /// tall as its share: 50 steps asked for over 3 bands are drawn as 51.</summary>
    [Fact]
    public void ABarOfBands_DrawsAWholeNumberOfStepsForEachBand()
    {
        var scale = new ColorBarScale(0, 1, [], [], [], 3);
        var ctx = new RecordingRenderContext();
        new ColorBarStrip(new Rect(0, 0, 300, 20), Vertical: false, ColorBarExtend.Neither, DrawEdges: false, Steps: 50)
            .Draw(ctx, new LinearColorMap("ramp", [Colors.Red, Colors.Blue]), scale, Colors.Black);

        Assert.Equal(51 + 1, ctx.Of<DrawRectangleCall>().Count);   // the steps and the frame
        Assert.Equal(51, scale.StepsOf(50));
        Assert.Equal(50, new ColorBarScale(0, 1, [], [], [], 0).StepsOf(50));
    }

    // ---- the series hands its bar over --------------------------------------------------------------------------------------

    [Fact]
    public void AFilledContour_NamesItsBarOfBands_UnderTheMatplotlibThemesOnly()
    {
        IThemedColorBar series = Peaks();

        var bands = Assert.IsType<ContourBandNormalizer>(series.ColorBarNormalizerUnder(Theme.MatplotlibV2));
        Assert.Equal(((ContourfSeries)series).LevelsUnder(Theme.MatplotlibV2), ((IDiscreteNormalizer)bands).Boundaries);
        Assert.IsType<ContourBandNormalizer>(series.ColorBarNormalizerUnder(Theme.MatplotlibClassic));
        Assert.Null(series.ColorBarNormalizerUnder(Theme.Default));
    }

    /// <summary>The bands' colours run through the series' own normaliser: a contour that names none is linear.</summary>
    [Fact]
    public void TheBarOfBands_ReadsThroughTheNormaliserTheSeriesColoursWith()
    {
        var series = Peaks();
        series.Normalizer = new PowerNormNormalizer(0.5);
        var bands = (IDiscreteNormalizer)((IThemedColorBar)series).ColorBarNormalizerUnder(Theme.MatplotlibV2)!;
        double[] levels = series.LevelsUnder(Theme.MatplotlibV2)!;

        double mid = levels[0] + (levels[1] - levels[0]) * 0.5;
        Assert.Equal(new PowerNormNormalizer(0.5).Normalize(mid, levels[0], levels[^1]), bands.MapFractionOf(0), 12);
    }

    /// <summary>Named levels set the bands as they set the bar's range: -1, 0 and 2 are two blocks, each as tall as the other
    /// (uniform spacing).</summary>
    [Fact]
    public void NamedLevels_AreTheBandsOfTheBar()
    {
        var series = Peaks();
        series.LevelValues = [-1, 0, 2];

        var bands = (IDiscreteNormalizer)((IThemedColorBar)series).ColorBarNormalizerUnder(Theme.MatplotlibV2)!;

        Assert.Equal([-1.0, 0, 2], bands.Boundaries);
    }

    /// <summary>A filled contour that has no two levels to bound a band (a flat grid) keeps the bar it had.</summary>
    [Fact]
    public void AFlatFilledContour_NamesNoBarOfBands()
    {
        IThemedColorBar series = new ContourfSeries([0.0, 1], [0.0, 1], new[,] { { 5.0, 5 }, { 5, 5 } });

        Assert.Null(series.ColorBarNormalizerUnder(Theme.MatplotlibV2));
    }
}
