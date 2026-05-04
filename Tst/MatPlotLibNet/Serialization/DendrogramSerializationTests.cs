// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.10 — verifies JSON round-trip for the new <see cref="DendrogramSeries"/>
/// properties (<c>Orientation</c>, <c>CutHeight</c>, <c>CutLineColor</c>, <c>ColorByCluster</c>).
/// The tree itself is not serialised in this phase — the registry rebuilds the series with
/// a placeholder root, mirroring the <c>treemap</c> / <c>sunburst</c> registry entries.</summary>
public class DendrogramSerializationTests
{
    private static TreeNode SampleTree => DendrogramTreeFixtures.TwoLeaf();

    private static DendrogramSeries Roundtrip(Action<DendrogramSeries> configure)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Dendrogram(SampleTree, configure)).Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<DendrogramSeries>().First();
    }

    [Fact]
    public void RoundTrip_DefaultProperties_AreNotEmittedToJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Dendrogram(SampleTree)).Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"dendrogramOrientation\"", json);
        Assert.DoesNotContain("\"cutHeight\"", json);
        Assert.DoesNotContain("\"cutLineColor\"", json);
        Assert.DoesNotContain("\"colorByCluster\"", json);
    }

    [Theory]
    [InlineData(DendrogramOrientation.Top)]
    [InlineData(DendrogramOrientation.Bottom)]
    [InlineData(DendrogramOrientation.Left)]
    [InlineData(DendrogramOrientation.Right)]
    public void RoundTrip_PreservesOrientation(DendrogramOrientation orientation)
    {
        var s = Roundtrip(s => s.Orientation = orientation);
        Assert.Equal(orientation, s.Orientation);
    }

    [Fact]
    public void RoundTrip_PreservesCutHeight()
    {
        var s = Roundtrip(s => s.CutHeight = 1.5);
        Assert.Equal(1.5, s.CutHeight);
    }

    [Fact]
    public void RoundTrip_PreservesCutLineColor()
    {
        var s = Roundtrip(s => s.CutLineColor = Colors.Blue);
        Assert.Equal(Colors.Blue, s.CutLineColor);
    }

    [Fact]
    public void RoundTrip_PreservesColorByCluster_False()
    {
        var s = Roundtrip(s => s.ColorByCluster = false);
        Assert.False(s.ColorByCluster);
    }

    [Fact]
    public void RoundTrip_PreservesColorMap()
    {
        var s = Roundtrip(s => s.ColorMap = QualitativeColorMaps.Tab10);
        Assert.NotNull(s.ColorMap);
        Assert.Equal("tab10", s.ColorMap!.Name);
    }

    [Fact]
    public void RoundTrip_PreservesShowLabels_False()
    {
        var s = Roundtrip(s => s.ShowLabels = false);
        Assert.False(s.ShowLabels);
    }

    [Fact]
    public void RoundTrip_AllNonDefaults_Combined()
    {
        var s = Roundtrip(s =>
        {
            s.Orientation = DendrogramOrientation.Left;
            s.CutHeight = 2.5;
            s.CutLineColor = Colors.Red;
            s.ColorByCluster = false;
            s.ColorMap = QualitativeColorMaps.Set1;
            s.ShowLabels = false;
        });
        Assert.Equal(DendrogramOrientation.Left, s.Orientation);
        Assert.Equal(2.5, s.CutHeight);
        Assert.Equal(Colors.Red, s.CutLineColor);
        Assert.False(s.ColorByCluster);
        Assert.Equal("set1", s.ColorMap!.Name);
        Assert.False(s.ShowLabels);
    }
}
