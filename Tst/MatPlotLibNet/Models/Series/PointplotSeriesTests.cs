// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PointplotSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class PointplotSeriesTests
{
    /// <summary>seaborn 0.13.2's <c>pointplot</c> draws the confidence interval of the mean (<c>errorbar=("ci", 95)</c>, a bootstrap of the
    /// mean), a few percent of the spread of the observations; the library's own themes keep the half range of the observations' percentiles.
    /// For 0 to 99 (sd 29.011, n 100) the interval of the mean is t(0.975, 99) = 1.98422 times 2.9011 either side, 5.7567 (scipy's
    /// <c>t.ppf</c>), and the percentile range 2.5 to 97.5 is 2.475 to 96.525.</summary>
    [Fact]
    public void TheInterval_UnderTheMatplotlibThemes_IsTheConfidenceIntervalOfTheMean()
    {
        var series = new PointplotSeries([[.. Enumerable.Range(0, 100).Select(i => (double)i)]]);

        var drawn = series.EstimateOf(0, Theme.MatplotlibV2)!.Value;

        Assert.Equal(49.5, drawn.Mean, 9);
        Assert.Equal(5.7567, drawn.Upper - drawn.Mean, 3);
        Assert.Equal(5.7567, drawn.Mean - drawn.Lower, 3);
    }

    [Fact]
    public void TheInterval_UnderTheLibrarysOwnThemes_IsTheRangeOfTheObservationsPercentiles()
    {
        var series = new PointplotSeries([[.. Enumerable.Range(0, 100).Select(i => (double)i)]]);

        var drawn = series.EstimateOf(0, Theme.Default)!.Value;

        Assert.Equal(47.025, drawn.Upper - drawn.Mean, 3);
        Assert.Equal(47.025, drawn.Mean - drawn.Lower, 3);
    }

    [Fact]
    public void ADatasetWithoutObservations_HasNoEstimate_AndALoneObservationAnIntervalOfNoLength()
    {
        var series = new PointplotSeries([[], [5.0]]);

        Assert.Null(series.EstimateOf(0, Theme.MatplotlibV2));
        Assert.Equal(new PointEstimate(5, 5, 5), series.EstimateOf(1, Theme.MatplotlibV2));
        Assert.Equal(new PointEstimate(5, 5, 5), series.EstimateOf(1, Theme.Default));
    }

    /// <summary>seaborn 0.13.2, printed with python: the lines of a point plot are <c>lines.linewidth * 1.8</c> points wide (1.8 under
    /// classic, 2.7 under the default style) and have no caps (<c>capsize=0</c>); the means are joined by a line (<c>ax.lines[0]</c>, four
    /// vertices). The library's own themes keep 1.5 pixels, caps 0.2 wide and no line.</summary>
    [Fact]
    public void TheLines_UnderTheMatplotlibThemes_AreSeabornsAndUnderTheLibrarysOwnThemesTheSeriesOwn()
    {
        var series = new PointplotSeries([[1.0, 2.0]]);

        Assert.Equal(2.7 * 100 / 72, Theme.MatplotlibV2.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, Theme.MatplotlibV2.Looks.Dots.PointplotLine), 9);
        Assert.Equal(1.8 * 100 / 72, Theme.MatplotlibClassic.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, Theme.MatplotlibClassic.Looks.Dots.PointplotLine), 9);
        Assert.Equal(1.5, Theme.Default.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, Theme.Default.Looks.Dots.PointplotLine));
        Assert.Equal(0, series.CapSizeIn(Theme.MatplotlibV2));
        Assert.Equal(0, series.CapSizeIn(Theme.MatplotlibClassic));
        Assert.Equal(0.2, series.CapSizeIn(Theme.Default));
        Assert.True(series.IsJoinedIn(Theme.MatplotlibV2));
        Assert.True(series.IsJoinedIn(Theme.MatplotlibClassic));
        Assert.False(series.IsJoinedIn(Theme.Default));
    }

    [Fact]
    public void AGivenWidthCapAndJoin_AreDrawnAsGivenUnderEveryTheme_AndWrittenToTheJson()
    {
        var series = new PointplotSeries([[1.0, 2.0]]) { LineWidth = 1.5, CapSize = 0.2, JoinPoints = false };

        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.MatplotlibClassic, Theme.Default })
        {
            Assert.Equal(1.5, theme.SeriesSlot(0).LineWidthOf(series.GivenLineWidth, theme.Looks.Dots.PointplotLine));
            Assert.Equal(0.2, series.CapSizeIn(theme));
            Assert.False(series.IsJoinedIn(theme));
        }

        var dto = series.ToSeriesDto();
        Assert.Equal(1.5, dto.LineWidth);
        Assert.Equal(0.2, dto.CapSize);
        Assert.False(dto.JoinPoints);
    }

    /// <summary>seaborn's axes end at the lines: its y limits are the lowest end of an interval and the highest, padded by the style
    /// (matplotlib 3.11.2 prints (-0.261, 3.486) for four groups of means 0.09 to 3.03), where the library's own themes range over every
    /// observation.</summary>
    [Fact]
    public void TheYRange_UnderTheMatplotlibThemes_IsTheIntervalsAndUnderTheLibrarysOwnThemesTheObservations()
    {
        var series = new PointplotSeries([[.. Enumerable.Range(0, 100).Select(i => (double)i)], [200.0, 201.0, 202.0]]);

        var seaborn = ((IThemedRange)series).RangeUnder(null!, Theme.MatplotlibV2);
        var own = ((IThemedRange)series).RangeUnder(null!, Theme.Default);

        Assert.Equal(49.5 - 5.7567, seaborn.YMin!.Value, 3);
        Assert.Equal(201 + StudentTHalf(3, 1.0), seaborn.YMax!.Value, 3);
        Assert.Equal(0, own.YMin);
        Assert.Equal(202, own.YMax);
        Assert.Equal(-0.5, seaborn.XMin);
        Assert.Equal(1.5, seaborn.XMax);
    }

    /// <summary>The critical value of t a series keeps is keyed by what it is made from: a changed level, or a group of another size, is
    /// worked out again, and a series that kept the value of an earlier level answers as a fresh one does.</summary>
    [Fact]
    public void TheKeptCriticalValue_FollowsTheLevelAndTheSizeOfTheGroup()
    {
        double[] ten = [.. Enumerable.Range(0, 10).Select(i => (double)i)], fifty = [.. Enumerable.Range(0, 50).Select(i => (double)i)];
        var reused = new PointplotSeries([ten, fifty]);
        double AtLevel(PointplotSeries series, int group, double level)
        {
            series.ConfidenceLevel = level;
            return series.EstimateOf(group, Theme.MatplotlibV2)!.Value.Upper;
        }

        double first = AtLevel(reused, 0, 0.95);
        double narrower = AtLevel(reused, 0, 0.80);
        double largerGroup = AtLevel(reused, 1, 0.80);

        Assert.Equal(AtLevel(new PointplotSeries([ten, fifty]), 0, 0.95), first);
        Assert.Equal(AtLevel(new PointplotSeries([ten, fifty]), 0, 0.80), narrower);
        Assert.Equal(AtLevel(new PointplotSeries([ten, fifty]), 1, 0.80), largerGroup);
        Assert.True(narrower < first);
    }

    [Fact]
    public void TheYRange_UnderTheMatplotlibThemes_WithNoObservationToDrawAnIntervalFor_IsTheSeriesOwn()
    {
        var series = new PointplotSeries([[], []]);

        Assert.Equal(series.ComputeDataRange(null!), ((IThemedRange)series).RangeUnder(null!, Theme.MatplotlibV2));
    }

    /// <summary>t(0.975, n - 1) * sd / sqrt(n) for n values of standard deviation <paramref name="sd"/>; t(0.975, 2) = 4.30265.</summary>
    private static double StudentTHalf(int n, double sd) => 4.302653 * sd / Math.Sqrt(n);
}
