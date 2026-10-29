// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The volume profile (volume by price, StockCharts' "Volume by Price"): the range of the closes is cut into equal price
/// bins and each bar's volume is added to the bin its close falls in, so the profile says where, in price, the volume traded.
/// A bin takes the closes from its lower edge up to, but not including, its upper edge; the last bin includes its upper edge, so
/// the highest close is counted. It is drawn as horizontal bars on the secondary X axis, which is inverted so the bars grow from
/// the right edge, and which spans four times the longest bar so that bar covers a quarter of the plot.</summary>
public class VolumeProfileTests
{
    private static VolumeProfileResult Profile(double[] close, double[] volume, int bins = 12) =>
        new VolumeProfile(OhlcvBars.Cv(close, volume), bins).Compute();

    // ---- the profile ------------------------------------------------------------------------------------------------------

    /// <summary>Closes 10, 12, 14 and 11 in two bins [10, 12) and [12, 14]: 10 (100) and 11 (400) in the first, 12 (200) and 14
    /// (300) in the second, 500 each.</summary>
    [Fact]
    public void TwoBins_TakeTheVolumeOfTheClosesInThem()
    {
        var profile = Profile([10, 12, 14, 11], [100, 200, 300, 400], bins: 2);

        Assert.Equal([10.0, 12.0, 14.0], profile.BinEdges);
        Assert.Equal([11.0, 13.0], profile.BinCentres);
        Assert.Equal([500.0, 500.0], profile.Volumes);
    }

    /// <summary>A close on an interior edge belongs to the bin above it, and the highest close to the last bin: 12 is the edge
    /// of two bins over 10 to 14 and is counted in [12, 14], with 14.</summary>
    [Fact]
    public void ACloseOnAnEdge_BelongsToTheBinAboveIt_AndTheHighestToTheLastBin()
    {
        var profile = Profile([10, 12, 14], [1, 2, 4], bins: 2);

        Assert.Equal([1.0, 6.0], profile.Volumes);
    }

    /// <summary>Every volume lands in some bin: the profile adds up to the volume of the bars, whatever the closes.</summary>
    [Fact]
    public void TheProfile_AddsUpToTheVolumeOfTheBars()
    {
        var random = new Random(20261029);
        double[] close = [.. Enumerable.Range(0, 300).Select(_ => 100 + random.NextDouble() * 12)];
        double[] volume = [.. Enumerable.Range(0, 300).Select(_ => Math.Round(random.NextDouble() * 9000, 2))];

        foreach (int bins in new[] { 1, 2, 7, 12, 50 })
        {
            var profile = Profile(close, volume, bins);

            Assert.Equal(bins, profile.Volumes.Length);
            Assert.Equal(volume.Sum(), profile.Volumes.Sum(), 6);
        }
    }

    [Fact]
    public void OneBin_TakesEverything()
    {
        var profile = Profile([10, 12, 14, 11], [100, 200, 300, 400], bins: 1);

        Assert.Equal([10.0, 14.0], profile.BinEdges);
        Assert.Equal([12.0], profile.BinCentres);
        Assert.Equal([1000.0], profile.Volumes);
    }

    /// <summary>The bins are equal and cover 10 to 14 from the first edge to the last, whose value is the highest close exactly
    /// (no drift from adding the width twelve times).</summary>
    [Fact]
    public void TheBins_AreEqual_AndSpanTheRangeOfTheCloses()
    {
        var profile = Profile([10.1, 12.7, 13.9, 11.3], [1, 1, 1, 1]);

        Assert.Equal(13, profile.BinEdges.Length);
        Assert.Equal(10.1, profile.BinEdges[0]);
        Assert.Equal(13.9, profile.BinEdges[^1]);
        double width = (13.9 - 10.1) / 12;
        for (int k = 0; k < 12; k++)
        {
            Assert.Equal(profile.BinEdges[k] + width / 2, profile.BinCentres[k], 12);
            if (k > 0) Assert.Equal(width, profile.BinEdges[k] - profile.BinEdges[k - 1], 12);
        }
    }

