// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks;

[MemoryDiagnoser]
public class SvgRenderingBenchmarks
{
    private Figure _simpleLine = default!;
    private Figure _complexChart = default!;
    private Figure _subplotGrid = default!;
    private Figure _treemap = default!;
    private Figure _sunburst = default!;
    private Figure _sankey = default!;
    private Figure _polar = default!;
    private Figure _surface3D = default!;
    private Figure _surface3DLit = default!;
    private Figure _legendChart = default!;
    private Figure _largeLine10K = default!;
    private Figure _largeLine100K = default!;
    private Figure _gappedLine100K = default!;
    private Figure _scatter3D = default!;
    private Figure _surface3DRotating = default!;
    private Figure _scatter3DRotating = default!;
    private Figure _pie = default!;
    private Figure _minorGrid = default!;
    private Figure _violin = default!;
    private Figure _kde = default!;
    private Figure _pairGridKde = default!;
    private Figure _dots = default!;
    private Figure _dotsLibrary = default!;
    private Figure _persistentLog = default!;
    private Figure _persistentSymlog = default!;
    private Figure _brokenAxes = default!;
    private Figure _tightGrid = default!;
    private Figure _groupedBars = default!;
    private Figure _stackedBars = default!;
    private Figure _twoSlotY = default!;
    private Figure _treemap600 = default!;
    private Figure _wordCloud100 = default!;
    private Figure _relativeRotation = default!;
    private Figure _treemapSmall = default!;
    private Figure _icicle = default!;
    private Figure _sunburstLabelled = default!;
    private Figure _sankeyInside = default!;
    private Figure _pieAutoPct = default!;
    private Figure _quiverKey = default!;
    private Figure _colorBarBelow = default!;
    private Figure _figureColorBarBelow = default!;
    private Figure _polarMixed = default!;
    private Figure _ternary = default!;
    private Figure _logOnePower = default!;
    private Figure _image256 = default!;
    private Figure _gantt = default!;
    private Figure _matShow = default!;
    private Figure _histogram2D = default!;
    private Figure _pcolormesh = default!;
    private Figure _spectrogram = default!;

    [GlobalSetup]
    public void Setup()
    {
        var x = Enumerable.Range(0, 100).Select(i => (double)i).ToArray();
        var y = x.Select(v => Math.Sin(v * 0.1) * 50 + 50).ToArray();

        _simpleLine = Plt.Create()
            .WithTitle("Simple Line")
            .Plot(x, y)
            .Build();

        _complexChart = Plt.Create()
            .WithTitle("Complex")
            .WithTheme(Theme.Seaborn)
            .Plot(x, y, l => { l.Color = Colors.Blue; l.Label = "sin"; })
            .Scatter(x.Take(20).ToArray(), y.Take(20).ToArray(), s => { s.Color = Colors.Red; })
            .Bar(["A", "B", "C", "D"], [10.0, 25, 15, 30])
            .Build();

        _subplotGrid = Plt.Create()
            .WithTitle("3x3 Grid")
            .AddSubPlot(3, 3, 1, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 2, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 3, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 4, ax => ax.Bar(["A", "B"], [10.0, 20]))
            .AddSubPlot(3, 3, 5, ax => ax.Bar(["C", "D"], [30.0, 40]))
            .AddSubPlot(3, 3, 6, ax => ax.Scatter(x.Take(10).ToArray(), y.Take(10).ToArray()))
            .AddSubPlot(3, 3, 7, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 8, ax => ax.Plot(x, y))
            .AddSubPlot(3, 3, 9, ax => ax.Plot(x, y))
            .Build();

        var tree = new TreeNode
        {
            Label = "Root",
            Children = [
                new TreeNode { Label = "A", Value = 40, Children = [
                    new TreeNode { Label = "A1", Value = 20 },
                    new TreeNode { Label = "A2", Value = 20 }
                ]},
                new TreeNode { Label = "B", Value = 30 },
                new TreeNode { Label = "C", Value = 20 },
                new TreeNode { Label = "D", Value = 10 }
            ]
        };

        _treemap = Plt.Create().Treemap(tree).Build();
        _sunburst = Plt.Create().Sunburst(tree).Build();

        _sankey = Plt.Create().Sankey(
            [new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C"), new SankeyNode("D")],
            [new SankeyLink(0, 2, 30), new SankeyLink(0, 3, 20), new SankeyLink(1, 2, 10), new SankeyLink(1, 3, 15)]
        ).Build();

        var r = Enumerable.Range(0, 50).Select(i => (double)i / 5).ToArray();
        var theta = r.Select(v => v * 0.5).ToArray();
        _polar = Plt.Create().PolarPlot(r, theta).Build();

        var sx = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sy = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sz = new double[10, 10];
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                sz[i, j] = Math.Sin(i * 0.5) * Math.Cos(j * 0.5);
        _surface3D = Plt.Create().Surface(sx, sy, sz).Build();

        _surface3DLit = Plt.Create()
            .Surface(sx, sy, sz)
            .WithLighting(0.5, 0.5, 1.0)
            .Build();

        _legendChart = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y, s => s.Label = "Line 1")
                .Plot(x, y.Select(v => v * 0.8).ToArray(), s => s.Label = "Line 2")
                .Plot(x, y.Select(v => v * 0.6).ToArray(), s => s.Label = "Line 3")
                .WithLegend())
            .Build();

