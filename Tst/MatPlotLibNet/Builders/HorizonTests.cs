// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A horizon chart in one call: a series folded into bands of increasing depth, each clamp(y - k x h, 0, h) of
/// the one before, all in the group's own colour — one hue for a non-negative series, a second for the negative part
/// of a series that goes below zero.</summary>
public class HorizonTests
{
    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(double[] x, double[] y, int bands = 3, Action<HorizonParts>? configure = null) =>
        AxesOf(ax => ax.Horizon(x, y, bands, configure));

    // ---- what the verb adds ----------------------------------------------------------------------------------------

    /// <summary>A non-negative series folds into exactly <c>bands</c> area series, all positive, none negative.</summary>
    [Fact]
    public void ANonNegativeSeries_FoldsIntoBandsAreaSeries_AllPositive()
    {
        var axes = Chart([0.0, 1, 2, 3], [0.0, 1, 2, 9], bands: 3);

        Assert.Equal(3, axes.Series.Count);
        Assert.All(axes.Series, s => Assert.IsType<AreaSeries>(s));
    }

    /// <summary>Band k of a non-negative series is clamp(y - k x h, 0, h), where h is the largest value over the number
    /// of bands: for [0, 1, 2, 9] over 3 bands, h = 3, so band 0 clamps to [0, 3], band 1 to the next 3, band 2 to the
    /// last 3.</summary>
    [Fact]
    public void EachBand_IsTheClampedFoldOfTheValue_NotAModulus()
    {
        var axes = Chart([0.0, 1, 2, 3], [0.0, 1, 2, 9], bands: 3);
        var band0 = (AreaSeries)axes.Series[0];
        var band1 = (AreaSeries)axes.Series[1];
        var band2 = (AreaSeries)axes.Series[2];

        // h = max(9) / 3 = 3.
        Assert.Equal([0.0, 1, 2, 3], band0.YData);     // clamp(y, 0, 3)
        Assert.Equal([0.0, 0, 0, 3], band1.YData);     // clamp(y-3, 0, 3)
        Assert.Equal([0.0, 0, 0, 3], band2.YData);     // clamp(y-6, 0, 3)
    }

    /// <summary>A series with a negative value folds into <c>bands</c> positive area series AND <c>bands</c> negative
    /// ones, the negative folds mirroring the positive formula below zero.</summary>
    [Fact]
    public void ASignedSeries_AlsoFoldsItsNegativePart_BelowZero()
    {
        var axes = Chart([0.0, 1, 2], [3.0, -3, 0], bands: 2);

        Assert.Equal(4, axes.Series.Count);
        var positive0 = (AreaSeries)axes.Series[0];
        var positive1 = (AreaSeries)axes.Series[1];
        var negative0 = (AreaSeries)axes.Series[2];
        var negative1 = (AreaSeries)axes.Series[3];

        // h = max(|3|, |-3|, |0|) / 2 = 1.5.
        Assert.Equal([1.5, 0, 0], positive0.YData);
        Assert.Equal([1.5, 0, 0], positive1.YData);
        Assert.Equal([0.0, -1.5, 0], negative0.YData);
        Assert.Equal([0.0, -1.5, 0], negative1.YData);
    }

    /// <summary>Each fold is drawn deeper than the one before it: band k's Alpha is (k + 1) / bands, so the last band
    /// (the deepest) is fully opaque.</summary>
    [Fact]
    public void EachBand_IsDeeperThanTheLast()
    {
        var axes = Chart([0.0, 1, 2, 3], [0.0, 1, 2, 9], bands: 3);

        Assert.Equal(1.0 / 3, ((AreaSeries)axes.Series[0]).Alpha, 1e-12);
        Assert.Equal(2.0 / 3, ((AreaSeries)axes.Series[1]).Alpha, 1e-12);
        Assert.Equal(1.0, ((AreaSeries)axes.Series[2]).Alpha, 1e-12);
    }