    /// <summary>All the closes the same: there is no range to cut, so the bins cover one unit centred on the price and the volume
    /// stands in the bin the price falls in (the edge between two bins belongs to the upper one).</summary>
    [Fact]
    public void FlatPrices_ArePutInAUnitRangeAroundThePrice()
    {
        var profile = Profile([5, 5, 5], [10, 20, 30], bins: 4);

        Assert.Equal([4.5, 4.75, 5.0, 5.25, 5.5], profile.BinEdges);
        Assert.Equal([0.0, 0.0, 60.0, 0.0], profile.Volumes);
    }

    /// <summary>A bar with no volume adds nothing to its bin, but its close still takes part in the range.</summary>
    [Fact]
    public void ABarWithNoVolume_AddsNothing()
    {
        var profile = Profile([10, 14, 12], [0, 5, 7], bins: 2);

        Assert.Equal([10.0, 12.0, 14.0], profile.BinEdges);
        Assert.Equal([0.0, 12.0], profile.Volumes);
    }

    /// <summary>A bar whose close or whose volume is missing (NaN) or infinite is not counted, and does not move the range.</summary>
    [Fact]
    public void ABarWithAMissingCloseOrVolume_IsSkipped()
    {
        var profile = Profile([10, double.NaN, 14, 12, 99], [100, 5, 300, double.PositiveInfinity, double.NaN], bins: 2);

        Assert.Equal([10.0, 12.0, 14.0], profile.BinEdges);
        Assert.Equal([100.0, 300.0], profile.Volumes);
    }

