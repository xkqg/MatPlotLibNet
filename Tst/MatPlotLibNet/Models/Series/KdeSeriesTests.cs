// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="KdeSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class KdeSeriesTests
{
    // ---- the range of the axes is the extent of the curve --------------------------------------------------------------

    /// <summary>The axes' range comes from the grid the curve is drawn on, which reaches three bandwidths past the smallest
    /// and the largest observation, so the curve and the range cannot disagree. For 1 to 6 and 30 the Silverman bandwidth
    /// is 7.2967, and the range runs from -20.89 to 51.89.</summary>
    [Fact]
    public void TheRange_IsTheGridTheCurveIsDrawnOn()
    {
        double[] data = [1, 2, 3, 4, 5, 6, 30];
        double bandwidth = MatPlotLibNet.Numerics.GaussianKde.Bandwidth(data, MatPlotLibNet.Numerics.GaussianKde.SilvermanFactor);

        var range = new KdeSeries(data).ComputeDataRange(new TestAxesContext());

        Assert.Equal(1 - 3 * bandwidth, range.XMin!.Value, 9);
        Assert.Equal(30 + 3 * bandwidth, range.XMax!.Value, 9);
        Assert.Equal(-20.89, range.XMin.Value, 2);
        Assert.Equal(51.89, range.XMax.Value, 2);
        Assert.Equal(0, range.YMin);
    }

    [Fact]
    public void TheRange_OfAnExplicitBandwidth_IsThreeBandwidthsPastTheSample()
    {
        var range = new KdeSeries([10, 20]) { Bandwidth = 2 }.ComputeDataRange(new TestAxesContext());

        Assert.Equal(4, range.XMin!.Value, 12);
        Assert.Equal(26, range.XMax!.Value, 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABandwidthThatIsNotAPositiveNumber_FallsBackToSilverman(double bandwidth)
    {
        double[] data = [1, 2, 3, 4, 5, 6, 30];

        var range = new KdeSeries(data) { Bandwidth = bandwidth }.ComputeDataRange(new TestAxesContext());
        var silverman = new KdeSeries(data).ComputeDataRange(new TestAxesContext());

        Assert.Equal(silverman, range);
        new[] { range.XMin!.Value, range.XMax!.Value }.AssertAllFinite();
    }

    [Fact]
    public void ASampleOfOneValue_HasARangeAroundIt()
    {
        var range = new KdeSeries([7.0]).ComputeDataRange(new TestAxesContext());

        Assert.True(range.XMin < 7 && range.XMax > 7);
    }

    [Fact]
    public void ASampleOfNothingButMissingValues_HasNoRange()
    {
        var range = new KdeSeries([double.NaN, double.PositiveInfinity]).ComputeDataRange(new TestAxesContext());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>The density peak, so the Y axis covers the curve, 5 % above it.</summary>
    [Fact]
    public void TheYRange_CoversThePeakOfTheCurve()
    {
        double[] data = [1, 2, 3, 4, 5, 6, 30];
        var series = new KdeSeries(data);

        var range = series.ComputeDataRange(new TestAxesContext());

        Assert.Equal(series.CurveUnder(ThemeLooks.Library)!.Value.Y.Max() * 1.05, range.YMax!.Value, 12);
    }

    [Fact]
    public void TheCurve_OfNoObservation_IsNothing() => Assert.Null(new KdeSeries([]).CurveUnder(ThemeLooks.Library));

    /// <summary>The range of the axes and the renderer both read the curve, so a draw asks for it at least twice: the series
    /// estimates it once and keeps it.</summary>
    [Fact]
    public void TheCurve_AskedTwice_IsEstimatedOnce()
    {
        var series = new KdeSeries([1.0, 2, 2.5, 3, 7]);

        var first = series.CurveUnder(ThemeLooks.Library)!.Value;
        var second = series.CurveUnder(ThemeLooks.Library)!.Value;

        Assert.Same(first.X, second.X);
        Assert.Same(first.Y, second.Y);
    }

    /// <summary>A figure drawn again with its series unchanged (here its title changed, so the subplot is drawn) reads the
    /// curve the draw before estimated: one estimate per series, not one per draw.</summary>
    [Fact]
    public void TheCurve_ADrawUsed_IsTheOneTheNextDrawUses()
    {
        KdeSeries series = null!;
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde([1.0, 2, 2.5, 3, 7], s => series = s)).Build();
        string first = figure.ToSvg();
        var drawn = series.CurveUnder(figure.Theme.Looks)!.Value;

        figure.SubPlots[0].Title = "drawn again";
        string second = figure.ToSvg();

        Assert.Same(drawn.Y, series.CurveUnder(figure.Theme.Looks)!.Value.Y);
        Assert.NotEqual(first, second);
    }

    /// <summary>A value changed in place passes no setter, so the change mark does not move; the curve is estimated again all
    /// the same, as a fresh series estimates it, because the copy of the data it was estimated from no longer matches.</summary>
    [Fact]
    public void TheCurve_AfterAValueChangedInPlace_IsEstimatedAgain()
    {
        double[] data = [1.0, 2, 2.5, 3, 7];
        var series = new KdeSeries(data);
        var before = series.CurveUnder(ThemeLooks.Library)!.Value;

        data[4] = 12;
        var after = series.CurveUnder(ThemeLooks.Library)!.Value;

        Assert.NotSame(before.Y, after.Y);
        var fresh = new KdeSeries([1.0, 2, 2.5, 3, 12]).CurveUnder(ThemeLooks.Library)!.Value;
        Assert.Equal(fresh.X, after.X);
        Assert.Equal(fresh.Y, after.Y);
    }

    /// <summary>A figure drawn again after a value changed in place draws the subplot again and estimates the curve again; the
    /// kept drawing and the kept curve each need a copy of the data they were made from, and they share one: a draw copies the
    /// data once, not twice. Measured as what the draw allocates on its thread against the size of the data.</summary>
    [Fact]
    public void ADrawAfterAValueChangedInPlace_CopiesTheDataOnce_ForTheKeptDrawingAndTheKeptCurve()
    {
        double[] data = [.. Enumerable.Range(0, 50_000).Select(i => i * 37 % 1000 / 10.0)];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde(data)).Build();
        _ = figure.ToSvg();
        data[0] += 1;
        _ = figure.ToSvg();   // the second draw under one key: from here on a draw keeps a copy of its data
        data[1] += 1;

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = figure.ToSvg();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // One copy of the data for both keeps, one sorted sample for the estimate, and the markup: under 2.5 times the data. A
        // copy for each keep makes it three times the data and more.
        long dataBytes = data.Length * sizeof(double);
        Assert.True(allocated < dataBytes * 5 / 2, $"A changed draw allocated {allocated:N0} B for {dataBytes:N0} B of data.");
    }

    /// <summary>The copy a draw shares between the kept drawing and the kept curve is a copy of what the series holds at that
    /// draw: a value changed in place and then changed back is drawn as the series holds it now, the same as a fresh figure of
    /// the same values, never as the drawing or the curve made in between.</summary>
    [Fact]
    public void AValueChangedInPlaceAndBack_IsDrawnAsAFreshFigureOfTheSameValues()
    {
        double[] data = [1.0, 2, 2.5, 3, 7];
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde(data)).Build();
        _ = figure.ToSvg();
        _ = figure.ToSvg();
        data[4] = 12;
        _ = figure.ToSvg();
        data[4] = 7;

        string svg = figure.ToSvg();

        Assert.Equal(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde([1.0, 2, 2.5, 3, 7])).Build().ToSvg(), svg);
    }

    /// <summary>A setting the curve depends on moves the series' change mark, and the curve is estimated again with it.</summary>
    [Fact]
    public void TheCurve_AfterTheBandwidthChanged_IsEstimatedAgain()
    {
        var series = new KdeSeries([1.0, 2, 2.5, 3, 7]);
        var before = series.CurveUnder(ThemeLooks.Library)!.Value;

        series.Bandwidth = 0.25;
        var after = series.CurveUnder(ThemeLooks.Library)!.Value;

        Assert.NotSame(before.Y, after.Y);
        var fresh = new KdeSeries([1.0, 2, 2.5, 3, 7]) { Bandwidth = 0.25 }.CurveUnder(ThemeLooks.Library)!.Value;
        Assert.Equal(fresh.X, after.X);
        Assert.Equal(fresh.Y, after.Y);
    }

    /// <summary>A sample with nothing to estimate from keeps that answer as well: no curve, asked twice.</summary>
    [Fact]
    public void NoCurve_AskedTwice_IsStillNoCurve()
    {
        var series = new KdeSeries([double.NaN]);

        Assert.Null(series.CurveUnder(ThemeLooks.Library));
        Assert.Null(series.CurveUnder(ThemeLooks.Library));
    }
}
