// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>A series that backs a colour bar with its colour scale (<see cref="IColorScaled"/>) says the range of its colours once, in
/// <c>ColourRangeOver</c>: its bar reads it under the figure's theme (<c>ColorBarRangeUnder</c>), and the public
/// <see cref="IColorBarDataProvider.GetColorBarRange"/> every one of them keeps is that range under the library's own theme, never a
/// copy of the rule in the series.</summary>
public class ColorScaledBarRangeTests
{
    /// <summary>Values that hold a zero and a negative number, so the range over a log scale (the values above 0) is not the linear one.</summary>
    private static readonly double[,] Grid = { { -2, 0 }, { 1, 8 } };

    private static readonly double[] Xs = [0, 1, 2, 3, 4, 5], Ys = [0, 1, 0, 1, 0, 1], Zs = [-2, 0, 1, 8, 3, 5];

    public static TheoryData<string, ISeries> Series => new()
    {
        { "Heatmap", new HeatmapSeries(Grid) },
        { "Clustermap", new ClustermapSeries(Grid) },
        { "Pcolormesh", new PcolormeshSeries(new double[] { 0, 1 }, new double[] { 0, 1 }, Grid) },
        { "Image", new ImageSeries(Grid) },
        { "PolarHeatmap", new PolarHeatmapSeries(Grid, 2, 2) },
        { "Histogram2D", new Histogram2DSeries(Xs, Ys, 2, 2) },
        { "Tripcolor", new TripcolorSeries(Xs, Ys, Zs) },
        { "Spectrogram", new SpectrogramSeries(Enumerable.Range(0, 256).Select(i => Math.Sin(i * 0.3)).ToArray()) },
        { "Hexbin", new HexbinSeries(Xs, Ys) },
    };

    [Theory]
    [MemberData(nameof(Series))]
    public void ThePublicRange_IsTheColourRangeUnderTheLibrarysOwnTheme(string name, ISeries series)
    {
        var scaled = (IColorScaled)series;

        Assert.True(((IColorBarDataProvider)series).GetColorBarRange() == scaled.ColourRangeUnder(Theme.Default),
            $"{name}: GetColorBarRange is not its colour range under the library's own theme.");
    }

    [Theory]
    [MemberData(nameof(Series))]
    public void TheBar_NamesTheColourRange_UnderEachTheme(string name, ISeries series)
    {
        foreach (var theme in new[] { Theme.Default, Theme.MatplotlibV2, Theme.MatplotlibClassic })
            Assert.True(((IColorBarDataProvider)series).ColorBarRangeUnder(theme) == ((IColorScaled)series).ColourRangeUnder(theme),
                $"{name}: the bar does not name the colour range under {theme.Name}.");
    }

    [Theory]
    [InlineData("Heatmap")]
    [InlineData("Clustermap")]
    [InlineData("Pcolormesh")]
    [InlineData("Image")]
    [InlineData("PolarHeatmap")]
    [InlineData("Tripcolor")]
    public void OverALogScale_AValueOfZeroOrBelowStandsOutsideTheRange(string name)
    {
        var series = (IColorScaled)Series.Select(row => ((ITheoryDataRow)row).GetData()).Single(row => (string)row[0]! == name)[1]!;

        var log = series.ColourRangeOver(new ColorMapping(ColorMaps.Viridis, LogNorm.Instance, Theme.MatplotlibV2));
        var linear = series.ColourRangeOver(new ColorMapping(ColorMaps.Viridis, LinearNormalizer.Instance, Theme.MatplotlibV2));

        Assert.Equal(-2, linear.Min);
        Assert.Equal(1, log.Min);
        Assert.Equal(8, log.Max);
    }
}
