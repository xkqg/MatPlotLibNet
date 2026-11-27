// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that batch transform methods produce results equivalent to per-point <c>DataToPixel</c>.</summary>
public class DataTransformBatchTests
{
    private static DataTransform MakeTransform() =>
        new DataTransform(0, 100, 0, 200, new Rect(10, 20, 400, 300));

    [Fact]
    public void TransformX_MatchesPerPointDataToPixelX()
    {
        var t = MakeTransform();
        double[] xData = [0.0, 25.0, 50.0, 75.0, 100.0];
        double[] batch = t.TransformX(xData);
        for (int i = 0; i < xData.Length; i++)
            Assert.Equal(t.DataToPixel(xData[i], 0).X, batch[i], 1e-10);
    }

    [Fact]
    public void TransformY_MatchesPerPointDataToPixelY()
    {
        var t = MakeTransform();
        double[] yData = [0.0, 50.0, 100.0, 150.0, 200.0];
        double[] batch = t.TransformY(yData);
        for (int i = 0; i < yData.Length; i++)
            Assert.Equal(t.DataToPixel(0, yData[i]).Y, batch[i], 1e-10);
    }

    [Fact]
    public void TransformBatch_MatchesPerPointDataToPixel()
    {
        var t = MakeTransform();
        double[] xData = [0.0, 25.0, 50.0, 75.0, 100.0];
        double[] yData = [0.0, 50.0, 100.0, 150.0, 200.0];
        var batch = t.TransformBatch(xData, yData);
        for (int i = 0; i < xData.Length; i++)
        {
            var expected = t.DataToPixel(xData[i], yData[i]);
            Assert.Equal(expected.X, batch[i].X, 1e-10);
            Assert.Equal(expected.Y, batch[i].Y, 1e-10);
        }
    }

    [Fact]
    public void TransformX_ZeroDataRange_ReturnsCenteredX()
    {
        var t = new DataTransform(5, 5, 0, 100, new Rect(10, 20, 400, 300));
        double[] xData = [5.0, 5.0, 5.0];
        double[] batch = t.TransformX(xData);
        double expected = t.DataToPixel(5.0, 50.0).X; // per-point handles zero range
        foreach (var px in batch)
            Assert.Equal(expected, px, 1e-10);
    }

    [Fact]
    public void TransformY_ZeroDataRange_ReturnsCenteredY()
    {
        var t = new DataTransform(0, 100, 3, 3, new Rect(10, 20, 400, 300));
        double[] yData = [3.0, 3.0, 3.0];
        double[] batch = t.TransformY(yData);
        double expected = t.DataToPixel(50.0, 3.0).Y;
        foreach (var py in batch)
            Assert.Equal(expected, py, 1e-10);
    }

    /// <summary>An axis whose range has no width puts every value it can place on its centre, but a value it cannot
    /// place (NaN, an infinity, a non-positive value on a log axis) still comes out NaN: the pixel is what a renderer
    /// asks whether a coordinate can be drawn, so a missing value must never land on the centre.</summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DataToPixel_ZeroDataRange_LeavesAValueItCannotPlaceUnplaced(double value)
    {
        var flatX = new DataTransform(5, 5, 0, 100, new Rect(10, 20, 400, 300));
        var flatY = new DataTransform(0, 100, 3, 3, new Rect(10, 20, 400, 300));

        Assert.True(double.IsNaN(flatX.DataToPixel(value, 50).X));
        Assert.True(double.IsNaN(flatY.DataToPixel(50, value).Y));
        Assert.Equal(210, flatX.DataToPixel(5, 50).X, 1e-10);
        Assert.Equal(170, flatY.DataToPixel(50, 3).Y, 1e-10);
    }

    [Fact]
    public void TransformXAndY_ZeroDataRange_LeaveAValueTheyCannotPlaceUnplaced()
    {
        var flat = new DataTransform(5, 5, 3, 3, new Rect(10, 20, 400, 300));

        double[] px = flat.TransformX([5.0, double.NaN, double.PositiveInfinity]);
        double[] py = flat.TransformY([3.0, double.NaN, double.NegativeInfinity]);

        Assert.Equal(210, px[0], 1e-10);
        Assert.True(double.IsNaN(px[1]) && double.IsNaN(px[2]));
        Assert.Equal(170, py[0], 1e-10);
        Assert.True(double.IsNaN(py[1]) && double.IsNaN(py[2]));
    }

    [Fact]
    public void TransformBatch_ZeroDataRangeOnALogAxis_LeavesAValueItCannotPlaceUnplaced()
    {
        var flatLog = new DataTransform(0, 10, 1, 1, new Rect(10, 20, 400, 300), null, null, 0, 10, 10, 10,
            MatPlotLibNet.Models.AxisScale.Linear, MatPlotLibNet.Models.AxisScale.Log, 1, 1);

        var points = flatLog.TransformBatch([1.0, 2], [10, 0]);

        Assert.Equal(170, points[0].Y, 1e-10);
        Assert.True(double.IsNaN(points[1].Y));
    }