    /// <summary>The positive bands share one colour group, the negative bands a second one: both wear the theme's
    /// cycle, but they are not the same colour as each other.</summary>
    [Fact]
    public void ThePositiveAndTheNegativeBands_EachShareTheirOwnColourGroup()
    {
        var axes = Chart([0.0, 1, 2], [3.0, -3, 0], bands: 2);

        Assert.Equal(0, ((ChartSeries)axes.Series[0]).ColorGroup);
        Assert.Equal(0, ((ChartSeries)axes.Series[1]).ColorGroup);
        Assert.Equal(1, ((ChartSeries)axes.Series[2]).ColorGroup);
        Assert.Equal(1, ((ChartSeries)axes.Series[3]).ColorGroup);
    }

    /// <summary>No band carries a label: a fold is a view of one series, not a unit of its own to name in a
    /// legend.</summary>
    [Fact]
    public void NoBand_CarriesALabel()
    {
        var axes = Chart([0.0, 1, 2, 3], [0.0, 1, 2, 9]);

        Assert.All(axes.Series, s => Assert.Null(s.Label));
        Assert.Empty(axes.LegendEntries(directLabelsDrawn: false));
    }

    /// <summary>A missing value (NaN) leaves a gap in every band at that point, never a zero.</summary>
    [Fact]
    public void AMissingValue_LeavesAGapInEveryBand()
    {
        var axes = Chart([0.0, 1, 2], [1.0, double.NaN, 2], bands: 2);

        Assert.All(axes.Series, s => Assert.True(double.IsNaN(((AreaSeries)s).YData[1])));
    }

    // ---- the callback -----------------------------------------------------------------------------------------------

    /// <summary>The callback runs once, after the verb has built every band, and hands over the very series on the
    /// axes, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOnce_OnThePartsOnTheAxes_AfterTheVerbBuiltThem()
    {
        HorizonParts? seen = null;

        var axes = Chart([0.0, 1, 2], [3.0, -3, 0], bands: 2, parts => seen = parts);

        Assert.NotNull(seen);
        Assert.Equal(2, seen.Value.Positive.Count);
        Assert.Equal(2, seen.Value.Negative.Count);
        Assert.Same(axes.Series[0], seen.Value.Positive[0]);
        Assert.Same(axes.Series[2], seen.Value.Negative[0]);
    }

    /// <summary>A non-negative series' callback hands over an empty negative list, never null.</summary>
    [Fact]
    public void ANonNegativeSeries_HandsOverAnEmptyNegativeList()
    {
        HorizonParts? seen = null;

        Chart([0.0, 1, 2, 3], [0.0, 1, 2, 9], configure: parts => seen = parts);

        Assert.NotNull(seen);
        Assert.Empty(seen.Value.Negative);
    }

    // ---- the hand recipe --------------------------------------------------------------------------------------------

    /// <summary>The verb draws exactly the picture of the hand recipe made of the public building blocks, with and
    /// without a missing observation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("horizon").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.HorizonByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>A horizon chart is its parts in JSON: read back it draws the same picture. It is not folded again.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_SurvivesJson(bool missing)
    {
        var figure = ComposedChartShapes.Named("horizon").Build(missing).Build();

        string drawn = figure.ToSvg();
        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- refusals -----------------------------------------------------------------------------------------------

    /// <summary>A missing argument is refused by name.</summary>
    [Fact]
    public void MissingArguments_AreRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Horizon([0.0], [0.0])).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Chart(null!, [0.0])).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Chart([0.0], null!)).ParamName);
    }

    /// <summary>x and y of different lengths are refused.</summary>
    [Fact]
    public void XAndYOfDifferentLengths_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([0.0, 1], [0.0]));

        Assert.Equal("y", error.ParamName);
    }

    /// <summary>A series with no finite value (empty, or every value NaN or infinite) has no height to fold by.</summary>
    [Fact]
    public void ASeriesWithNoFiniteValue_IsRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([0.0, 1], [double.NaN, double.NaN]));

        Assert.Equal("y", error.ParamName);
    }

    /// <summary>Fewer than one band is refused.</summary>
    [Fact]
    public void FewerThanOneBand_IsRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([0.0, 1], [1.0, 2], bands: 0));

        Assert.Equal("bands", error.ParamName);
    }
}
