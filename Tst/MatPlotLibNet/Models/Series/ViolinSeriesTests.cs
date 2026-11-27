// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ViolinSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class ViolinSeriesTests
{
    /// <summary>The library's rule for a violin's body: Silverman's bandwidth on matplotlib's 100 heights.</summary>
    private static readonly DensityRule Silverman = new(GaussianKde.SilvermanFactor, ViolinSeries.Heights);

    // ── the range follows where the violins are drawn ──────────────────────────────────────────────────────────────
    // A sample whose values are all equal puts every observation inside the window at the lowest heights, so the body is as
    // wide as it can be: Widths on each side of the position. That makes the extents below exact by hand.

    private static IAxesContext NoLimits => new TestAxesContext(null, null);

    private static readonly double[] Spike = [5.0, 5.0, 5.0, 5.0];

    /// <summary>A violin placed by Positions ranges over where it is drawn, not over 0..n-1: measured before the fix, a single
    /// violin at position 3 still reported -1 to 1, so it was drawn off the plot.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_RangesOverTheViolinAtItsPosition()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(2.5, range.XMin!.Value, 12);
        Assert.Equal(3.5, range.XMax!.Value, 12);
    }

    /// <summary>The body of a half violin reaches one side of its position only (High: right, Low: left), so the range
    /// does not make room on the side that is not drawn.</summary>
    [Theory]
    [InlineData(ViolinSide.Both, 2.5, 3.5)]
    [InlineData(ViolinSide.High, 3.0, 3.5)]
    [InlineData(ViolinSide.Low, 2.5, 3.0)]
    public void ComputeDataRange_WithPositions_IsSideAware(ViolinSide side, double xMin, double xMax)
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], ShowExtrema = false, Side = side }.ComputeDataRange(NoLimits);

        Assert.Equal(xMin, range.XMin!.Value, 12);
        Assert.Equal(xMax, range.XMax!.Value, 12);
    }

    /// <summary>The ticks at the extrema stand 0.3 widths to either side of the position whichever side the body is drawn
    /// on, so a half violin that shows them reaches that far on the side it does not draw.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_ReachesTheExtremaTicksOnBothSides()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3], Side = ViolinSide.High }.ComputeDataRange(NoLimits);

        Assert.Equal(3 - 0.3 * 0.5, range.XMin!.Value, 12);
        Assert.Equal(3.5, range.XMax!.Value, 12);
    }

    /// <summary>The body reaches a whole width at its widest (the density is scaled by its peak), and the median and mean lines
    /// are half a width across: they lie inside the body, so they never widen the room a violin is drawn in. Before the body was a
    /// density, a sample with nothing near any one height had a body of a tenth of the width and the lines (a quarter) were wider.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ComputeDataRange_WithPositions_ReachesTheBodyAtItsWidest_WhichTheMedianAndMeanLinesLieInside(bool medians, bool means)
    {
        var series = new ViolinSeries([[1.0, 2, 3, 4, 5]])
            { Positions = [3], ShowExtrema = false, ShowMedians = medians, ShowMeans = means };

        var range = series.ComputeDataRange(NoLimits);

        Assert.Equal(3 - 0.5, range.XMin!.Value, 12);
        Assert.Equal(3 + 0.5, range.XMax!.Value, 12);
    }

    /// <summary>A violin wider than the default room is not cut by it: at a width of 3 the body reaches 3 either side.</summary>
    [Fact]
    public void ComputeDataRange_WithoutPositions_WidensForAViolinWiderThanTheDefaultRoom()
    {
        var range = new ViolinSeries([Spike]) { Widths = 3, ShowExtrema = false }.ComputeDataRange(NoLimits);

        Assert.Equal(-3, range.XMin!.Value, 12);
        Assert.Equal(3, range.XMax!.Value, 12);
    }

    /// <summary>A limit set on the X axis stays the range, whatever the positions.</summary>
    [Fact]
    public void ComputeDataRange_WithPositions_HonoursContextXAxisLimits()
    {
        var range = new ViolinSeries([Spike]) { Positions = [3] }.ComputeDataRange(new TestAxesContext(-2, 9));

        Assert.Equal(-2, range.XMin);
        Assert.Equal(9, range.XMax);
    }

    /// <summary>Each violin is drawn from its drawable observations, ascending, and the outline of its body is the density of
    /// that sample at the series' width; a dataset with nothing to draw has an empty sample and no outline.</summary>
    [Fact]
    public void TheBodies_AreTheSortedSamplesAndTheirOutlines_OnePerDataset()
    {
        var series = new ViolinSeries([[3.0, double.NaN, 1, 2], [double.NaN], [5.0, 4]]) { Widths = 0.7 };

        var bodies = series.BodiesUnder(ThemeLooks.Library);

        Assert.Equal(3, bodies.Length);
        Assert.Equal(new[] { 1.0, 2, 3 }, bodies[0].Sample);
        Assert.Equal(ViolinSeries.Outline(new[] { 1.0, 2, 3 }, 0.7, Silverman), bodies[0].Outline);
        Assert.Empty(bodies[1].Sample);
        Assert.Empty(bodies[1].Outline);
        Assert.Equal(new[] { 4.0, 5 }, bodies[2].Sample);
        Assert.Equal(ViolinSeries.Outline(new[] { 4.0, 5 }, 0.7, Silverman), bodies[2].Outline);
    }

    /// <summary>The range of the axes and the renderer both read the bodies, so a draw asks for them at least twice: the series
    /// estimates them once and keeps them.</summary>
    [Fact]
    public void TheBodies_AskedTwice_AreEstimatedOnce()
    {
        var series = new ViolinSeries([[1.0, 2, 2.5, 3, 7], [4.0, 5, 6]]);

        Assert.Same(series.BodiesUnder(ThemeLooks.Library), series.BodiesUnder(ThemeLooks.Library));
    }

    /// <summary>A figure drawn again with its series unchanged (here its title changed, so the subplot is drawn) reads the
    /// bodies the draw before estimated: one density estimate per violin, not one per draw.</summary>
    [Fact]
    public void TheBodies_ADrawUsed_AreTheOnesTheNextDrawUses()
    {
        ViolinSeries series = null!;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Violin([[1.0, 2, 2.5, 3, 7], [4.0, 5, 6]], s => series = s)).Build();
        string first = figure.ToSvg();
        var drawn = series.BodiesUnder(figure.Theme.Looks);

        figure.SubPlots[0].Title = "drawn again";
        string second = figure.ToSvg();

        Assert.Same(drawn, series.BodiesUnder(figure.Theme.Looks));
        Assert.NotEqual(first, second);
    }

    /// <summary>A value changed in place passes no setter, so the change mark does not move; the bodies are estimated again all
    /// the same, as a fresh series estimates them, because the copy of the data they were estimated from no longer matches.</summary>
    [Fact]
    public void TheBodies_AfterAValueChangedInPlace_AreEstimatedAgain()
    {
        double[][] data = [[1.0, 2, 2.5, 3, 7], [4.0, 5, 6]];
        var series = new ViolinSeries(data);
        var before = series.BodiesUnder(ThemeLooks.Library);

        data[1][0] = -3;
        var after = series.BodiesUnder(ThemeLooks.Library);

        Assert.NotSame(before, after);
        var fresh = new ViolinSeries([[1.0, 2, 2.5, 3, 7], [-3.0, 5, 6]]).BodiesUnder(ThemeLooks.Library);
        Assert.Equal(fresh[1].Sample, after[1].Sample);
        Assert.Equal(fresh[1].Outline, after[1].Outline);
    }

    /// <summary>The width moves the series' change mark, and the bodies are estimated again at the new width.</summary>
    [Fact]
    public void TheBodies_AfterTheWidthChanged_AreEstimatedAgain()
    {
        var series = new ViolinSeries([[1.0, 2, 2.5, 3, 7]]);
        var before = series.BodiesUnder(ThemeLooks.Library);

        series.Widths = 0.9;
        var after = series.BodiesUnder(ThemeLooks.Library);

        Assert.NotSame(before, after);
        Assert.Equal(0.9, after[0].Outline.Max(slice => slice.HalfWidth), 12);
    }

    // ---- the outline of a violin's body (Outline) ----
    // The outline of a violin's body: how wide it is at each height. The renderer draws it and the series ranges over it, so the
    // width is decided here once. The outline is the sample's Gaussian kernel density, the same estimator the density curve and
    // the ridgeline are drawn with (Silverman's bandwidth), read at 100 heights from the lowest value to the highest as
    // matplotlib's <c>violinplot</c> reads its estimate (<c>points=100</c>), and scaled so that the violin's widest point is the
    // width: a violin of several samples has the same widest point whatever the sizes of the samples. <para>Measured before the
    // change: the width was the share of the sample within a twentieth of the extent of a height, a count in a window and not a
    // density, so on 12 observations the outline had five bumps and pinched to no width inside the body.</para>


    private static readonly double[] Twelve = [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4];

    private static int LocalMaxima(IReadOnlyList<ViolinSlice> outline) =>
        Enumerable.Range(1, outline.Count - 2).Count(i => outline[i].HalfWidth > outline[i - 1].HalfWidth && outline[i].HalfWidth >= outline[i + 1].HalfWidth);

    [Fact]
    public void TheOutline_StandsAtOneHundredHeights_FromTheLowestValueToTheHighest()
    {
        var outline = ViolinSeries.Outline(new[] { 1.0, 2, 3, 4, 5 }, 0.5, Silverman);

        Assert.Equal(ViolinSeries.Heights, outline.Length);
        Assert.Equal(100, outline.Length);
        Assert.Equal(1, outline[0].Y);
        Assert.Equal(5, outline[^1].Y);
        Assert.Equal(1 + 4.0 / 99, outline[1].Y, 12);
    }

    /// <summary>Twelve observations, the sample of the raincloud example: one smooth hump, where the window count had five bumps
    /// with the body pinched to no width between them.</summary>
    [Fact]
    public void TheOutline_OfTwelveObservations_IsOneHump()
    {
        var outline = ViolinSeries.Outline(Twelve, 0.5, Silverman);

        Assert.Equal(1, LocalMaxima(outline));
        Assert.All(outline, slice => Assert.True(slice.HalfWidth > 0, $"the body is pinched to no width at {slice.Y}"));
    }

    /// <summary>The violin is scaled by its own peak: its widest half-width is the width the series says, whatever the sample.</summary>
    [Theory]
    [InlineData(0.5)]
    [InlineData(2.0)]
    public void TheWidestHalfWidth_IsTheWidth(double widths)
    {
        Assert.Equal(widths, ViolinSeries.Outline(Twelve, widths, Silverman).Max(slice => slice.HalfWidth), 12);
        Assert.Equal(widths, ViolinSeries.Outline(new[] { 1.0, 2, 3, 4, 12 }, widths, Silverman).Max(slice => slice.HalfWidth), 12);
    }

    /// <summary>The half-width at a height is the Gaussian density of the sample there over the density at the highest height,
    /// times the width, with Silverman's bandwidth: checked here against the formula written out once more, to 1e-12.</summary>
    [Fact]
    public void TheHalfWidth_IsTheKernelDensityOverItsPeak_TimesTheWidth()
    {
        double[] sample = [1, 2, 3, 4, 12];
        double n = sample.Length;
        double mean = sample.Average();
        double sd = Math.Sqrt(sample.Sum(v => (v - mean) * (v - mean)) / (n - 1));
        double h = 1.06 * sd * Math.Pow(n, -0.2);
        Assert.Equal(3.3751301062875134, h, 12);

        double Density(double y) => sample.Sum(v => Math.Exp(-0.5 * Math.Pow((y - v) / h, 2))) / (n * h * Math.Sqrt(2 * Math.PI));
        double[] ys = [.. Enumerable.Range(0, 100).Select(i => 1 + 11.0 * i / 99)];
        double peak = ys.Max(Density);

        var outline = ViolinSeries.Outline(sample, 0.5, Silverman);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(ys[i], outline[i].Y, 12);
            Assert.Equal(0.5 * Density(ys[i]) / peak, outline[i].HalfWidth, 12);
        }
    }

    [Fact]
    public void TheHalfWidth_ScalesWithTheWidth()
    {
        var narrow = ViolinSeries.Outline(new[] { 1.0, 2, 3, 4, 5 }, 0.5, Silverman);
        var wide = ViolinSeries.Outline(new[] { 1.0, 2, 3, 4, 5 }, 2, Silverman);

        for (int i = 0; i < narrow.Length; i++) Assert.Equal(4 * narrow[i].HalfWidth, wide[i].HalfWidth, 12);
    }

    /// <summary>Two sizes of one shape are the same violin: the density is divided by its peak, not by the number of observations.</summary>
    [Fact]
    public void TheSampleSize_DoesNotChangeTheWidestPoint()
    {
        double[] small = [1, 2, 3];
        double[] large = [.. small.SelectMany(v => Enumerable.Repeat(v, 50))];

        Assert.Equal(ViolinSeries.Outline(small, 0.5, Silverman).Max(s => s.HalfWidth), ViolinSeries.Outline(large, 0.5, Silverman).Max(s => s.HalfWidth), 9);
    }

    /// <summary>A sample without a spread (one observation, or every value the same) has no density to draw: its body is the
    /// spike matplotlib draws, the whole width at its value, at every height. The window count ran it from the value up to the
    /// value plus 1.</summary>
    [Theory]
    [InlineData(5.0)]
    [InlineData(5.0, 5.0, 5.0, 5.0)]
    public void ASampleWithoutASpread_IsASpikeAtItsValue(params double[] sample)
    {
        var outline = ViolinSeries.Outline(sample, 0.5, Silverman);

        Assert.Equal(100, outline.Length);
        Assert.All(outline, slice => Assert.Equal(new ViolinSlice(5, 0.5), slice));
    }

    /// <summary>Two clumps are two humps with a dip between, a density and not a pair of separate bars: the bandwidth (4.05 for
    /// ten units apart) smooths them into each other.</summary>
    [Fact]
    public void TwoClumps_MakeTwoHumps_AndADipBetween()
    {
        var outline = ViolinSeries.Outline(new[] { 0.0, 0, 0, 10, 10, 10 }, 1, Silverman);

        Assert.Equal(2, LocalMaxima(outline));
        double dip = outline.Min(slice => slice.HalfWidth);
        Assert.True(dip > 0 && dip < 1, $"the dip is {dip}");
    }

    /// <summary>The violin has no kernel or bandwidth to choose: it reads the library's one Gaussian estimator at its one
    /// bandwidth rule, as the density curve does. A setting for either would be a second way to draw the same density.</summary>
    [Fact]
    public void TheViolin_OffersNoKernelOrBandwidth()
    {
        var members = typeof(ViolinSeries).GetProperties().Select(p => p.Name).Concat(typeof(ViolinSeries).GetMethods().Select(m => m.Name));

        Assert.DoesNotContain(members, name => name.Contains("Kernel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Bandwidth", StringComparison.OrdinalIgnoreCase));
    }
}
