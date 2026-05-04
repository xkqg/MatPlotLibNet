// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 — Verifies <see cref="DendrogramSeries"/> default properties, mutation,
/// and visitor dispatch. Companion render and serialization behaviour live in
/// <c>DendrogramRenderTests</c> and <c>DendrogramSerializationTests</c>.</summary>
public class DendrogramSeriesTests
{
    private static TreeNode SampleTree => DendrogramTreeFixtures.TwoLeaf();

    [Fact]
    public void Constructor_StoresRoot()
    {
        var root = SampleTree;
        var series = new DendrogramSeries(root);
        Assert.Same(root, series.Root);
    }

    [Fact]
    public void Orientation_DefaultsToTop()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.Equal(DendrogramOrientation.Top, series.Orientation);
    }

    [Fact]
    public void CutHeight_DefaultsToNull()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.Null(series.CutHeight);
    }

    [Fact]
    public void CutLineColor_DefaultsToNull()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.Null(series.CutLineColor);
    }

    [Fact]
    public void ColorByCluster_DefaultsToTrue()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.True(series.ColorByCluster);
    }

    [Fact]
    public void ShowLabels_DefaultsToTrue()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.True(series.ShowLabels);
    }

    [Fact]
    public void ColorMap_DefaultsToNull()
    {
        var series = new DendrogramSeries(SampleTree);
        Assert.Null(series.ColorMap);
    }

    [Theory]
    [InlineData(DendrogramOrientation.Top)]
    [InlineData(DendrogramOrientation.Bottom)]
    [InlineData(DendrogramOrientation.Left)]
    [InlineData(DendrogramOrientation.Right)]
    public void Orientation_CanBeSet_ToAllValues(DendrogramOrientation orientation)
    {
        var series = new DendrogramSeries(SampleTree) { Orientation = orientation };
        Assert.Equal(orientation, series.Orientation);
    }

    [Fact]
    public void CutHeight_CanBeSet()
    {
        var series = new DendrogramSeries(SampleTree) { CutHeight = 7.5 };
        Assert.Equal(7.5, series.CutHeight);
    }

    [Fact]
    public void CutLineColor_CanBeSet()
    {
        var series = new DendrogramSeries(SampleTree) { CutLineColor = Colors.Red };
        Assert.Equal(Colors.Red, series.CutLineColor);
    }

    [Fact]
    public void ColorByCluster_CanBeSetFalse()
    {
        var series = new DendrogramSeries(SampleTree) { ColorByCluster = false };
        Assert.False(series.ColorByCluster);
    }

    [Fact]
    public void ColorMap_CanBeSet()
    {
        var series = new DendrogramSeries(SampleTree) { ColorMap = ColorMaps.Plasma };
        Assert.Same(ColorMaps.Plasma, series.ColorMap);
    }

    [Fact]
    public void ToSeriesDto_TypeIsDendrogram()
    {
        var dto = new DendrogramSeries(SampleTree).ToSeriesDto();
        Assert.Equal("dendrogram", dto.Type);
    }

    [Fact]
    public void Accept_DispatchesToDendrogramVisitor()
    {
        var series = new DendrogramSeries(SampleTree);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(DendrogramSeries), visitor.LastVisited);
    }
}