    /// <summary>No bar to count: an empty profile, and nothing is drawn.</summary>
    [Fact]
    public void WithNoBarToCount_TheProfileIsEmpty()
    {
        var profile = Profile([double.NaN, 5], [1, double.NaN]);

        Assert.Empty(profile.BinEdges);
        Assert.Empty(profile.BinCentres);
        Assert.Empty(profile.Volumes);
        Assert.Empty(Profile([], []).Volumes);

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.VolumeProfile(OhlcvBars.Cv([double.NaN], [1]))).Build().SubPlots[0];
        Assert.Empty(axes.XSecondarySeries);
    }

    /// <summary>The number of bins is 1 or more.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void BinsBelowOne_AreRefused(int bins)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new VolumeProfile(OhlcvBars.Cv([1, 2], [1, 1]), bins));

        Assert.Equal("bins", error.ParamName);
    }

    [Fact]
    public void TheIndicator_IsNamed_AndTwelveBinsIsTheDefault()
    {
        var indicator = new VolumeProfile(OhlcvBars.Cv([10, 11, 12], [1, 1, 1]));

        Assert.Equal("Volume profile", indicator.Label);
        Assert.Equal(12, indicator.Compute().Volumes.Length);
    }

    // ---- drawn on the axes ------------------------------------------------------------------------------------------------

    private static Figure Drawn(int bins = 2, Action<VolumeProfile>? configure = null) =>
        Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.0, 1, 2, 3], [10.0, 12, 14, 11])
            .SetYLim(10, 14)
            .VolumeProfile(OhlcvBars.Cv([10, 12, 14, 11], [100, 200, 300, 400]), bins, configure))
            .Build();

    /// <summary>The profile is ONE horizontal bar series on the secondary X axis: a bar at each bin centre, as thick as the bin,
    /// as long as the bin's volume; the axis runs from 0 to four times the longest bar and is drawn from right to left.</summary>
    [Fact]
    public void TheProfile_IsOneHorizontalBarSeries_OnTheInvertedSecondaryXAxis()
    {
        var axes = Drawn().SubPlots[0];

        var bars = Assert.IsType<BarSeries>(Assert.Single(axes.XSecondarySeries));
        Assert.Equal(BarOrientation.Horizontal, bars.Orientation);
        Assert.Equal([11.0, 13.0], bars.XCoordinate!);
        Assert.Equal([500.0, 500.0], bars.Values);
        Assert.Equal(2.0, bars.BarWidth);
        Assert.Equal("Volume profile", bars.Label);
        var axis = axes.SecondaryXAxis!;
        Assert.Equal(0.0, axis.Min);
        Assert.Equal(2000.0, axis.Max);
        Assert.True(axis.Inverted);
        Assert.Single(axes.Series);
    }

    /// <summary>Drawn: each bar is as thick as its bin on the price axis (two units of a Y axis fixed at 10 to 14: half the plot),
    /// stands from the right edge of the plot, and the longest covers a quarter of the plot's width.</summary>
    [Fact]
    public void TheBars_AreAsThickAsTheirBin_AndGrowFromTheRightEdge_TheLongestAQuarterAcross()
    {
        var figure = Drawn();
        figure.EnableLegendToggle = true;
        string svg = figure.ToSvg();
        var plot = ChartRendererDefault.Current.ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

        // The filled rectangles of the series group; the clip path of the plot area is the one without a fill.
        var rects = Rects(Group(svg, 1)).Where(rect => rect.Contains("fill=")).ToList();

        Assert.Equal(2, rects.Count);
        foreach (var rect in rects)
        {
            double x = Num(rect, "x"), width = Num(rect, "width"), height = Num(rect, "height");
            Assert.Equal(plot.Right, x + width, 6);
            Assert.Equal(plot.Height / 2, height, 6);
            Assert.Equal(plot.Width / 4, width, 6);
        }
    }

    /// <summary>Drawn with twelve bins the bars are the profile's: as many as the bins that hold volume.</summary>
    [Fact]
    public void TheProfile_SurvivesJson()
    {
        var figure = Drawn(bins: 12);

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
        var axes = ChartSerializerDefault.Current.FromJson(figure.ToJson()).SubPlots[0];
        Assert.True(axes.SecondaryXAxis!.Inverted);
        Assert.IsType<BarSeries>(Assert.Single(axes.XSecondarySeries));
    }

    [Fact]
    public void TheShortcut_RunsItsCallbackOnTheIndicator_Once()
    {
        int calls = 0;

        var axes = Drawn(configure: p => { calls++; p.Label = "where it traded"; }).SubPlots[0];

        Assert.Equal(1, calls);
        Assert.Equal("where it traded", Assert.Single(axes.XSecondarySeries).Label);
    }

    [Fact]
    public void ANamedColour_IsTheColourOfTheBars()
    {
        var axes = Drawn(configure: p => p.Color = Colors.Crimson).SubPlots[0];

        Assert.Equal<Color?>(Colors.Crimson, Assert.IsType<BarSeries>(Assert.Single(axes.XSecondarySeries)).Color);
    }

    /// <summary>The axis spans four times the longest bar; with no bar longer than 0 there is nothing to scale against, so it
    /// spans 0 to 1 rather than to 0.</summary>
    [Fact]
    public void WhenNoVolumeTraded_TheAxisSpansZeroToOne_AndEveryBarIsEmpty()
    {
        var axes = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .VolumeProfile(OhlcvBars.Cv([10, 12, 14, 11], [0, 0, 0, 0]), 2))
            .Build().SubPlots[0];

        Assert.Equal(1.0, axes.SecondaryXAxis!.Max);
        Assert.Equal([0.0, 0.0], Assert.IsType<BarSeries>(Assert.Single(axes.XSecondarySeries)).Values);
    }

    // ---- reading the picture ----------------------------------------------------------------------------------------------

    private static double Num(string rect, string attribute) =>
        double.Parse(Regex.Match(rect, $"""(?<![\w-]){attribute}="([^"]+)" """.TrimEnd()).Groups[1].Value, CultureInfo.InvariantCulture);

    /// <summary>The opening tags of the rectangles in a piece of markup.</summary>
    private static List<string> Rects(string markup) =>
        [.. Regex.Matches(markup, """<rect\b[^>]*>""").Select(match => match.Value)];

    /// <summary>The markup of the series group with index <paramref name="seriesIndex"/>: from its opening tag to the end of the
    /// clipped group inside it (the group closes with two end tags in a row).</summary>
    private static string Group(string svg, int seriesIndex)
    {
        int start = svg.IndexOf($"""<g class="series" data-series-index="{seriesIndex}" """.TrimEnd(), StringComparison.Ordinal);
        Assert.True(start >= 0, $"series group {seriesIndex} is not in the picture");
        var end = Regex.Match(svg[start..], """</g>\s*</g>""");
        Assert.True(end.Success, $"series group {seriesIndex} does not close");
        return svg[start..(start + end.Index)];
    }
}
