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
    public void RoundTrip_PreservesSecondaryYAxis()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.TwinX();
        axes.SecondaryYAxis!.Label = "Right";
        axes.PlotSecondary([1.0, 2.0], [100.0, 200.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.NotNull(restored.SubPlots[0].SecondaryYAxis);
        Assert.Equal("Right", restored.SubPlots[0].SecondaryYAxis!.Label);
        Assert.Single(restored.SubPlots[0].SecondarySeries);
    }

    [Fact]
    public void RoundTrip_PreservesAnnotations()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        var ann = axes.Annotate("peak", 2.0, 4.0);
        ann.ArrowTargetX = 1.5;
        ann.ArrowTargetY = 3.5;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.Single(restored.SubPlots[0].Annotations);
        Assert.Equal("peak", restored.SubPlots[0].Annotations[0].Text);
        Assert.Equal(1.5, restored.SubPlots[0].Annotations[0].ArrowTargetX);
    }

    [Fact]
    public void RoundTrip_PreservesReferenceLines()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.AxHLine(3.5);
        axes.AxVLine(1.5);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.Equal(2, restored.SubPlots[0].ReferenceLines.Count);
    }

    [Fact]
    public void RoundTrip_PreservesSpanRegions()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Plot([1.0, 2.0], [3.0, 4.0]);
        axes.AxHSpan(3.0, 3.5);
        axes.AxVSpan(1.2, 1.8);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.Equal(2, restored.SubPlots[0].Spans.Count);
    }

    [Fact]
    public void RoundTrip_PreservesRadarSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var radar = axes.Radar(["Speed", "Power", "Range"], [8.0, 6.0, 9.0]);
        radar.Alpha = 0.5;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<RadarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(["Speed", "Power", "Range"], series.Categories);
        Assert.Equal([8.0, 6.0, 9.0], series.Values);
        Assert.Equal(0.5, series.Alpha);
    }

    [Fact]
    public void RoundTrip_PreservesBarMode()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetBarMode(BarMode.Stacked)
                .Bar(["A", "B"], [10.0, 20.0]))
            .Build();

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.Equal(BarMode.Stacked, restored.SubPlots[0].BarMode);
    }

    [Fact]
    public void RoundTrip_PreservesQuiverSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Quiver([1.0, 2.0], [3.0, 4.0], [0.5, -0.5], [0.5, 0.5]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<QuiverSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([0.5, -0.5], series.UData);
        Assert.Equal([0.5, 0.5], series.VData);
    }

    [Fact]
    public void RoundTrip_PreservesCandlestickSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Candlestick([10.0, 12.0], [15.0, 14.0], [8.0, 10.0], [13.0, 11.0], ["Mon", "Tue"]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<CandlestickSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([10.0, 12.0], series.Open);
        Assert.Equal(["Mon", "Tue"], series.DateLabels);
    }

    [Fact]
    public void RoundTrip_PreservesErrorBarSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var eb = axes.ErrorBar([1.0, 2.0], [3.0, 4.0], [0.5, 0.5], [1.0, 1.0]);
        eb.CapSize = 8.0;
        eb.Color = Color.Red;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<ErrorBarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Color.Red, series.Color);
        Assert.Equal(8.0, series.CapSize);
        Assert.Equal([0.5, 0.5], series.YErrorLow);
        Assert.Equal([1.0, 1.0], series.YErrorHigh);
    }

    [Fact]
    public void RoundTrip_PreservesStepSeries()
    {
        var figure = Plt.Create()
            .Step([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], s => s.StepPosition = StepPosition.Pre)
            .Build();

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<StepSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(StepPosition.Pre, series.StepPosition);
        Assert.Equal([1.0, 2.0, 3.0], series.XData);
    }

    [Fact]
    public void RoundTrip_PreservesAreaSeries()
    {
        var figure = Plt.Create()
            .FillBetween([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [1.0, 2.0, 3.0], s =>
            {
                s.Color = Color.Blue;
                s.Alpha = 0.5;
            })
            .Build();

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<AreaSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Color.Blue, series.Color);
        Assert.Equal(0.5, series.Alpha);
        Assert.Equal([1.0, 2.0, 3.0], series.XData);
        Assert.Equal([4.0, 5.0, 6.0], series.YData);
        Assert.Equal([1.0, 2.0, 3.0], series.YData2);
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
