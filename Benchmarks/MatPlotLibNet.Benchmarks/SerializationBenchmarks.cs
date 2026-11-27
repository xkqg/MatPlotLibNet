// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private readonly IChartSerializer _serializer = new ChartSerializer();
    private Figure _figure = default!;
    private string _json = default!;
    private Figure _rich = default!;
    private string _richJson = default!;

    [GlobalSetup]
    public void Setup()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _figure = Plt.Create()
            .WithTitle("Serialization Test")
            .WithTheme(Theme.Dark)
            .Plot(x, y, l => { l.Color = Colors.Blue; l.Label = "Data"; })
            .Bar(["Q1", "Q2", "Q3", "Q4"], [100.0, 250, 180, 320])
            .Build();

        _json = _serializer.ToJson(_figure);

        // A figure that carries what the first one does not: colour-mapped series (a heatmap, a filled contour, a scatter), a
        // histogram and a pie with their flags, and a theme that is not the resting one, so the colour-scale, flag, enum and
        // kind-name writes and the optional-field reads all run.
        var grid = new double[20, 20];
        for (int i = 0; i < 20; i++)
        {
            for (int j = 0; j < 20; j++)
            {
                grid[i, j] = Math.Sin(i * 0.3) * Math.Cos(j * 0.3);
            }
        }

        var axis = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
        _rich = Plt.Create()
            .WithTitle("Rich Serialization Test")
            .WithTheme(Theme.Seaborn)
            .AddSubPlot(2, 3, 1, ax => ax.Heatmap(grid, h => h.ShowLabels = true))
            .AddSubPlot(2, 3, 2, ax => ax.Contourf(axis, axis, grid))
            .AddSubPlot(2, 3, 3, ax => ax.Scatter(x, y, s => s.ColorMap = Styling.ColorMaps.ColorMaps.Viridis))
            .AddSubPlot(2, 3, 4, ax => ax.Hist(y, 12, h => { h.Density = true; h.Cumulative = true; }))
            .AddSubPlot(2, 3, 5, ax => ax.Pie([30, 20, 50], ["a", "b", "c"], p => p.CounterClockwise = true))
            .AddSubPlot(2, 3, 6, ax => ax.Bar(["Q1", "Q2", "Q3"], [3.0, 2, 5]))
            .Build();
        _richJson = _serializer.ToJson(_rich);
        _everyEnumJson = _serializer.ToJson(EveryEnumFigure(x, y, grid));

        // A figure under the classic matplotlib theme, whose looks say something under nearly every key of the theme record, so
        // the write and the read of every look's keys run.
        _everyLook = Plt.Create().WithTheme(Theme.MatplotlibClassic).Plot(x, y).Build();
        _everyLookJson = _serializer.ToJson(_everyLook);
    }

    private Figure _everyLook = default!;
    private string _everyLookJson = default!;

    [Benchmark]
    public string ToJsonEveryLook() => _serializer.ToJson(_everyLook);

    [Benchmark]
    public Figure FromJsonEveryLook() => _serializer.FromJson(_everyLookJson);

    private string _everyEnumJson = default!;

    /// <summary>A figure that writes every enum field a document carries off its resting value: the ones written as a lower-case
    /// or PascalCase name (orientations, styles, scales, layouts, kinds) and the enum-typed ones (markers, alignments, the legend
    /// position), so a read runs every enum decode the serializer has.</summary>
    private static Figure EveryEnumFigure(double[] x, double[] y, double[,] grid)
    {
        var figure = new Figure { Width = 900, Height = 600 };
        var first = figure.AddSubPlot(2, 3, 1);
        first.BarMode = BarMode.Stacked;
        first.Bar(["a", "b"], [1.0, 2]).Orientation = Models.Series.BarOrientation.Horizontal;
        first.Countplot(["a", "b", "a"]).Orientation = Models.Series.BarOrientation.Horizontal;
        first.FillBetweenX(y, x);
        first.Step(x, y).StepPosition = Models.Series.StepPosition.Mid;
        var line = first.Plot(x, y);
        line.LineStyle = LineStyle.Dashed;
        line.Marker = MarkerStyle.Circle;
        line.LabelPlacement = Models.Series.SeriesLabelPlacement.LineEnd;
        line.AsksAs = Models.Series.CycleCall.Scatter;
        first.XAxis.Scale = AxisScale.Log;
        first.AddXBreak(10, 20, BreakStyle.Straight);
        first.Spines = first.Spines with { Left = first.Spines.Left with { Position = SpinePosition.Data } };
        var annotation = first.Annotate("note", 1, 1);
        annotation.ConnectionStyle = ConnectionStyle.Arc3;
        annotation.BoxStyle = BoxStyle.Round;
        annotation.Alignment = Rendering.TextAlignment.Left;
        first.AxVLine(3).LineStyle = LineStyle.Dotted;
        first.AxVSpan(1, 2).LineStyle = LineStyle.Dashed;
        first.AddTrendline(0, 0, 1, 1).LineStyle = LineStyle.Dotted;
        first.AddLevel(5).LineStyle = LineStyle.Dashed;
        first.Legend = first.Legend with { Position = LegendPosition.UpperLeft };

        figure.AddSubPlot(2, 3, 2).Bullet(0.5).Orientation = Orientation.Vertical;
        figure.AddSubPlot(2, 3, 3).Heatmap(grid).MaskMode = Models.Series.HeatmapMaskMode.UpperTriangle;
        figure.AddSubPlot(2, 3, 4).NetworkGraph([new("a"), new("b")], [new("a", "b")]).Layout = Models.Series.GraphLayout.ForceDirected;
        figure.AddSubPlot(2, 3, 5).PairGrid([x[..20], y[..20]], p =>
        {
            p.DiagonalKind = Models.Series.PairGridDiagonalKind.Kde;
            p.OffDiagonalKind = Models.Series.PairGridOffDiagonalKind.None;
            p.Triangular = Models.Series.PairGridTriangle.LowerOnly;
        });
        var polar = figure.AddSubPlot(2, 3, 6);
        polar.CoordinateSystem = CoordinateSystem.Polar;
        polar.RelativeRotation([y], y, ["a"]).Formula = Models.Series.RrgFormula.ZScore;
        return figure;
    }

    [Benchmark]
    public Figure FromJsonEveryEnum() => _serializer.FromJson(_everyEnumJson);

    [Benchmark]
    public string ToJsonRich() => _serializer.ToJson(_rich);

    [Benchmark]
    public Figure FromJsonRich() => _serializer.FromJson(_richJson);

    [Benchmark]
    public string ToJson() => _serializer.ToJson(_figure);

    [Benchmark]
    public string ToJsonIndented() => _serializer.ToJson(_figure, indented: true);

    [Benchmark]
    public Figure FromJson() => _serializer.FromJson(_json);

    [Benchmark]
    public Figure RoundTrip()
    {
        var json = _serializer.ToJson(_figure);
        return _serializer.FromJson(json);
    }
}
