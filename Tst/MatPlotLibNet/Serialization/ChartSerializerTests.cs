// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

public class ChartSerializerTests
{
    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var figure = Plt.Create()
            .WithTitle("Test")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Build();

        string json = ChartServices.Serializer.ToJson(figure);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Test", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void ToJson_IncludesDimensions()
    {
        var figure = Plt.Create()
            .WithSize(1024, 768)
            .Build();

        string json = ChartServices.Serializer.ToJson(figure);
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1024, doc.RootElement.GetProperty("width").GetDouble());
        Assert.Equal(768, doc.RootElement.GetProperty("height").GetDouble());
    }

    [Fact]
    public void ToJson_IncludesSeriesTypeDiscriminator()
    {
        var figure = Plt.Create()
            .Scatter([1.0], [2.0])
            .Build();

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.Contains("\"type\"", json);
        Assert.Contains("scatter", json);
    }

    [Fact]
    public void ToJson_IncludesLineSeries()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], line =>
            {
                line.Color = Color.Red;
                line.LineWidth = 2.5;
                line.Label = "My Line";
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.Contains("line", json);
        Assert.Contains("My Line", json);
    }

    [Fact]
    public void RoundTrip_PreservesTitle()
    {
        var original = Plt.Create()
            .WithTitle("Round Trip")
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal("Round Trip", restored.Title);
    }

    [Fact]
    public void RoundTrip_PreservesDimensions()
    {
        var original = Plt.Create()
            .WithSize(1024, 768)
            .WithDpi(150)
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal(1024, restored.Width);
        Assert.Equal(768, restored.Height);
        Assert.Equal(150, restored.Dpi);
    }

    [Fact]
    public void RoundTrip_PreservesLineSeries()
    {
        var original = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], line =>
            {
                line.Color = Color.Red;
                line.LineWidth = 2.5;
                line.Label = "My Line";
                line.LineStyle = LineStyle.Dashed;
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Single(restored.SubPlots);
        var series = Assert.IsType<LineSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Color.Red, series.Color);
        Assert.Equal(2.5, series.LineWidth);
        Assert.Equal("My Line", series.Label);
        Assert.Equal(LineStyle.Dashed, series.LineStyle);
        Assert.Equal([1.0, 2.0, 3.0], series.XData);
        Assert.Equal([4.0, 5.0, 6.0], series.YData);
    }

    [Fact]
    public void RoundTrip_PreservesScatterSeries()
    {
        var original = Plt.Create()
            .Scatter([1.0, 2.0], [3.0, 4.0], s => s.Color = Color.Blue)
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        var series = Assert.IsType<ScatterSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Color.Blue, series.Color);
    }

    [Fact]
    public void RoundTrip_PreservesBarSeries()
    {
        var original = Plt.Create()
            .Bar(["A", "B"], [10.0, 20.0])
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        var series = Assert.IsType<BarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(["A", "B"], series.Categories);
        Assert.Equal([10.0, 20.0], series.Values);
    }

    [Fact]
    public void RoundTrip_PreservesMultipleSubPlots()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Left").Plot([1.0], [2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Right").Scatter([3.0], [4.0]))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal(2, restored.SubPlots.Count);
        Assert.Equal("Left", restored.SubPlots[0].Title);
        Assert.Equal("Right", restored.SubPlots[1].Title);
        Assert.IsType<LineSeries>(restored.SubPlots[0].Series[0]);
        Assert.IsType<ScatterSeries>(restored.SubPlots[1].Series[0]);
    }

    [Fact]
    public void RoundTrip_PreservesAxisConfig()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("Time")
                  .SetYLabel("Value")
                  .SetXLim(0, 100)
                  .SetYLim(-50, 50)
                  .Plot([1.0], [2.0]);
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Equal("Time", restored.SubPlots[0].XAxis.Label);
        Assert.Equal("Value", restored.SubPlots[0].YAxis.Label);
        Assert.Equal(0, restored.SubPlots[0].XAxis.Min);
        Assert.Equal(100, restored.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void ToJson_Indented_IsReadable()
    {
        var figure = Plt.Create().WithTitle("Test").Build();
        string json = ChartServices.Serializer.ToJson(figure, indented: true);
        Assert.Contains("\n", json);
    }

    [Fact]
    public void RoundTrip_PreservesHistogramSeries()
    {
        var figure = Plt.Create().Hist([1.0, 2.0, 3.0, 4.0, 5.0], 5).Build();
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<HistogramSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(5, series.Bins);
    }

    [Fact]
    public void RoundTrip_PreservesPieSeries()
    {
        var figure = Plt.Create().Pie([30.0, 70.0], ["A", "B"]).Build();
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<PieSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
        Assert.Equal(["A", "B"], series.Labels);
    }

    [Fact]
    public void RoundTrip_PreservesBoxSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var box = axes.BoxPlot([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]]);
        box.Label = "BoxTest";
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<BoxSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal("BoxTest", series.Label);
        Assert.Equal(2, series.Datasets.Length);
    }

    [Fact]
    public void RoundTrip_PreservesViolinSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Violin([[1.0, 2.0, 3.0]]);
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<ViolinSeries>(restored.SubPlots[0].Series[0]);
    }

    [Fact]
    public void RoundTrip_PreservesStemSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Stem([1.0, 2.0], [3.0, 4.0]);
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<StemSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([1.0, 2.0], series.XData);
        Assert.Equal([3.0, 4.0], series.YData);
    }

    [Fact]
    public void RoundTrip_PreservesContourSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Contour([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } });
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<ContourSeries>(restored.SubPlots[0].Series[0]);
    }

    [Fact]
    public void RoundTrip_PreservesHeatmapSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<HeatmapSeries>(restored.SubPlots[0].Series[0]);
    }

    [Fact]
    public void RoundTrip_PreservesLabelAcrossAllTypes()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var line = axes.Plot([1.0], [2.0]);
        line.Label = "shared-label";
        var json = ChartServices.Serializer.ToJson(figure);
        var restored = ChartServices.Serializer.FromJson(json);
        Assert.Equal("shared-label", restored.SubPlots[0].Series[0].Label);
    }
}
