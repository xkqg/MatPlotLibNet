// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 — Verifies <see cref="ClustermapSeries"/> default properties, ratio
/// clamping, interface contracts, and visitor dispatch. Companion render and serialization
/// behaviour live in <c>ClustermapRenderTests</c> and <c>ClustermapSerializationTests</c>.</summary>
public class ClustermapSeriesTests
{
    private static double[,] SampleData => new double[,]
    {
        { 0.1, 0.9, 0.3 },
        { 0.8, 0.2, 0.7 },
        { 0.4, 0.6, 0.5 },
    };

    // ── Panel-ratio clamping ─────────────────────────────────────────────────

    /// <summary>Each dendrogram panel's share of the figure is held between 0 and 0.9: a value outside is clamped to the nearer end, one
    /// inside is kept as given.</summary>
    [Theory]
    [InlineData(nameof(ClustermapSeries.RowDendrogramWidth), -0.5, 0.0)]
    [InlineData(nameof(ClustermapSeries.RowDendrogramWidth), 1.5, 0.9)]
    [InlineData(nameof(ClustermapSeries.RowDendrogramWidth), 0.25, 0.25)]
    [InlineData(nameof(ClustermapSeries.ColumnDendrogramHeight), -1.0, 0.0)]
    [InlineData(nameof(ClustermapSeries.ColumnDendrogramHeight), 2.0, 0.9)]
    [InlineData(nameof(ClustermapSeries.ColumnDendrogramHeight), 0.20, 0.20)]
    public void ADendrogramPanelsShare_IsHeldBetweenZeroAndNineTenths(string panel, double given, double kept)
    {
        var series = new ClustermapSeries(SampleData);
        var share = typeof(ClustermapSeries).GetProperty(panel)!;

        share.SetValue(series, given);

        Assert.Equal(kept, (double)share.GetValue(series)!, 10);
    }

    // ── GetColorBarRange ──────────────────────────────────────────────────────

    [Fact]
    public void GetColorBarRange_ReturnsMinMaxOfData()
    {
        var data = new double[,] { { 1.0, 5.0 }, { 3.0, 2.0 } };
        var series = new ClustermapSeries(data);
        var range = series.GetColorBarRange();
        Assert.Equal(1.0, range.Min, precision: 10);
        Assert.Equal(5.0, range.Max, precision: 10);
    }

    [Fact]
    public void GetColorBarRange_AllEqualData_RunsFromTheValueToOneAboveIt()
    {
        var series = new ClustermapSeries(new double[,] { { 7.0, 7.0 } });
        var range = series.GetColorBarRange();
        Assert.Equal(7.0, range.Min, precision: 10);
        Assert.Equal(8.0, range.Max, precision: 10);
    }

    // ── ComputeDataRange ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_ReturnsColRowExtent()
    {
        var series = new ClustermapSeries(SampleData);
        var ctx = new TestAxesContext();
        var range = series.ComputeDataRange(ctx);
        Assert.Equal(0, range.XMin);
        Assert.Equal(3, range.XMax); // 3 columns
        Assert.Equal(0, range.YMin);
        Assert.Equal(3, range.YMax); // 3 rows
    }

    [Fact]
    public void ComputeDataRange_EmptyData_ReturnsAllNull()
    {
        var series = new ClustermapSeries(new double[0, 0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    // ── Visitor dispatch ──────────────────────────────────────────────────────

    [Fact]
    public void Accept_DispatchesToClustermapVisitor()
    {
        var series = new ClustermapSeries(SampleData);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(ClustermapSeries), visitor.LastVisited);
    }
}
