// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="KdeSeries"/> default properties, construction, and serialization.</summary>
public class KdeSeriesTests
{
    /// <summary>Constructor stores the data array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] data = [1.0, 2.0, 3.0];
        var series = new KdeSeries(data);
        Assert.Equal(data, series.Data);
    }

    /// <summary>Bandwidth defaults to null (auto Silverman).</summary>
    [Fact]
    public void Bandwidth_DefaultsToNull()
    {
        var series = new KdeSeries([1.0, 2.0, 3.0]);
        Assert.Null(series.Bandwidth);
    }

    /// <summary>Fill defaults to true.</summary>
    [Fact]
    public void Fill_DefaultsToTrue()
    {
        var series = new KdeSeries([1.0]);
        Assert.True(series.Fill);
    }

    /// <summary>Alpha defaults to 0.3.</summary>
    [Fact]
    public void Alpha_DefaultsTo0p3()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(0.3, series.Alpha);
    }

    /// <summary>LineWidth defaults to 1.5.</summary>
    [Fact]
    public void LineWidth_DefaultsTo1p5()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>LineStyle defaults to Solid.</summary>
    [Fact]
    public void LineStyle_DefaultsToSolid()
    {
        var series = new KdeSeries([1.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Bandwidth can be set and read back.</summary>
    [Fact]
    public void Bandwidth_CanBeSet()
    {
        var series = new KdeSeries([1.0, 2.0, 3.0]) { Bandwidth = 0.5 };
        Assert.Equal(0.5, series.Bandwidth);
    }

    /// <summary>ToSeriesDto returns type "kde".</summary>
    [Fact]
    public void ToSeriesDto_ReturnsTypeKde()
    {
        var series = new KdeSeries([1.0, 2.0]);
        Assert.Equal("kde", series.ToSeriesDto().Type);
    }

    /// <summary>ToSeriesDto includes Data array.</summary>
    [Fact]
    public void ToSeriesDto_IncludesData()
    {
        double[] data = [1.0, 2.0, 3.0];
        var series = new KdeSeries(data);
        Assert.Equal(data, series.ToSeriesDto().Data);
    }

    /// <summary>ToSeriesDto serializes Bandwidth when set.</summary>
    [Fact]
    public void ToSeriesDto_IncludesBandwidthWhenSet()
    {
        var series = new KdeSeries([1.0, 2.0]) { Bandwidth = 0.7 };
        Assert.Equal(0.7, series.ToSeriesDto().Bandwidth);
    }


    // ---- the range of the axes is the extent of the curve --------------------------------------------------------------

    /// <summary>The axes' range comes from the grid the curve is drawn on, which reaches three bandwidths past the smallest
    /// and the largest observation, so the curve and the range cannot disagree. For 1 to 6 and 30 the Silverman bandwidth
    /// is 7.2967, and the range runs from -20.89 to 51.89.</summary>
    [Fact]
    public void TheRange_IsTheGridTheCurveIsDrawnOn()
    {
        double[] data = [1, 2, 3, 4, 5, 6, 30];
        double bandwidth = MatPlotLibNet.Numerics.GaussianKde.SilvermanBandwidth(data);

        var range = new KdeSeries(data).ComputeDataRange(new NoAxes());

        Assert.Equal(1 - 3 * bandwidth, range.XMin!.Value, 9);
        Assert.Equal(30 + 3 * bandwidth, range.XMax!.Value, 9);
        Assert.Equal(-20.89, range.XMin.Value, 2);
        Assert.Equal(51.89, range.XMax.Value, 2);
        Assert.Equal(0, range.YMin);
    }

    [Fact]
    public void TheRange_OfAnExplicitBandwidth_IsThreeBandwidthsPastTheSample()
    {
        var range = new KdeSeries([10, 20]) { Bandwidth = 2 }.ComputeDataRange(new NoAxes());

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

        var range = new KdeSeries(data) { Bandwidth = bandwidth }.ComputeDataRange(new NoAxes());
        var silverman = new KdeSeries(data).ComputeDataRange(new NoAxes());

        Assert.Equal(silverman, range);
        Assert.True(double.IsFinite(range.XMin!.Value) && double.IsFinite(range.XMax!.Value));
    }

    [Fact]
    public void ASampleOfOneValue_HasARangeAroundIt()
    {
        var range = new KdeSeries([7.0]).ComputeDataRange(new NoAxes());

        Assert.True(range.XMin < 7 && range.XMax > 7);
    }

    [Fact]
    public void ASampleOfNothingButMissingValues_HasNoRange()
    {
        var range = new KdeSeries([double.NaN, double.PositiveInfinity]).ComputeDataRange(new NoAxes());

        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
    }

    /// <summary>The density peak, so the Y axis covers the curve, 5 % above it.</summary>
    [Fact]
    public void TheYRange_CoversThePeakOfTheCurve()
    {
        double[] data = [1, 2, 3, 4, 5, 6, 30];
        var series = new KdeSeries(data);

        var range = series.ComputeDataRange(new NoAxes());

        Assert.Equal(series.Curve()!.Value.Y.Max() * 1.05, range.YMax!.Value, 12);
    }

    [Fact]
    public void TheCurve_OfNoObservation_IsNothing() => Assert.Null(new KdeSeries([]).Curve());

    private sealed class NoAxes : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
