// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies <see cref="ChartServices"/> serialization behavior.</summary>
public class ChartSerializerTests
{
    /// <summary>Verifies that ToJson produces a valid JSON document with the expected title.</summary>
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

    /// <summary>Verifies that ToJson includes width and height dimensions.</summary>
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

    /// <summary>Verifies that ToJson includes a type discriminator for series.</summary>
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

    /// <summary>Verifies that ToJson includes line series data and label.</summary>
    [Fact]
    public void ToJson_IncludesLineSeries()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], line =>
            {
                line.Color = Colors.Red;
                line.LineWidth = 2.5;
                line.Label = "My Line";
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.Contains("line", json);
        Assert.Contains("My Line", json);
    }

    /// <summary>Verifies that a round-trip serialization preserves the figure title.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves width, height, and DPI.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves LineSeries properties.</summary>
    [Fact]
    public void RoundTrip_PreservesLineSeries()
    {
        var original = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], line =>
            {
                line.Color = Colors.Red;
                line.LineWidth = 2.5;
                line.Label = "My Line";
                line.LineStyle = LineStyle.Dashed;
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        Assert.Single(restored.SubPlots);
        var series = Assert.IsType<LineSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Red, series.Color);
        Assert.Equal(2.5, series.LineWidth);
        Assert.Equal("My Line", series.Label);
        Assert.Equal(LineStyle.Dashed, series.LineStyle);
        Assert.Equal([1.0, 2.0, 3.0], series.XData);
        Assert.Equal([4.0, 5.0, 6.0], series.YData);
    }

    /// <summary>Verifies that a round-trip serialization preserves ScatterSeries color.</summary>
    [Fact]
    public void RoundTrip_PreservesScatterSeries()
    {
        var original = Plt.Create()
            .Scatter([1.0, 2.0], [3.0, 4.0], s => s.Color = Colors.Blue)
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);

        var series = Assert.IsType<ScatterSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Blue, series.Color);
    }

    /// <summary>Verifies that a round-trip serialization preserves BarSeries categories and values.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves multiple subplots with distinct series types.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves axis labels and limits.</summary>
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

    /// <summary>Verifies that ToJson with indentation produces multi-line output.</summary>
    [Fact]
    public void ToJson_Indented_IsReadable()
    {
        var figure = Plt.Create().WithTitle("Test").Build();
        string json = ChartServices.Serializer.ToJson(figure, indented: true);
        Assert.Contains("\n", json);
    }

    /// <summary>Verifies that a round-trip serialization preserves HistogramSeries bin count.</summary>
    [Fact]
    public void RoundTrip_PreservesHistogramSeries()
    {
        var figure = Plt.Create().Hist([1.0, 2.0, 3.0, 4.0, 5.0], 5).Build();
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<HistogramSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(5, series.Bins);
    }

    /// <summary>Verifies that a round-trip serialization preserves PieSeries sizes and labels.</summary>
    [Fact]
    public void RoundTrip_PreservesPieSeries()
    {
        var figure = Plt.Create().Pie([30.0, 70.0], ["A", "B"]).Build();
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<PieSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
        Assert.Equal(["A", "B"], series.Labels!);
    }

    /// <summary>Verifies that a round-trip serialization preserves BoxSeries label and datasets.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves ViolinSeries type.</summary>
    [Fact]
    public void RoundTrip_PreservesViolinSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Violin([[1.0, 2.0, 3.0]]);
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<ViolinSeries>(restored.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that a round-trip serialization preserves StemSeries X and Y data.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves ContourSeries type.</summary>
    [Fact]
    public void RoundTrip_PreservesContourSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Contour([1.0, 2.0], [1.0, 2.0], new double[,] { { 1, 2 }, { 3, 4 } });
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<ContourSeries>(restored.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that a round-trip serialization preserves HeatmapSeries type.</summary>
    [Fact]
    public void RoundTrip_PreservesHeatmapSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.IsType<HeatmapSeries>(restored.SubPlots[0].Series[0]);
    }

    /// <summary>Verifies that a round-trip serialization preserves the secondary Y axis and its series.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves annotation text and arrow targets.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves horizontal and vertical reference lines.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves horizontal and vertical span regions.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves RadarSeries categories, values, and alpha.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves the subplot bar mode.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves QuiverSeries vector data.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves CandlestickSeries OHLC data and date labels.</summary>
    [Fact]
    public void RoundTrip_PreservesCandlestickSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Candlestick([10.0, 12.0], [15.0, 14.0], [8.0, 10.0], [13.0, 11.0], ["Mon", "Tue"]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<CandlestickSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([10.0, 12.0], series.Open);
        Assert.Equal(["Mon", "Tue"], series.DateLabels!);
    }

    /// <summary>Verifies that a round-trip serialization preserves ErrorBarSeries color, cap size, and error ranges.</summary>
    [Fact]
    public void RoundTrip_PreservesErrorBarSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var eb = axes.ErrorBar([1.0, 2.0], [3.0, 4.0], [0.5, 0.5], [1.0, 1.0]);
        eb.CapSize = 8.0;
        eb.Color = Colors.Red;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<ErrorBarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Red, series.Color);
        Assert.Equal(8.0, series.CapSize);
        Assert.Equal([0.5, 0.5], series.YErrorLow);
        Assert.Equal([1.0, 1.0], series.YErrorHigh);
    }

    /// <summary>Verifies that a round-trip serialization preserves StepSeries position and data.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves AreaSeries color, alpha, and fill data.</summary>
    [Fact]
    public void RoundTrip_PreservesAreaSeries()
    {
        var figure = Plt.Create()
            .FillBetween([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [1.0, 2.0, 3.0], s =>
            {
                s.Color = Colors.Blue;
                s.Alpha = 0.5;
            })
            .Build();

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<AreaSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Blue, series.Color);
        Assert.Equal(0.5, series.Alpha);
        Assert.Equal([1.0, 2.0, 3.0], series.XData);
        Assert.Equal([4.0, 5.0, 6.0], series.YData);
        Assert.Equal([1.0, 2.0, 3.0], series.YData2!);
    }

    /// <summary>Verifies that a round-trip serialization preserves the label on any series type.</summary>
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

    /// <summary>Verifies that a round-trip serialization preserves DonutSeries properties including inner radius and center text.</summary>
    [Fact]
    public void RoundTrip_PreservesDonutSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var donut = axes.Donut([30.0, 70.0], ["A", "B"]);
        donut.InnerRadius = 0.5;
        donut.CenterText = "Total";
        donut.StartAngle = 45;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<DonutSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
        Assert.Equal(["A", "B"], series.Labels!);
        Assert.Equal(0.5, series.InnerRadius);
        Assert.Equal("Total", series.CenterText);
        Assert.Equal(45, series.StartAngle);
    }

    /// <summary>Verifies that a round-trip serialization preserves BubbleSeries positions, sizes, and styling.</summary>
    [Fact]
    public void RoundTrip_PreservesBubbleSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var bubble = axes.Bubble([1.0, 2.0], [3.0, 4.0], [10.0, 20.0]);
        bubble.Color = Colors.Blue;
        bubble.Alpha = 0.8;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<BubbleSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([1.0, 2.0], series.XData);
        Assert.Equal([3.0, 4.0], series.YData);
        Assert.Equal([10.0, 20.0], series.Sizes);
        Assert.Equal(Colors.Blue, series.Color);
        Assert.Equal(0.8, series.Alpha);
    }

    /// <summary>Verifies that a round-trip serialization preserves OhlcBarSeries OHLC data and tick width.</summary>
    [Fact]
    public void RoundTrip_PreservesOhlcBarSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var ohlc = axes.OhlcBar([10.0, 12.0], [15.0, 14.0], [8.0, 10.0], [13.0, 11.0], ["Mon", "Tue"]);
        ohlc.TickWidth = 0.5;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<OhlcBarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([10.0, 12.0], series.Open);
        Assert.Equal([15.0, 14.0], series.High);
        Assert.Equal([8.0, 10.0], series.Low);
        Assert.Equal([13.0, 11.0], series.Close);
        Assert.Equal(["Mon", "Tue"], series.DateLabels!);
        Assert.Equal(0.5, series.TickWidth);
    }

    /// <summary>Verifies that a round-trip serialization preserves WaterfallSeries categories, values, and bar width.</summary>
    [Fact]
    public void RoundTrip_PreservesWaterfallSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var wf = axes.Waterfall(["Start", "Add", "Total"], [100.0, 50.0, 150.0]);
        wf.BarWidth = 0.8;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<WaterfallSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(["Start", "Add", "Total"], series.Categories);
        Assert.Equal([100.0, 50.0, 150.0], series.Values);
        Assert.Equal(0.8, series.BarWidth);
    }

    /// <summary>Verifies that a round-trip serialization preserves FunnelSeries labels and values.</summary>
    [Fact]
    public void RoundTrip_PreservesFunnelSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        axes.Funnel(["Leads", "Qualified", "Won"], [1000.0, 500.0, 100.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<FunnelSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(["Leads", "Qualified", "Won"], series.Labels);
        Assert.Equal([1000.0, 500.0, 100.0], series.Values);
    }

    /// <summary>Verifies that a round-trip serialization preserves GanttSeries tasks, timing, and styling.</summary>
    [Fact]
    public void RoundTrip_PreservesGanttSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var gantt = axes.Gantt(["Task A", "Task B"], [0.0, 2.0], [3.0, 5.0]);
        gantt.Color = Colors.Green;
        gantt.BarHeight = 0.8;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<GanttSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(["Task A", "Task B"], series.Tasks);
        Assert.Equal([0.0, 2.0], series.Starts);
        Assert.Equal([3.0, 5.0], series.Ends);
        Assert.Equal(Colors.Green, series.Color);
        Assert.Equal(0.8, series.BarHeight);
    }

    /// <summary>Verifies that a round-trip serialization preserves GaugeSeries value, range, and needle color.</summary>
    [Fact]
    public void RoundTrip_PreservesGaugeSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var gauge = axes.Gauge(75.0);
        gauge.Min = 0;
        gauge.Max = 150;
        gauge.NeedleColor = Colors.Red;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<GaugeSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(75.0, series.Value);
        Assert.Equal(0, series.Min);
        Assert.Equal(150, series.Max);
        Assert.Equal(Colors.Red, series.NeedleColor);
    }

    /// <summary>Verifies that a round-trip serialization preserves ProgressBarSeries value, colors, and bar height.</summary>
    [Fact]
    public void RoundTrip_PreservesProgressBarSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var pb = axes.ProgressBar(0.75);
        pb.FillColor = Colors.Green;
        pb.TrackColor = Color.FromHex("#CCCCCC");
        pb.BarHeight = 0.5;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<ProgressBarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(0.75, series.Value);
        Assert.Equal(Colors.Green, series.FillColor);
        Assert.Equal(Color.FromHex("#CCCCCC"), series.TrackColor);
        Assert.Equal(0.5, series.BarHeight);
    }

    /// <summary>Verifies that a round-trip serialization preserves SparklineSeries values, color, and line width.</summary>
    [Fact]
    public void RoundTrip_PreservesSparklineSeries()
    {
        var figure = new Figure();
        var axes = figure.AddSubPlot();
        var sparkline = axes.Sparkline([1.0, 3.0, 2.0, 5.0, 4.0]);
        sparkline.Color = Colors.Blue;
        sparkline.LineWidth = 2.0;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var series = Assert.IsType<SparklineSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal([1.0, 3.0, 2.0, 5.0, 4.0], series.Values);
        Assert.Equal(Colors.Blue, series.Color);
        Assert.Equal(2.0, series.LineWidth);
    }

    // --- GridSpec serialization ---

    /// <summary>Verifies that a GridSpec with ratios survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridSpec()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 3, Cols = 2, HeightRatios = [1, 2, 1], WidthRatios = [1, 3] } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, GridPosition.Single(0, 0)).Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        Assert.NotNull(restored.GridSpec);
        Assert.Equal(3, restored.GridSpec.Rows);
        Assert.Equal(2, restored.GridSpec.Cols);
        Assert.Equal([1.0, 2.0, 1.0], restored.GridSpec.HeightRatios);
        Assert.Equal([1.0, 3.0], restored.GridSpec.WidthRatios);
    }

    /// <summary>Verifies that a GridPosition survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridPosition()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 2, Cols = 2 } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, GridPosition.Single(0, 1)).Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        Assert.NotNull(restored.SubPlots[0].GridPosition);
        var pos = restored.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(1, pos.RowEnd);
        Assert.Equal(1, pos.ColStart);
        Assert.Equal(2, pos.ColEnd);
    }

    /// <summary>Verifies that a spanning GridPosition survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesGridPosition_Spanning()
    {
        var figure = new Figure { GridSpec = new GridSpec { Rows = 3, Cols = 3 } };
        var gs = figure.GridSpec;
        figure.AddSubPlot(gs, new GridPosition(0, 2, 0, 3)).Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        var pos = restored.SubPlots[0].GridPosition!.Value;
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(2, pos.RowEnd);
        Assert.Equal(0, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    /// <summary>Verifies that a figure without GridSpec omits the gridSpec field from JSON.</summary>
    [Fact]
    public void RoundTrip_NoGridSpec_OmitsFromJson()
    {
        var figure = Plt.Create().Plot([1.0], [2.0]).Build();

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.DoesNotContain("gridSpec", json);
    }

    // --- Spines serialization ---

    /// <summary>Verifies that hidden spines survive JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesSpinesConfig()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with
        {
            Top = new SpineConfig() with { Visible = false },
            Right = new SpineConfig() with { Visible = false }
        };
        ax.Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.False(restored.SubPlots[0].Spines.Top.Visible);
        Assert.False(restored.SubPlots[0].Spines.Right.Visible);
        Assert.True(restored.SubPlots[0].Spines.Bottom.Visible);
        Assert.True(restored.SubPlots[0].Spines.Left.Visible);
    }

    /// <summary>Verifies that spine data position survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesSpinePosition()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Spines = ax.Spines with
        {
            Bottom = new SpineConfig() with { Position = SpinePosition.Data, PositionValue = 0.0 }
        };
        ax.Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        Assert.Equal(SpinePosition.Data, restored.SubPlots[0].Spines.Bottom.Position);
        Assert.Equal(0.0, restored.SubPlots[0].Spines.Bottom.PositionValue);
    }

    /// <summary>Verifies that default spines are omitted from JSON.</summary>
    [Fact]
    public void RoundTrip_DefaultSpines_OmitsFromJson()
    {
        var figure = new Figure();
        figure.AddSubPlot().Plot([1.0], [2.0]);

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.DoesNotContain("spines", json);
    }

    // --- Shared axes serialization ---

    /// <summary>Verifies that shared X axis survives JSON round-trip via keys.</summary>
    [Fact]
    public void RoundTrip_PreservesSharedXAxis()
    {
        var figure = new Figure();
        var ax1 = figure.AddSubPlot(2, 1, 1);
        ax1.Key = "price";
        ax1.Plot([1.0, 2.0], [3.0, 4.0]);

        var ax2 = figure.AddSubPlot(2, 1, 2, sharex: ax1);
        ax2.Key = "volume";
        ax2.Plot([1.0, 2.0], [5.0, 6.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        Assert.NotNull(restored.SubPlots[1].ShareXWith);
        Assert.Same(restored.SubPlots[0], restored.SubPlots[1].ShareXWith);
    }

    /// <summary>Verifies that shared Y axis survives JSON round-trip via keys.</summary>
    [Fact]
    public void RoundTrip_PreservesSharedYAxis()
    {
        var figure = new Figure();
        var ax1 = figure.AddSubPlot(1, 2, 1);
        ax1.Key = "main";
        ax1.Plot([1.0, 2.0], [3.0, 4.0]);

        var ax2 = figure.AddSubPlot(1, 2, 2, sharey: ax1);
        ax2.Key = "side";
        ax2.Plot([1.0, 2.0], [5.0, 6.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        Assert.NotNull(restored.SubPlots[1].ShareYWith);
        Assert.Same(restored.SubPlots[0], restored.SubPlots[1].ShareYWith);
    }

    /// <summary>Verifies that no sharing keys are emitted when not configured.</summary>
    [Fact]
    public void RoundTrip_NoSharing_OmitsKeys()
    {
        var figure = new Figure();
        figure.AddSubPlot().Plot([1.0], [2.0]);

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.DoesNotContain("shareXKey", json);
        Assert.DoesNotContain("shareYKey", json);
    }

    // --- Inset axes serialization ---

    /// <summary>Verifies that an inset with series survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesInsetAxes()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);
        var inset = ax.AddInset(0.6, 0.1, 0.35, 0.35);
        inset.Plot([1.5, 2.0], [3.5, 4.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        Assert.Single(restored.SubPlots[0].Insets);
        Assert.Single(restored.SubPlots[0].Insets[0].Series);
    }

    /// <summary>Verifies that InsetBounds values are preserved.</summary>
    [Fact]
    public void RoundTrip_PreservesInsetBounds()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        ax.Plot([1.0], [2.0]);
        ax.AddInset(0.6, 0.1, 0.35, 0.35).Plot([1.0], [2.0]);

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));

        var bounds = restored.SubPlots[0].Insets[0].InsetBounds!.Value;
        Assert.Equal(0.6, bounds.X);
        Assert.Equal(0.1, bounds.Y);
        Assert.Equal(0.35, bounds.Width);
        Assert.Equal(0.35, bounds.Height);
    }

    /// <summary>Verifies that colormap name survives JSON round-trip.</summary>
    [Fact]
    public void RoundTrip_PreservesColorMapName()
    {
        var figure = new Figure();
        var ax = figure.AddSubPlot();
        var hs = ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } });
        hs.ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma;

        var restored = ChartServices.Serializer.FromJson(ChartServices.Serializer.ToJson(figure));
        var restoredHs = (HeatmapSeries)restored.SubPlots[0].Series[0];
        Assert.NotNull(restoredHs.ColorMap);
        Assert.Equal("plasma", restoredHs.ColorMap!.Name);
    }

    /// <summary>Verifies that no insets field is emitted when not configured.</summary>
    [Fact]
    public void RoundTrip_NoInsets_OmitsFromJson()
    {
        var figure = new Figure();
        figure.AddSubPlot().Plot([1.0], [2.0]);

        string json = ChartServices.Serializer.ToJson(figure);
        Assert.DoesNotContain("insets", json);
        Assert.DoesNotContain("insetBounds", json);
    }
}
