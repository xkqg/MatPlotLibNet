// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>NaN is the library's missing value, so a figure that holds one must survive ToJson and FromJson. JSON has
/// no number for NaN or infinity; System.Text.Json refused to write them ("positive and negative infinity cannot be
/// written"), so a heatmap with an empty cell or a line with a gap could not be saved at all. The serializer writes a
/// non-finite number as the string that names it ("NaN", "Infinity", "-Infinity") and reads it back as the number.</summary>
public class NonFiniteNumberSerializationTests
{
    private static readonly ChartSerializer Serializer = new();

    [Fact]
    public void ALineWithAMissingValue_IsWritten_AndReadBackWithTheMissingValueInPlace()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [1.0, double.NaN, 3])).Build();

        var readBack = Serializer.FromJson(Serializer.ToJson(figure));

        var line = Assert.IsType<LineSeries>(Assert.Single(readBack.SubPlots[0].Series));
        Assert.Equal(1.0, line.YData[0]);
        Assert.True(double.IsNaN(line.YData[1]), "the missing value came back as " + line.YData[1]);
        Assert.Equal(3.0, line.YData[2]);
    }

    [Fact]
    public void AHeatmapWithAnEmptyCell_IsWritten_AndReadBackWithTheCellEmpty()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Heatmap(new[,] { { 1.0, double.NaN }, { 3.0, 4.0 } })).Build();

        var readBack = Serializer.FromJson(Serializer.ToJson(figure));

        var heatmap = Assert.IsType<HeatmapSeries>(Assert.Single(readBack.SubPlots[0].Series));
        Assert.True(double.IsNaN(heatmap.Data[0, 1]));
        Assert.Equal(4.0, heatmap.Data[1, 1]);
    }

    [Fact]
    public void AnInfinity_IsWritten_AndReadBackAsThatInfinity()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [double.PositiveInfinity, 2, double.NegativeInfinity])).Build();

        var line = (LineSeries)Serializer.FromJson(Serializer.ToJson(figure)).SubPlots[0].Series[0];

        Assert.Equal(double.PositiveInfinity, line.YData[0]);
        Assert.Equal(double.NegativeInfinity, line.YData[2]);
    }

    /// <summary>The written form, pinned: the string that names the number, quoted, where a number would stand.</summary>
    [Fact]
    public void ANonFiniteNumber_IsWrittenAsTheQuotedNameOfThatNumber()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1, 2], [double.NaN, double.PositiveInfinity, double.NegativeInfinity])).Build();

        string json = Serializer.ToJson(figure);

        Assert.Contains("\"yData\":[\"NaN\",\"Infinity\",\"-Infinity\"]", json);
    }
}
