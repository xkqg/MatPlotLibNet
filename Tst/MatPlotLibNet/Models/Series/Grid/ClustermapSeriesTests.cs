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

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresData()
    {
        var data = SampleData;
        var series = new ClustermapSeries(data);
        Assert.Same(data, series.Data);
    }

    [Fact]
    public void RowTree_DefaultsToNull()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Null(series.RowTree);
    }

    [Fact]
    public void ColumnTree_DefaultsToNull()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Null(series.ColumnTree);
    }

    // ── Panel-ratio defaults ──────────────────────────────────────────────────

    [Fact]
    public void RowDendrogramWidth_DefaultsToPoint15()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Equal(0.15, series.RowDendrogramWidth, precision: 10);
    }

    [Fact]
    public void ColumnDendrogramHeight_DefaultsToPoint15()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Equal(0.15, series.ColumnDendrogramHeight, precision: 10);
    }

    // ── Panel-ratio clamping ─────────────────────────────────────────────────

    [Fact]
    public void RowDendrogramWidth_NegativeValue_ClampedToZero()
    {
        var series = new ClustermapSeries(SampleData) { RowDendrogramWidth = -0.5 };
        Assert.Equal(0.0, series.RowDendrogramWidth, precision: 10);
    }

    [Fact]
    public void RowDendrogramWidth_ValueAboveMax_ClampedToPointNine()
    {
        var series = new ClustermapSeries(SampleData) { RowDendrogramWidth = 1.5 };
        Assert.Equal(0.9, series.RowDendrogramWidth, precision: 10);
    }

    [Fact]
    public void RowDendrogramWidth_ValidValue_RoundTrips()
    {
        var series = new ClustermapSeries(SampleData) { RowDendrogramWidth = 0.25 };
        Assert.Equal(0.25, series.RowDendrogramWidth, precision: 10);
    }

    [Fact]
    public void ColumnDendrogramHeight_NegativeValue_ClampedToZero()
    {
        var series = new ClustermapSeries(SampleData) { ColumnDendrogramHeight = -1.0 };
        Assert.Equal(0.0, series.ColumnDendrogramHeight, precision: 10);
    }

    [Fact]
    public void ColumnDendrogramHeight_ValueAboveMax_ClampedToPointNine()
    {
        var series = new ClustermapSeries(SampleData) { ColumnDendrogramHeight = 2.0 };
        Assert.Equal(0.9, series.ColumnDendrogramHeight, precision: 10);
    }

    [Fact]
    public void ColumnDendrogramHeight_ValidValue_RoundTrips()
    {
        var series = new ClustermapSeries(SampleData) { ColumnDendrogramHeight = 0.20 };
        Assert.Equal(0.20, series.ColumnDendrogramHeight, precision: 10);
    }

    // ── Interface defaults ────────────────────────────────────────────────────

    [Fact]
    public void ColorMap_DefaultsToNull()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Null(series.ColorMap);
    }

    [Fact]
    public void ColorMap_CanBeSet()
    {
        var series = new ClustermapSeries(SampleData) { ColorMap = ColorMaps.Plasma };
        Assert.Same(ColorMaps.Plasma, series.ColorMap);
    }

    [Fact]
    public void Normalizer_DefaultsToNull()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Null(series.Normalizer);
    }

    [Fact]
    public void ShowLabels_DefaultsFalse()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.False(series.ShowLabels);
    }

    [Fact]
    public void LabelFormat_DefaultsToNull()
    {
        var series = new ClustermapSeries(SampleData);
        Assert.Null(series.LabelFormat);
    }

    // ── Interface contracts ───────────────────────────────────────────────────

    [Fact]
    public void Implements_IColormappable()
    {
        Assert.IsAssignableFrom<IColormappable>(new ClustermapSeries(SampleData));
    }

    [Fact]
    public void Implements_INormalizable()
    {
        Assert.IsAssignableFrom<INormalizable>(new ClustermapSeries(SampleData));
    }

    [Fact]
    public void Implements_IColorBarDataProvider()
    {
        Assert.IsAssignableFrom<IColorBarDataProvider>(new ClustermapSeries(SampleData));
    }

    [Fact]
    public void Implements_ILabelable()
    {
        Assert.IsAssignableFrom<ILabelable>(new ClustermapSeries(SampleData));
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
    public void GetColorBarRange_AllEqualData_Returns0To1()
    {
        var series = new ClustermapSeries(new double[,] { { 7.0, 7.0 } });
        var range = series.GetColorBarRange();
        Assert.Equal(0.0, range.Min, precision: 10);
        Assert.Equal(1.0, range.Max, precision: 10);
    }

    // ── ComputeDataRange ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_ReturnsColRowExtent()
    {
        var series = new ClustermapSeries(SampleData);
        var ctx = new NullAxesContext();
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
        var range = series.ComputeDataRange(new NullAxesContext());
        Assert.Null(range.XMin);
        Assert.Null(range.XMax);
        Assert.Null(range.YMin);
        Assert.Null(range.YMax);
    }

    // ── Serialization type tag ────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsClustermap()
    {
        var dto = new ClustermapSeries(SampleData).ToSeriesDto();
        Assert.Equal("clustermap", dto.Type);
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

    // ── Trees can be set ──────────────────────────────────────────────────────

    [Fact]
    public void RowTree_CanBeSet()
    {
        var tree = new TreeNode { Label = "r", Children = [new TreeNode { Label = "A" }, new TreeNode { Label = "B" }] };
        var series = new ClustermapSeries(SampleData) { RowTree = tree };
        Assert.Same(tree, series.RowTree);
    }

    [Fact]
    public void ColumnTree_CanBeSet()
    {
        var tree = new TreeNode { Label = "c", Children = [new TreeNode { Label = "X" }, new TreeNode { Label = "Y" }] };
        var series = new ClustermapSeries(SampleData) { ColumnTree = tree };
        Assert.Same(tree, series.ColumnTree);
    }

    // ── NullAxesContext stub ──────────────────────────────────────────────────

    private sealed class NullAxesContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