        var x10K = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
        var y10K = x10K.Select(v => Math.Sin(v * 0.01) * 50 + 50).ToArray();
        _largeLine10K = Plt.Create().WithTitle("Large Line 10K").Plot(x10K, y10K).Build();

        var x100K = Enumerable.Range(0, 100_000).Select(i => (double)i).ToArray();
        var y100K = x100K.Select(v => Math.Sin(v * 0.001) * 50 + 50).ToArray();
        _largeLine100K = Plt.Create().WithTitle("Large Line 100K")
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x100K, y100K)
                .WithDownsampling(2000))
            .Build();

        // The same 100,000 points with a missing value every 500: 200 runs that the downsampler reshapes one at a time and joins again.
        var yGapped = y100K.Select((v, i) => i % 500 == 499 ? double.NaN : v).ToArray();
        _gappedLine100K = Plt.Create().WithTitle("Gapped Line 100K")
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x100K, yGapped)
                .WithDownsampling(2000))
            .Build();

        var s3x = Enumerable.Range(0, 2_000).Select(i => Math.Cos(i * 0.05) * (1 + i / 2_000.0)).ToArray();
        var s3y = Enumerable.Range(0, 2_000).Select(i => Math.Sin(i * 0.05) * (1 + i / 2_000.0)).ToArray();
        var s3z = Enumerable.Range(0, 2_000).Select(i => i / 2_000.0).ToArray();
        _scatter3D = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Scatter3D(s3x, s3y, s3z)).Build();

        // The same two scenes in a figure a browser turns: every element carries the vertex data the script reprojects.
        _surface3DRotating = Plt.Create().With3DRotation().Surface(sx, sy, sz).Build();
        _scatter3DRotating = Plt.Create().With3DRotation().AddSubPlot(1, 1, 1, ax => ax.Scatter3D(s3x, s3y, s3z)).Build();

        _pie = Plt.Create().Pie([30.0, 25, 20, 15, 10], ["a", "b", "c", "d", "e"]).Build();

        // One subplot with minor ticks shown and a grid that stands on both the major and the minor ticks.
        _minorGrid = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y)
                .WithMinorTicks()
                .WithGrid(g => g with { Visible = true, Axis = GridAxis.Both, Which = GridWhich.Both }))
            .Build();

        var groups = Enumerable.Range(0, 4)
            .Select(g => Enumerable.Range(0, 300).Select(i => (double)((i * (g + 3)) % 101)).ToArray())
            .ToArray();
        _violin = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Violin(groups)).Build();
        _kde = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Kde(groups[0])).Build();
        int[] hue = [.. Enumerable.Range(0, 300).Select(i => i % 2)];
        _pairGridKde = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PairGrid(groups[..3], s =>
        {
            s.DiagonalKind = PairGridDiagonalKind.Kde;
            s.HueGroups = hue;
        })).Build();

        // The four kinds of dots (a strip, a swarm, a point plot, a residual plot) on one axes, under the default matplotlib style and under the library's own.
        double[] dotsX = [.. Enumerable.Range(0, 60).Select(i => (double)i)];
        double[] dotsY = [.. dotsX.Select(i => (i * 7) % 23 + i * 0.5)];
        _dots = DotsFigure(groups, dotsX, dotsY, null);
        _dotsLibrary = DotsFigure(groups, dotsX, dotsY, Theme.Default);

        // A 256 x 256 image, a Gantt chart of eight tasks and a 12 x 12 matrix shown as matshow does, under the default matplotlib style.
        _image256 = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Image(Image256())).Build();
        string[] tasks = [.. Enumerable.Range(0, 8).Select(i => $"Task {i}")];
        double[] starts = [.. Enumerable.Range(0, 8).Select(i => (double)(i * 3 % 7))];
        double[] ends = [.. starts.Select((s, i) => s + 2 + i % 4)];
        _gantt = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Gantt(tasks, starts, ends)).Build();
        var matrix = new double[12, 12];
        for (int row = 0; row < 12; row++) for (int col = 0; col < 12; col++) matrix[row, col] = (row * 5 + col * 3) % 11;
        _matShow = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MatShow(matrix)).Build();

        // The cell meshes: a 2-D histogram of 5 000 points (whole-number pattern, the same on every platform), a 30 x 30 mesh, and the
        // spectrogram of 2 000 samples at 1 000 per second.
        double[] hx = [.. Enumerable.Range(0, 5_000).Select(i => (i * 7919 % 1000) / 250.0 - 2)];
        double[] hy = [.. Enumerable.Range(0, 5_000).Select(i => (i * 104729 % 1000) / 250.0 - 2)];
        _histogram2D = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Histogram2D(hx, hy, 30)).Build();
        double[] edges = [.. Enumerable.Range(0, 31).Select(i => (double)i)];
        var mesh = new double[30, 30];
        for (int row = 0; row < 30; row++) for (int col = 0; col < 30; col++) mesh[row, col] = (row * 11 + col * 7) % 23;
        _pcolormesh = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pcolormesh(edges, edges, mesh)).Build();
        double[] samples = [.. Enumerable.Range(0, 2_000).Select(i => (i * 37 % 100) / 50.0 - 1)];
        _spectrogram = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Spectrogram(samples, 1000)).Build();

        // Bare scales: no locator of their own, so these are the figures whose first draw the draw-time scale install touches.
        var positive = x.Select(v => Math.Exp(v * 0.05)).ToArray();
        _persistentLog = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, positive).SetYScale(AxisScale.Log)).Build();
        _persistentSymlog = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, y.Select(v => v - 50).ToArray()).SetYScale(AxisScale.SymLog)).Build();

        // A 1 000-point line on an X axis with two breaks, handed over out of order, and a Y axis with one: every transform of the
        // draw builds the break maps, and every point is placed past them.
        var xb = Enumerable.Range(0, 1_000).Select(i => (double)i).ToArray();
        var yb = xb.Select(v => (v * 37 % 100)).ToArray();
        _brokenAxes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(xb, yb)
            .WithXBreak(700, 750).WithXBreak(300, 400).WithYBreak(40, 60)).Build();

        // A tight-layout 3 x 3 grid: every subplot has a title and an X axis title, and one carries its legend outside on the
        // right, so the layout measures the four edges of every subplot and the legend box before the draw.
        var tight = Plt.Create().WithTitle("Tight 3x3").TightLayout();
        for (int i = 1; i <= 9; i++)
        {
            int cell = i;
            tight = tight.AddSubPlot(3, 3, cell, ax =>
            {
                ax.Plot(x, y, s => s.Label = "sin").WithTitle($"panel {cell}").SetXLabel("x").SetYLabel("y");
                if (cell == 6) ax.WithLegend(LegendPosition.OutsideRight);
            });
        }
        _tightGrid = tight.Build();

        // Four bar series over twelve categories, side by side: every draw arranges the bars, each stepping aside in its slot.
        var months = Enumerable.Range(1, 12).Select(i => "m" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        _groupedBars = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            for (int s = 0; s < 4; s++)
            {
                int k = s;
                ax.Bar(months, months.Select((_, i) => (double)((i * 7 + k * 3) % 11 + 1)).ToArray());
            }
        }).Build();

        // Ten bar series of 1 000 columns stacked, some columns falling below zero: every draw works out where each layer
        // stands and the range takes in where each one ends.
        var columns = Enumerable.Range(0, 1_000).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        _stackedBars = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            for (int s = 0; s < 10; s++)
            {
                int k = s;
                ax.Bar(columns, columns.Select((_, i) => (double)((i * 13 + k * 5) % 17 - 4)).ToArray());
            }

            ax.SetBarMode(BarMode.Stacked);
        }).Build();

        // A tight-layout figure with two Y axes on the right and one more on the left of the primary: the layout reserves the
        // band of every slot past the primary, and the draw places and stacks each one.
        _twoSlotY = Plt.Create().TightLayout().AddSubPlot(1, 1, 1, ax => ax
            .Plot(x, y).SetYLabel("primary")
            .WithAddRightYAxis(s => s.Plot(x, y.Select(v => v * 100).ToArray()).SetYLabel("hundreds"))
            .WithAddRightYAxis(s => s.Plot(x, y.Select(v => v * 1000).ToArray()).SetYLabel("thousands"))
            .WithAddLeftYAxis(s => s.Plot(x, y.Select(v => v * 10).ToArray()).SetYLabel("tens"))).Build();

        // A treemap of 12 groups of 10 groups of 5 leaves (600 leaves, values with ties): every draw lays out the children of
        // each of its 133 inner nodes, largest first.
        _treemap600 = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Treemap(new TreeNode
        {
            Label = "Root",
            Children = [.. Enumerable.Range(0, 12).Select(g => new TreeNode
            {
                Label = "G" + g.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Children = [.. Enumerable.Range(0, 10).Select(h => new TreeNode
                {
                    Label = "H" + h.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Children = [.. Enumerable.Range(0, 5).Select(l => new TreeNode { Value = (g * 7 + h * 3 + l * 5) % 9 + 1 })],
                })],
            })],
        })).Build();

        // The small hierarchy, the pie with percentages, the Sankey with its names inside the nodes and the quiver key: figures whose
        // texts stand on a ground the series paints, drawn again after every change.
        _treemapSmall = Plt.Create().Treemap(tree).Build();
        _icicle = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Icicle(tree)).Build();
        _sunburstLabelled = Plt.Create().Sunburst(tree).Build();
        _sankeyInside = Plt.Create().Sankey(
            [new SankeyNode("A"), new SankeyNode("B"), new SankeyNode("C"), new SankeyNode("D")],
            [new SankeyLink(0, 2, 30), new SankeyLink(0, 3, 20), new SankeyLink(1, 2, 10), new SankeyLink(1, 3, 15)],
            s => s.InsideLabels = true).Build();
        _pieAutoPct = Plt.Create().Pie([30.0, 25, 20, 15, 10], ["a", "b", "c", "d", "e"], s => s.AutoPct = "{0:F1}%").Build();
        _quiverKey = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(x, y).QuiverKey(0.8, 0.9, 10, "10 units")).Build();

        // A polar axes with bars, a line over them and a rectangle in data units, and a ternary axes with 300 points, a line and a
        // rectangle: the rows that read what drawing in a circle and in a triangle costs.
        double[] spokes = [.. Enumerable.Range(0, 24).Select(i => i * 2 * Math.PI / 24)];
        double[] lengths = [.. spokes.Select(t => 2 + Math.Sin(3 * t))];
        _polarMixed = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarBar(lengths, spokes).PolarPlot(r, theta)
            .AddPatch(new MatPlotLibNet.Models.Patches.Rectangle(new(0.5, 1), 0.5, 2))).Build();
        double[] top = [.. Enumerable.Range(0, 300).Select(i => (i * 37 % 300) / 300.0)];
        double[] left = [.. top.Select((t, i) => (1 - t) * ((i * 53 % 300) / 300.0))];
        double[] right = [.. top.Select((t, i) => 1 - t - left[i])];
        _ternary = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.TernaryScatter(top, left, right).TernaryPlot(top[..20], left[..20], right[..20])
            .AddPatch(new MatPlotLibNet.Models.Patches.Rectangle(new(0.2, 0.1), 0.3, 0.3))).Build();

        // Two log axes that each show one power of ten under the default matplotlib style, with a major and minor grid: the
        // multiples 2, 3, 4 and 6 of each power are named, every power and multiple has a grid line.
        var logX = Enumerable.Range(0, 100).Select(i => 2 + i * 0.78).ToArray();
        _logOnePower = Plt.Create().WithTheme(Theme.MatplotlibV2).AddSubPlot(1, 1, 1, ax => ax.Plot(logX, logX)
            .SetXScale(AxisScale.Log).SetYScale(AxisScale.Log).SetXLim(2, 80).SetYLim(2, 80)
            .WithGrid(g => g with { Visible = true, Which = GridWhich.Both })).Build();

        // A heatmap under the library's own theme with a colour bar below it, and two heatmaps with the figure's bar below them: the
        // bar stands under the X tick labels and the X axis title, and the layout keeps the room for it.
        var cells = new double[12, 12];
        for (int i = 0; i < 12; i++) for (int j = 0; j < 12; j++) cells[i, j] = i * 12 + j;
        _colorBarBelow = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax => ax.Heatmap(cells).SetXLabel("x")
            .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "value" })).Build();
        _figureColorBarBelow = Plt.Create().WithTheme(Theme.Default)
            .AddSubPlot(1, 2, 1, ax => ax.Heatmap(cells).SetXLabel("x")).AddSubPlot(1, 2, 2, ax => ax.Heatmap(cells).SetXLabel("x"))
            .WithColorBar(b => b with { Orientation = ColorBarOrientation.Horizontal, Label = "value" }).Build();

        // A word cloud of 100 words: every draw packs the boxes of the words into the canvas on a spiral, the heaviest first.
        string[] words = [.. Enumerable.Range(0, 100).Select(i => "term" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        double[] weights = [.. Enumerable.Range(0, 100).Select(i => 1.0 + (i * 31 % 17))];
        _wordCloud100 = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.WordCloud(words, weights)).Build();

        // A relative rotation graph of ten assets over 200 bars: the range of the axes and the drawing each read every asset's
        // RS-Ratio and RS-Momentum.
        var rotation = new Random(42);
        double[] Walk(double step)
        {
            var closes = new double[200];
            closes[0] = 100;
            for (int i = 1; i < closes.Length; i++) closes[i] = Math.Max(1, closes[i - 1] * (1 + (rotation.NextDouble() - 0.49) * step));
            return closes;
        }

        double[] benchmark = Walk(0.03);
        double[][] assets = [.. Enumerable.Range(0, 10).Select(_ => Walk(0.04))];
        string[] tickers = [.. Enumerable.Range(0, 10).Select(i => "A" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        _relativeRotation = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(assets, benchmark, tickers)).Build();
    }

    [Benchmark(Baseline = true)]
    public string SimpleLine() => _simpleLine.ToSvg();

    [Benchmark]
    public string ComplexChart() => _complexChart.ToSvg();

    [Benchmark]
    public string SubplotGrid3x3() => _subplotGrid.ToSvg();

    [Benchmark]
    public string Treemap() => _treemap.ToSvg();

    [Benchmark]
    public string Sunburst() => _sunburst.ToSvg();

    [Benchmark]
    public string Sankey() => _sankey.ToSvg();

    [Benchmark]
    public string PolarLine() => _polar.ToSvg();

    [Benchmark]
    public string Surface3D() => _surface3D.ToSvg();

    [Benchmark]
    public string Surface3D_WithLighting() => _surface3DLit.ToSvg();

    [Benchmark]
    public string WithLegend() => _legendChart.ToSvg();

    [Benchmark]
    public string LargeLineChart_10K() => _largeLine10K.ToSvg();

    [Benchmark]
    public string LargeLineChart_100K_LTTB() => _largeLine100K.ToSvg();

    // A figure that is drawn again unchanged is handed its kept drawing, which costs a fraction of a draw. The rows from here on
    // change the first subplot (its title flips between two words of the same length) before every draw, so each one is a draw of
    // that subplot, which is what a live chart pays when its data moved. The rows above keep their older meaning: an unchanged figure.

    private static Figure DotsFigure(double[][] groups, double[] x, double[] y, Theme? theme)
    {
        var figure = Plt.Create();
        if (theme is not null) figure = figure.WithTheme(theme);
        return figure.AddSubPlot(1, 1, 1, ax => ax.Stripplot(groups).Swarmplot(groups).Pointplot(groups).Residplot(x, y)).Build();
    }

    private int _tick;

    private string Draw(Figure figure)
    {
        figure.SubPlots[0].Title = (_tick++ & 1) == 0 ? "tick" : "tock";
        return figure.ToSvg();
    }

    /// <summary>A polar line, changed and drawn: the radial scale, its rings and labels, the spokes and the line.</summary>
    [Benchmark]
    public string PolarLine_Changed() => Draw(_polar);

    /// <summary>A polar axes of 24 bars, a line and a rectangle, changed and drawn.</summary>
    [Benchmark]
    public string PolarMixed_Changed() => Draw(_polarMixed);

    /// <summary>A ternary axes of 300 points, a line and a rectangle, changed and drawn.</summary>
    [Benchmark]
    public string Ternary_Changed() => Draw(_ternary);

    /// <summary>Two log axes of one power of ten each under the default matplotlib style, with a major and minor grid, changed and
    /// drawn: the named multiples, their marks and labels, and the grid lines on the powers and the multiples.</summary>
    [Benchmark]
    public string LogOnePower_Changed() => Draw(_logOnePower);

    /// <summary>A pie, changed and drawn: the wedges, their labels and the percentages.</summary>
    [Benchmark]
    public string Pie() => Draw(_pie);

    /// <summary>One subplot whose grid stands on the major and the minor ticks, changed and drawn.</summary>
    [Benchmark]
    public string MinorGrid() => Draw(_minorGrid);

    /// <summary>Four violins, changed and drawn: the subplot is drawn again, and each group's density, which nothing changed,
    /// is the estimate the series kept.</summary>
    [Benchmark]
    public string ViolinDraw() => Draw(_violin);

    /// <summary>A strip, a swarm, a point plot and a residual plot on one axes under the default matplotlib style, changed and drawn: the
    /// size, opacity and jitter of every dot are asked of the theme once a draw.</summary>
    [Benchmark]
    public string DotsDraw() => Draw(_dots);

    /// <summary>The same four kinds of dots under the library's own theme, changed and drawn.</summary>
    [Benchmark]
    public string DotsDrawLibrary() => Draw(_dotsLibrary);

    /// <summary>The values of a 256 x 256 image: whole numbers in a repeating pattern, the same on every platform.</summary>
    internal static double[,] Image256()
    {
        var data = new double[256, 256];
        for (int r = 0; r < 256; r++) for (int c = 0; c < 256; c++) data[r, c] = (r * 7 + c * 13) % 64;
        return data;
    }

    /// <summary>A 256 x 256 image under the default matplotlib style, changed and drawn: its cells as the SVG writes them.</summary>
    [Benchmark]
    public string Image256Changed() => Draw(_image256);

    /// <summary>A Gantt chart of eight tasks, changed and drawn: its bars, and its rows named by the tasks.</summary>
    [Benchmark]
    public string GanttChanged() => Draw(_gantt);

    /// <summary>A 12 x 12 matrix shown as matplotlib's matshow shows it, changed and drawn: its cells, its numbered rows and columns and its
    /// colour bar.</summary>
    [Benchmark]
    public string MatShowChanged() => Draw(_matShow);

    /// <summary>A 2-D histogram of 5 000 points in 30 x 30 bins under the default matplotlib style, changed and drawn: its cells.</summary>
    [Benchmark]
    public string Histogram2DChanged() => Draw(_histogram2D);

    /// <summary>A 30 x 30 mesh under the default matplotlib style, changed and drawn: its cells.</summary>
    [Benchmark]
    public string PcolormeshChanged() => Draw(_pcolormesh);

    /// <summary>The spectrogram of 2 000 samples (256-sample windows, 14 of them) under the default matplotlib style, changed and drawn:
    /// its short-time spectrum and its cells.</summary>
    [Benchmark]
    public string SpectrogramChanged() => Draw(_spectrogram);

    /// <summary>One density curve, changed and drawn: the subplot is drawn again from the curve the series kept.</summary>
    [Benchmark]
    public string KdeDraw() => Draw(_kde);

    /// <summary>A pair grid of three variables split into two hue groups, its diagonal a density curve per group, changed and
    /// drawn: the six densities, which nothing changed, are the estimate the series kept.</summary>
    [Benchmark]
    public string PairGridKdeChanged() => Draw(_pairGridKde);

    /// <summary>One log-scale figure kept on screen and changed five times in a row, drawn each time: the cost per draw. A draw
    /// that writes into the model is not the same draw twice, so a figure that stays up is measured for what it costs after the
    /// first.</summary>
    [Benchmark(OperationsPerInvoke = 5)]
    public string PersistentLogRedraw() => Redraw(_persistentLog);

    /// <summary>The same for a symmetric-log figure.</summary>
    [Benchmark(OperationsPerInvoke = 5)]
    public string PersistentSymlogRedraw() => Redraw(_persistentSymlog);

    /// <summary>The simple line, changed and drawn: the draw the unchanged row above does not make.</summary>
    [Benchmark]
    public string SimpleLineChanged() => Draw(_simpleLine);

    /// <summary>The 3 x 3 grid with the first subplot changed: one subplot drawn, eight handed back as they were.</summary>
    [Benchmark]
    public string SubplotGrid3x3Changed() => Draw(_subplotGrid);

    /// <summary>The 10 000-point line, changed and drawn.</summary>
    [Benchmark]
    public string LargeLineChart_10K_Changed() => Draw(_largeLine10K);

    /// <summary>The 100,000-point line with 200 gaps, downsampled to 2,000 points, changed and drawn: every run is reshaped on its own
    /// and the runs are joined again.</summary>
    [Benchmark]
    public string GappedLargeLine_100K_LTTB_Changed() => Draw(_gappedLine100K);

    /// <summary>A 3-D scatter of 2,000 points, changed and drawn: the placed, sorted and drawn marker of each point.</summary>
    [Benchmark]
    public string Scatter3D_Changed() => Draw(_scatter3D);

    /// <summary>A line on axes with breaks (two on X, one on Y), changed and drawn: the break maps every transform builds and
    /// every point placed past them.</summary>
    [Benchmark]
    public string BrokenAxesChanged() => Draw(_brokenAxes);

    /// <summary>The tight-layout 3 x 3 grid with the first subplot changed: the layout measures every subplot, one is drawn.</summary>
    [Benchmark]
    public string TightLayoutGrid_Changed() => Draw(_tightGrid);

    /// <summary>Four bar series side by side over twelve categories, the first subplot changed and drawn.</summary>
    [Benchmark]
    public string GroupedBars_Changed() => Draw(_groupedBars);

    /// <summary>Ten stacked bar series of 1 000 columns, changed and drawn: the stack the bars stand on and the range it reaches.</summary>
    [Benchmark]
    public string StackedBars10x1000_Changed() => Draw(_stackedBars);

    /// <summary>A tight-layout axes with two Y axes on the right and one more on the left, changed and drawn: the layout
    /// reserves each slot's band, the draw places and stacks each slot.</summary>
    [Benchmark]
    public string TwoSlotY_Changed() => Draw(_twoSlotY);

    /// <summary>The 3-D surface, changed and drawn.</summary>
    [Benchmark]
    public string Surface3D_Changed() => Draw(_surface3D);

    /// <summary>The treemap of 600 leaves, changed and drawn: the layout of the children of every inner node.</summary>
    [Benchmark]
    public string Treemap600_Changed() => Draw(_treemap600);

    /// <summary>The word cloud of 100 words, changed and drawn: the packing of the boxes of the words into the canvas. A packing
    /// that grew with the cube of the words would show here as minutes where this takes a fraction of a second.</summary>
    [Benchmark]
    public string WordCloud100_Changed() => Draw(_wordCloud100);

    /// <summary>The relative rotation graph of ten assets over 200 bars, changed and drawn: the range and the drawing each read
    /// the RS-Ratio and RS-Momentum of every asset.</summary>
    [Benchmark]
    public string RelativeRotation10x200_Changed() => Draw(_relativeRotation);

    /// <summary>The small treemap, changed and drawn: each tile and the name on it.</summary>
    [Benchmark]
    public string TreemapSmall_Changed() => Draw(_treemapSmall);

    /// <summary>The icicle, changed and drawn: each cell and the name on it.</summary>
    [Benchmark]
    public string Icicle_Changed() => Draw(_icicle);

    /// <summary>The sunburst, changed and drawn: each ring segment and the name placed on it.</summary>
    [Benchmark]
    public string Sunburst_Changed() => Draw(_sunburstLabelled);

    /// <summary>The Sankey with its names inside the nodes, changed and drawn.</summary>
    [Benchmark]
    public string SankeyInside_Changed() => Draw(_sankeyInside);

    /// <summary>The pie with the percentage on each slice, changed and drawn.</summary>
    [Benchmark]
    public string PieAutoPct_Changed() => Draw(_pieAutoPct);

    /// <summary>A line chart with a quiver key, changed and drawn: the key's arrow and its label.</summary>
    [Benchmark]
    public string QuiverKey_Changed() => Draw(_quiverKey);

    /// <summary>A heatmap with a colour bar below it under the library's own theme, changed and drawn.</summary>
    [Benchmark]
    public string ColorBarBelow_Changed() => Draw(_colorBarBelow);

    /// <summary>Two heatmaps with the figure's colour bar below them under the library's own theme, changed and drawn.</summary>
    [Benchmark]
    public string FigureColorBarBelow_Changed() => Draw(_figureColorBarBelow);

    /// <summary>A surface in a figure a browser turns, changed and drawn: the faces and the cube with their vertex data.</summary>
    [Benchmark]
    public string Surface3DRotating_Changed() => Draw(_surface3DRotating);

    /// <summary>2,000 points of a 3-D scatter in a figure a browser turns, changed and drawn: each marker with its vertex data.</summary>
    [Benchmark]
    public string Scatter3DRotating_Changed() => Draw(_scatter3DRotating);

    private string Redraw(Figure figure)
    {
        var svg = string.Empty;
        for (var i = 0; i < 5; i++) svg = Draw(figure);
        return svg;
    }
}
