// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class NearestPointFinderTests
{
    // Plot area: pixel [10,110]x[10,60], data [0,10]x[0,5].
    // Data-to-pixel scales: 10 px/unit X, 10 px/unit Y.
    private static (Figure figure, ChartLayout layout) MakeFigure(
        double[] xData, double[] yData, string label = "TestSeries", bool visible = true)
    {
        var figure = Plt.Create().Plot(xData, yData).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        figure.SubPlots[0].Series[0].Label = label;
        figure.SubPlots[0].Series[0].Visible = visible;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        return (figure, layout);
    }

    [Fact]
    public void FindsClosestPointInLineSeries()
    {
        var (figure, layout) = MakeFigure([2.0, 5.0, 8.0], [1.0, 3.0, 4.0], "Line");

        // Query near data point (5, 3) — should find it exactly.
        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout, maxPixelDistance: 20.0);

        Assert.NotNull(result);
        Assert.Equal(5.0, result.DataX, precision: 3);
        Assert.Equal(3.0, result.DataY, precision: 3);
        Assert.Equal("Line", result.SeriesLabel);
        Assert.Equal(0, result.SeriesIndex);
        Assert.Equal(0.0, result.PixelDistance, precision: 3);
    }

    [Fact]
    public void ReturnsNull_WhenNoPointWithinMaxDistance()
    {
        var (figure, layout) = MakeFigure([2.0, 8.0], [1.0, 4.0]);

        // Query far from any point — data (5, 2.5) is at least ~3 data units from (2,1).
        // At 10 px/unit that is ~30+ px, beyond a 5-px threshold.
        var result = NearestPointFinder.Find(figure, 0, 5.0, 2.5, layout, maxPixelDistance: 5.0);

        Assert.Null(result);
    }

    [Fact]
    public void IgnoresInvisibleSeries()
    {
        var (figure, layout) = MakeFigure([5.0], [3.0], visible: false);

        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout, maxPixelDistance: 20.0);

        Assert.Null(result);
    }

    [Fact]
    public void ReturnsNull_WhenAxesIndexOutOfRange()
    {
        var (figure, layout) = MakeFigure([5.0], [3.0]);

        var result = NearestPointFinder.Find(figure, 99, 5.0, 3.0, layout, maxPixelDistance: 20.0);

        Assert.Null(result);
    }

    [Fact]
    public void FindsClosestAcrossMultipleSeries()
    {
        // Build a figure with two series manually.
        var figure = Plt.Create().Plot([2.0], [1.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        figure.SubPlots[0].Series[0].Label = "Far";

        // Add a second closer series.
        var closer = new ScatterSeries([5.0], [3.0]) { Label = "Close" };
        figure.SubPlots[0].AddSeries(closer);

        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);

        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout, maxPixelDistance: 20.0);

        Assert.NotNull(result);
        Assert.Equal("Close", result.SeriesLabel);
        Assert.Equal(1, result.SeriesIndex);
    }

    /// <summary>Negative axesIndex is rejected — covers the lower bound of the
    /// `axesIndex &lt; 0 || axesIndex &gt;= ...` guard.</summary>
    [Fact]
    public void ReturnsNull_WhenAxesIndexNegative()
    {
        var (figure, layout) = MakeFigure([5.0], [3.0]);
        var result = NearestPointFinder.Find(figure, -1, 5.0, 3.0, layout);
        Assert.Null(result);
    }

    /// <summary>Degenerate range (xMax == xMin) — covers the `xSpan == 0 || ySpan == 0`
    /// early-return arm.</summary>
    [Fact]
    public void ReturnsNull_WhenXAxisHasZeroSpan()
    {
        var figure = Plt.Create().Plot([5.0], [3.0]).Build();
        figure.SubPlots[0].XAxis.Min = 5;
        figure.SubPlots[0].XAxis.Max = 5;       // zero span
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);

        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout);
        Assert.Null(result);
    }

    /// <summary>Series that is not an XYSeries (e.g. a PieSeries) is skipped — covers
    /// the `is not XYSeries` filter arm.</summary>
    [Fact]
    public void IgnoresNonXYSeries()
    {
        var figure = Plt.Create().Build();
        var ax = figure.SubPlots.Count > 0 ? figure.SubPlots[0] : figure.AddSubPlot();
        ax.XAxis.Min = 0; ax.XAxis.Max = 10;
        ax.YAxis.Min = 0; ax.YAxis.Max = 5;
        ax.AddSeries(new PieSeries([30.0, 70.0]));         // not XYSeries
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);

        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout);
        Assert.Null(result);
    }
    // ── which point, not just where ────────────────────────────────────────────
    //
    // The finder walks XData/YData by index and had the index in hand at the moment it chose a point, then
    // built a result without it. An application that wants to know WHICH of its own rows was hit had to
    // compare two doubles against its source array — and two samples at the same instant are indistinguishable
    // that way. The index is carried now.

    [Fact]
    public void TheResult_CarriesTheIndexOfThePointItChose()
    {
        var (figure, layout) = MakeFigure([2.0, 5.0, 8.0], [1.0, 3.0, 4.0], "Line");

        var result = NearestPointFinder.Find(figure, 0, 5.0, 3.0, layout, maxPixelDistance: 20.0);

        Assert.NotNull(result);
        Assert.Equal(1, result.PointIndex);
        Assert.Equal(0, result.SeriesIndex);
    }

    [Fact]
    public void TheResult_CarriesTheIndexOfTheFirstPointToo()
    {
        var (figure, layout) = MakeFigure([2.0, 5.0, 8.0], [1.0, 3.0, 4.0], "Line");

        var result = NearestPointFinder.Find(figure, 0, 2.0, 1.0, layout, maxPixelDistance: 20.0);

        Assert.NotNull(result);
        Assert.Equal(0, result.PointIndex);
    }

    [Fact]
    public void AResultConstructedWithoutAnIndex_SaysSoRatherThanPointingAtTheFirstPoint()
    {
        var result = new NearestPointResult("s", 0, 1.0, 2.0, 3.0);

        Assert.Equal(-1, result.PointIndex);
    }

    [Fact]
    public void TwoArraysOfDifferentLength_NeverReachTheFinderAtAll()
    {
        // The index is only useful if it addresses a sample in BOTH arrays. It always does, and not because
        // the finder is careful: a series with mismatched arrays cannot be built in the first place. The
        // finder's own Math.Min stays as the second line of defence for a series assembled by hand.
        var ex = Assert.Throws<ArgumentException>(() => Plt.Create().Plot([2.0, 5.0, 8.0], [1.0, 3.0]).Build());

        Assert.Contains("Array lengths must match", ex.Message, StringComparison.Ordinal);
    }

}
