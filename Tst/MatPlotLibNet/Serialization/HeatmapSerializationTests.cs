// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.10 — verifies JSON round-trip for the new <see cref="HeatmapSeries"/> properties
/// (<c>ShowLabels</c>, <c>LabelFormat</c>, <c>MaskMode</c>, <c>CellValueColor</c>).</summary>
public class HeatmapSerializationTests
{
    private static double[,] Sample => new double[,] { { 1, 2 }, { 3, 4 } };

    private static HeatmapSeries Roundtrip(Action<HeatmapSeries> configure)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(Sample, configure)).Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<HeatmapSeries>().First();
    }

    [Fact]
    public void RoundTrip_DefaultProperties_AreNotEmittedToJson()
    {
        // Defaults must round-trip silently (no extraneous fields in JSON for the common case).
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(Sample)).Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"showLabels\"", json);
        Assert.DoesNotContain("\"labelFormat\"", json);
        Assert.DoesNotContain("\"maskMode\"", json);
        Assert.DoesNotContain("\"cellValueColor\"", json);
    }

    [Fact]
    public void RoundTrip_PreservesShowLabels()
    {
        var s = Roundtrip(s => s.ShowLabels = true);
        Assert.True(s.ShowLabels);
    }

    [Fact]
    public void RoundTrip_PreservesLabelFormat()
    {
        var s = Roundtrip(s => s.LabelFormat = "P1");
        Assert.Equal("P1", s.LabelFormat);
    }

    [Theory]
    [InlineData(HeatmapMaskMode.None)]
    [InlineData(HeatmapMaskMode.UpperTriangle)]
    [InlineData(HeatmapMaskMode.LowerTriangle)]
    [InlineData(HeatmapMaskMode.UpperTriangleStrict)]
    [InlineData(HeatmapMaskMode.LowerTriangleStrict)]
    public void RoundTrip_PreservesMaskMode(HeatmapMaskMode mode)
    {
        var s = Roundtrip(s => s.MaskMode = mode);
        Assert.Equal(mode, s.MaskMode);
    }

    [Fact]
    public void RoundTrip_PreservesCellValueColor()
    {
        var s = Roundtrip(s => s.CellValueColor = Colors.Red);
        Assert.Equal(Colors.Red, s.CellValueColor);
    }
}