    [Fact]
    public void TransformBatch_LargeDataset_MatchesPerPoint()
    {
        // 1003 points: exercises both SIMD (4-wide) path and scalar remainder (3 elements)
        var rng = new Random(42);
        int n = 1003;
        double[] xData = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] yData = Enumerable.Range(0, n).Select(_ => rng.NextDouble() * 200).ToArray();
        var t = new DataTransform(0, n - 1, 0, 200, new Rect(10, 20, 800, 600));

        var batch = t.TransformBatch(xData, yData);

        Assert.Equal(n, batch.Length);
        for (int i = 0; i < n; i++)
        {
            var expected = t.DataToPixel(xData[i], yData[i]);
            Assert.Equal(expected.X, batch[i].X, 1e-10);
            Assert.Equal(expected.Y, batch[i].Y, 1e-10);
        }
    }

    [Fact]
    public void TransformBatch_ZeroBothRanges_AllCentered()
    {
        var t = new DataTransform(5, 5, 3, 3, new Rect(10, 20, 400, 300));
        double[] xData = [5.0, 5.0, 5.0, 5.0, 5.0];
        double[] yData = [3.0, 3.0, 3.0, 3.0, 3.0];
        var batch = t.TransformBatch(xData, yData);
        var expected = t.DataToPixel(5.0, 3.0);
        foreach (var p in batch)
        {
            Assert.Equal(expected.X, p.X, 1e-10);
            Assert.Equal(expected.Y, p.Y, 1e-10);
        }
    }

    [Fact]
    public void TransformBatch_SinglePoint_MatchesPerPoint()
    {
        var t = MakeTransform();
        double[] xData = [42.0];
        double[] yData = [137.0];
        var batch = t.TransformBatch(xData, yData);
        var expected = t.DataToPixel(42.0, 137.0);
        Assert.Equal(expected.X, batch[0].X, 1e-10);
        Assert.Equal(expected.Y, batch[0].Y, 1e-10);
    }

    [Fact]
    public void TransformBatch_Empty_ReturnsEmpty()
    {
        var t = MakeTransform();
        var batch = t.TransformBatch(ReadOnlySpan<double>.Empty, ReadOnlySpan<double>.Empty);
        Assert.Empty(batch);
    }

    // ── one rule for a pixel ────────────────────────────────────────────────────

    private static bool Same(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

    /// <summary>A point lands on the same bits whichever path places it: on its own, as part of one direction of an array, or as
    /// part of an array of points, four at a time or among the up to three left over, on any processor.</summary>
    [Fact]
    public void EveryPathThatPlacesAPoint_GivesItTheSameBits()
    {
        var t = new DataTransform(0, 3, 0, 7, new Rect(13.7, 21.3, 517.9, 333.1));
        var rng = new Random(7);
        const int n = 4_003;
        double[] x = new double[n], y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = rng.NextDouble() * 3; y[i] = rng.NextDouble() * 7; }

        var batch = t.TransformBatch(x, y);
        var px = t.TransformX(x);
        var py = t.TransformY(y);

        int differ = 0;
        for (int i = 0; i < n; i++)
        {
            var p = t.DataToPixel(x[i], y[i]);
            if (!Same(p.X, batch[i].X) || !Same(p.Y, batch[i].Y) || !Same(p.X, px[i]) || !Same(p.Y, py[i])) differ++;
        }

        Assert.Equal(0, differ);
    }

    /// <summary>The rule itself: the value less where the range starts, times the pixels per unit, plus where the plot starts.
    /// Each step is rounded once, so the bits are the same on every processor.</summary>
    [Fact]
    public void ThePixelOfAValue_IsTheValueLessTheRangeStart_TimesThePixelsPerUnit_PlusThePlotStart()
    {
        var plot = new Rect(13.7, 21.3, 517.9, 333.1);
        var t = new DataTransform(2.5, 5.5, -3, 4, plot);
        var rng = new Random(11);
        double xPerUnit = plot.Width / 3, yPerUnit = -(plot.Height / 7);
        const int n = 1_001;
        double[] x = new double[n], y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = 2.5 + rng.NextDouble() * 3; y[i] = -3 + rng.NextDouble() * 7; }

        var batch = t.TransformBatch(x, y);
        int differ = 0;
        for (int i = 0; i < n; i++)
        {
            double ex = (x[i] - 2.5) * xPerUnit + plot.X;
            double ey = (y[i] - -3.0) * yPerUnit + (plot.Y + plot.Height);
            if (!Same(ex, t.DataToPixel(x[i], y[i]).X) || !Same(ey, t.DataToPixel(x[i], y[i]).Y)
                || !Same(ex, batch[i].X) || !Same(ey, batch[i].Y)) differ++;
        }

        Assert.Equal(0, differ);
    }

    // A tenth of a millisecond of dates, in whole steps of the last bit of a date in 2026, and the date the window starts at.
    private static readonly double DateStart = new DateTime(2026, 1, 1).ToOADate();
    private static readonly double TenthOfAMillisecond =
        Math.Round(1e-4 / 86_400 / (Math.BitIncrement(DateStart) - DateStart)) * (Math.BitIncrement(DateStart) - DateStart);

    private static double[] MillisecondOfDates() =>
        Enumerable.Range(0, 11).Select(k => DateStart + k * TenthOfAMillisecond).ToArray();

    /// <summary>A millisecond of dates is a narrow window far from 0. Each value is measured from where the window starts, so
    /// the eleven evenly spaced dates land on evenly spaced pixels on every path, to far less than a pixel.</summary>
    [Fact]
    public void AMillisecondOfDates_IsSpreadEvenly_OnEveryPath()
    {
        var plot = new Rect(0, 0, 800, 400);
        double end = DateStart + 10 * TenthOfAMillisecond;
        var alongX = new DataTransform(DateStart, end, 0, 1, plot);
        var alongY = new DataTransform(0, 1, DateStart, end, plot);
        var dates = MillisecondOfDates();
        var halves = Enumerable.Repeat(0.5, dates.Length).ToArray();

        var px = alongX.TransformX(dates);
        var py = alongY.TransformY(dates);
        var batchX = alongX.TransformBatch(dates, halves);
        var batchY = alongY.TransformBatch(halves, dates);
        for (int k = 0; k < dates.Length; k++)
        {
            Assert.Equal(80.0 * k, alongX.DataToPixel(dates[k], 0.5).X, 1e-9);
            Assert.Equal(80.0 * k, px[k], 1e-9);
            Assert.Equal(80.0 * k, batchX[k].X, 1e-9);
            Assert.Equal(400 - 40.0 * k, alongY.DataToPixel(0.5, dates[k]).Y, 1e-9);
            Assert.Equal(400 - 40.0 * k, py[k], 1e-9);
            Assert.Equal(400 - 40.0 * k, batchY[k].Y, 1e-9);
        }
    }

    /// <summary>A line through six dates a millisecond apart, drawn on a date axis that shows five milliseconds, lands its points
    /// evenly over the plot, to far less than a pixel. Measured from 0 instead of from where the window starts, each date would
    /// be about 4e14 pixels, whose last bit is a sixteenth of a pixel, and the points would stand unevenly.</summary>
    [Fact]
    public void SixDatesAMillisecondApart_OnAFiveMillisecondDateAxis_AreDrawnEvenlySpaced()
    {
        var start = new DateTime(2026, 1, 1);
        var dates = Enumerable.Range(0, 6).Select(k => start.AddTicks(k * TimeSpan.TicksPerMillisecond)).ToArray();
        var figure = MatPlotLibNet.Plt.Create().WithSize(800, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(dates, [1.0, 2.0, 3.0, 2.0, 1.0, 2.0])
                .SetXLim(dates[0].ToOADate(), dates[^1].ToOADate()))
            .Build();
        var ctx = new RecordingRenderContext();

        new ChartRenderer().Render(figure, ctx);

        var line = ctx.Polylines()
            .Single(p => p.Length == 6);
        // Each date is stored as the nearest double, whose last bit is about 0.6 microseconds, so the points are spread over
        // the dates as stored, not over whole milliseconds.
        double[] stored = dates.Select(d => d.ToOADate()).ToArray();
        double span = line[5].X - line[0].X;
        Assert.True(span > 250, $"the window spans {span} px");
        for (int k = 0; k < 6; k++)
            Assert.Equal(line[0].X + (stored[k] - stored[0]) / (stored[5] - stored[0]) * span, line[k].X, 1e-9);
    }

    /// <summary>The pixel of a date reads back as that date, to the bit: the way back starts from the same place as the way
    /// there.</summary>
    [Fact]
    public void AMillisecondOfDates_ReadsBackFromItsPixels_ToTheBit()
    {
        var plot = new Rect(0, 0, 800, 400);
        double end = DateStart + 10 * TenthOfAMillisecond;
        var t = new DataTransform(DateStart, end, DateStart, end, plot);

        foreach (double date in MillisecondOfDates())
        {
            var back = t.PixelToData(t.DataToPixel(date, date));
            Assert.True(Same(date, back.X), $"{date:R} read back as {back.X:R}");
            Assert.True(Same(date, back.Y), $"{date:R} read back as {back.Y:R}");
        }
    }
}
