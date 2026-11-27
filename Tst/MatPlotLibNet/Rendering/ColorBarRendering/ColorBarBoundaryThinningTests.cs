// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The bar of a <see cref="BoundaryNormalizer"/> names its boundaries as matplotlib's does under the matplotlib themes:
/// 3.11.2, <c>colorbar.py _get_ticker_locator_formatter</c> gives a <c>BoundaryNorm</c> <c>ticker.FixedLocator(b, nbins=10)</c>, the
/// one a contour's bar has, so more than ten boundaries are thinned to every <c>step</c>-th (12 boundaries from 0 to 11 name 0, 2,
/// ..., 10: six ticks). The library's own themes name every boundary, as they always did.</summary>
public sealed class ColorBarBoundaryThinningTests
{
    private static readonly double[] Boundaries = [.. Enumerable.Range(0, 12).Select(k => (double)k)];

    private static double[] TickValues(Theme theme, bool figureBar)
    {
        var data = new double[,] { { 0.5, 3.5, 6.5, 10.5 }, { 1.5, 4.5, 8.5, 9.5 } };
        var figure = figureBar
            ? Plt.Create().WithTheme(theme).WithSize(600, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, s => s.Normalizer = new BoundaryNormalizer(Boundaries))).WithColorBar().Build()
            : Plt.Create().WithTheme(theme).WithSize(600, 400)
                .AddSubPlot(1, 1, 1, ax => ax.Heatmap(data, s => s.Normalizer = new BoundaryNormalizer(Boundaries)).WithColorBar()).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        string group = figureBar ? "figure-colorbar" : "colorbar";
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall k && k.CssClass == group);
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return [.. ctx.Calls.GetRange(start + 1, end - start - 1).OfType<DrawTextCall>().Select(c => c.Text)
            .Where(text => double.TryParse(text.Replace('−', '-'), out _)).Select(text => double.Parse(text.Replace('−', '-'))).Order()];
    }

    [Theory]
    [InlineData("matplotlib-v2", false)]
    [InlineData("matplotlib-classic", false)]
    [InlineData("matplotlib-v2", true)]
    public void UnderTheMatplotlibThemes_MoreThanTenBoundaries_AreThinned(string name, bool figureBar) =>
        Assert.Equal([0.0, 2, 4, 6, 8, 10], TickValues(name.AsTheme(), figureBar));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnderTheLibrarysOwnTheme_EveryBoundaryIsNamed(bool figureBar) =>
        Assert.Equal(Boundaries, TickValues(Theme.Default, figureBar));

    /// <summary>The bar of the matplotlib themes asks the normaliser for the same ten a contour's bar is held to, and the normaliser
    /// it hands over colours every value as the series' own does.</summary>
    [Fact]
    public void TheBarsNormaliser_NamesAtMostTheTenAContourBarNames_AndColoursAsTheSeriesDoes()
    {
        var own = new BoundaryNormalizer(Boundaries);
        var thinned = Assert.IsType<BoundaryNormalizer>(new HeatmapSeries(new double[,] { { 1 } }) { Normalizer = own }.ColorBarNormalizerUnder(Theme.MatplotlibV2));

        Assert.Equal(IDiscreteNormalizer.MostTicks, ((IDiscreteNormalizer)thinned).MostNamed);
        Assert.Equal(((IDiscreteNormalizer)new ContourBandNormalizer(Boundaries, LinearNormalizer.Instance)).MostNamed, ((IDiscreteNormalizer)thinned).MostNamed);
        Assert.Equal(int.MaxValue, ((IDiscreteNormalizer)own).MostNamed);
        Assert.All(new[] { -1.0, 0.5, 5.2, 10.9, 20 }, value => Assert.Equal(own.Normalize(value, 0, 11), thinned.Normalize(value, 0, 11)));
        Assert.Same(own, new HeatmapSeries(new double[,] { { 1 } }) { Normalizer = own }.ColorBarNormalizerUnder(Theme.Default));
    }
}
