// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>One composed chart as the test side draws it: the name a Theory carries, the figure builder, and what a
/// reader of the picture is owed.</summary>
/// <param name="Name">The shape's name; xunit carries this string, never the builder.</param>
/// <param name="Build">Builds the figure; the flag puts one missing value (NaN) into the data.</param>
/// <param name="Colours">How many distinct colours the series of the chart wear, one per drawn unit; <see langword="null"/>
/// when the chart is a grid of cells and has no series colour to count.</param>
/// <param name="LegendEntries">How many entries the legend lists; 0 when the chart names its units on the axis or at the
/// end of the line instead.</param>
/// <param name="Verb">The verb this shape draws, when it is the shape that stands for that verb; <see langword="null"/> for a
/// variant of a verb that another shape stands for. Every verb has one shape that names it.</param>
public readonly record struct ComposedShape(string Name, Func<bool, FigureBuilder> Build, int? Colours, int LegendEntries, string? Verb = null);

/// <summary>The composed charts of the recipes-to-verbs increment, built from the public verbs only: the call chains the
/// cookbook recipes make today and the one-call verbs will make tomorrow. Every shape is a figure with a known
/// right answer: it must survive a JSON round trip drawing the same picture, one drawn unit must wear one colour,
/// and the legend must list exactly its units.
/// <para>The data is platform deterministic on purpose (integer arithmetic, no Sin/Cos/Exp/Log/Pow), so a shape can
/// join a byte comparison later; the tests here compare in-process and never against stored bytes.</para>
/// <para>Public API only, so the Mcp test project links this very file and runs the same shapes through its reader.</para></summary>
public static class ComposedChartShapes
{
    private static readonly Color Neutral = Color.FromHex("#444444");

    /// <summary>Every shape, tranche A first.</summary>
    public static IReadOnlyList<ComposedShape> All { get; } =
    [
        new("lollipop", Lollipop, 1, 1, "Lollipop"),
        new("dumbbell", Dumbbell, 3, 2, "Dumbbell"),
        new("pareto", Pareto, 2, 2, "Pareto"),
        new("ridgeline", Ridgeline, 3, 0, "Ridgeline"),
        new("bump", Bump, 3, 0, "BumpChart"),
        new("calendar", Calendar, null, 0, "CalendarHeatmap"),
        new("spy", Spy, 1, 0, "Spy"),
        new("matshow", MatShow, null, 0, "MatShow"),
        new("calendar-events", CalendarEvents, null, 2, "CalendarEvents"),
        new("slope", Slope, 5, 0, "SlopeGraph"),
        new("dot-plot", DotPlot, 2, 2, "DotPlot"),
        new("qq", QQ, 1, 1, "QQPlot"),
        new("probability-plot", ProbabilityPlot, 1, 1, "ProbabilityPlot"),
        new("control-chart", ControlChart, 2, 2, "ControlChart"),
        new("shewhart-chart", ShewhartChart, 2, 2, "ShewhartChart"),
        new("moving-range-chart", MovingRangeChart, 2, 2),
        new("xbar-chart", XBarChart, 2, 2),
        new("range-chart", RangeChart, 2, 2),
        new("p-chart", PChart, 2, 2),
        new("c-chart", CChart, 2, 2),
        new("kaplan-meier", KaplanMeier, 2, 2, "KaplanMeier"),
        new("risk-table", RiskTable, 2, 2, "RiskTable"),
        new("parallel-coordinates", ParallelCoordinates, 3, 3, "ParallelCoordinates"),
        new("stream-graph", StreamGraph, 4, 4, "StreamGraph"),
        new("marimekko", Marimekko, 3, 3, "Marimekko"),
        new("raincloud", Raincloud, 1, 1, "Raincloud"),
        new("waterfall-total", WaterfallTotal, null, 0),
        new("horizon", Horizon, 2, 0, "Horizon"),
        new("renko", Renko, 2, 2, "Renko"),
        new("connected-scatter", ConnectedScatter, 1, 0, "ConnectedScatter"),
        new("kagi", Kagi, 1, 0, "Kagi"),
        new("stem-and-leaf", StemAndLeaf, null, 0, "StemAndLeaf"),
    ];

    /// <summary>The names of <see cref="All"/>, as a Theory carries them.</summary>
    public static TheoryData<string> Names { get; } = new(All.Select(s => s.Name));

    /// <summary>Every shape twice, without and with a missing value (NaN): the name and the flag the builder takes, as a
    /// Theory carries them. For the suites that send a shape somewhere and compare what comes back.</summary>
    public static TheoryData<string, bool> NamesAndVariants { get; } = VariantsOf(All);

    private static TheoryData<string, bool> VariantsOf(IEnumerable<ComposedShape> shapes)
    {
        var data = new TheoryData<string, bool>();
        foreach (var shape in shapes)
        {
            data.Add(shape.Name, false);
            data.Add(shape.Name, true);
        }
        return data;
    }

    /// <summary>The names of the shapes that have series colours to count.</summary>
    public static TheoryData<string> ColouredNames { get; } = new(All.Where(s => s.Colours is not null).Select(s => s.Name));

    /// <summary>The shape called <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">No shape has that name.</exception>
    public static ComposedShape Named(string name) =>
        All.FirstOrDefault(s => s.Name == name) is { Name: not null } shape
            ? shape
            : throw new ArgumentException($"No composed shape is called '{name}'.", nameof(name));

    private static double[] Ramp(int count) => Enumerable.Range(0, count).Select(i => (double)i).ToArray();

    // ---- tranche A ----------------------------------------------------------------------------------------------

    private static readonly string[] Products = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta"];

    private static double[] Scores(bool missing)
    {
        double[] scores = [82.5, 67.3, 91.1, 54.8, 76.4, 88.2];
        if (missing) scores[2] = double.NaN;
        return scores;
    }

    private static readonly Color Blue = Color.FromHex("#5B9BD5");

    private static void StyleLollipop(StemSeries s)
    {
        s.StemColor = Blue; s.MarkerColor = Color.FromHex("#1F4E79"); s.Marker = MarkerStyle.Diamond; s.Label = "Score";
    }

    /// <summary>One StemSeries, built by <c>Lollipop</c>: the stem draws its own head, so there is no second series on top
    /// of it, and the products are the names on the X axis.</summary>
    private static FigureBuilder Lollipop(bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Lollipop(Products, Scores(missing), StyleLollipop)
            .WithLegend());

    /// <summary>The call chain the <c>Lollipop</c> verb replaces, written by hand with the public building blocks: one stem
    /// series on the centre of each category's slot and the categories' names as the ticks of the X axis. The verb must
    /// draw exactly this picture.</summary>
    /// <param name="missing">Whether one score is missing (NaN).</param>
    public static FigureBuilder LollipopByHand(bool missing)
    {
        double[] centres = Enumerable.Range(0, Products.Length).Select(k => k + 0.5).ToArray();
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Stem(centres, Scores(missing), StyleLollipop)
            .SetXTickLocator(new FixedLocator(centres))
            .SetXTickFormatter(new CategoryFormatter(Products))
            .WithLegend());
    }

    private static readonly string[] DumbbellProducts = ["Product A", "Product B", "Product C", "Product D", "Product E", "Product F"];

    private static readonly double[] Before = [62.0, 78.5, 55.0, 83.0, 70.0, 48.0];

    private static double[] After(bool missing)
    {
        double[] after = [74.0, 82.0, 68.0, 79.5, 88.5, 61.0];
        if (missing) after[1] = double.NaN;
        return after;
    }

    private static void StyleDot(ScatterSeries s) { s.Marker = MarkerStyle.Circle; s.MarkerSize = 14; }

    /// <summary>Two dots and a connector, built by <c>Dumbbell</c>: the connector spans [min, max] whichever dot is higher
    /// (the fourth product goes down), in a neutral colour and added last so the dots take the first two colour slots.</summary>
    private static FigureBuilder Dumbbell(bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Dumbbell(DumbbellProducts, new BarGroup("Before", Before), new BarGroup("After", After(missing)))
            .WithLegend());

    /// <summary>The call chain the <c>Dumbbell</c> verb replaces, written by hand with the public building blocks: two
    /// scatter series on the centres of the categories' slots, the connector as an error bar of no error that spans the lower
    /// dot to the higher one, added last in a neutral colour behind the dots, and the categories' names as the ticks of the X
    /// axis. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one value of the second group is missing (NaN).</param>
    public static FigureBuilder DumbbellByHand(bool missing)
    {
        double[] after = After(missing);
        double[] x = Enumerable.Range(0, Before.Length).Select(k => k + 0.5).ToArray();
        double[] low = Before.Zip(after, Math.Min).ToArray();
        double[] span = Before.Zip(after, (b, a) => Math.Abs(a - b)).ToArray();
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Scatter(x, Before, s => { StyleDot(s); s.Label = "Before"; })
            .Scatter(x, after, s => { StyleDot(s); s.Label = "After"; })
            .ErrorBar(x, low, new double[x.Length], span, s =>
            {
                s.Color = Colors.Gray; s.LineWidth = 3; s.CapSize = 0; s.ZOrder = -1;
            })
            .SetXTickLocator(new FixedLocator(x))
            .SetXTickFormatter(new CategoryFormatter(DumbbellProducts))
            .WithLegend());
    }

    // In the order a tally sheet gives them, not sorted: the verb sorts.
    private static readonly string[] Defects = ["Cracks", "Warp", "Scratches", "Breaks", "Dents", "Missing", "Stains"];

    private static double[] DefectCounts(bool missing)
    {
        double[] counts = [67, 9, 142, 31, 98, 18, 45];
        if (missing) counts[3] = double.NaN;   // "Breaks"
        return counts;
    }

    /// <summary>Counts sorted largest first as bars on the left axis, the running share as a marked line on the right axis
    /// with its points on the bar centres, built by <c>Pareto</c>: the two axes are one scale (0 to 1.05 times the total, 0 to
    /// 105 %), each coloured and titled like its series.</summary>
    private static FigureBuilder Pareto(bool missing) =>
        Plt.Create().WithSize(900, 500).AddSubPlot(1, 1, 1, ax => ax.Pareto(Defects, DefectCounts(missing)));

    /// <summary>The call chain the <c>Pareto</c> verb replaces, written by hand with the public building blocks on data sorted
    /// by hand: the bars largest first (a missing count last), the running share over the total of the counts that are there
    /// on the bar centres, the left axis from 0 to 1.05 times the total and the right one from 0 to 105 % so the two are one
    /// scale, each axis coloured and titled like its series, the legend right of centre. The verb must draw exactly this
    /// picture.</summary>
    /// <param name="missing">Whether one count ("Breaks") is missing (NaN).</param>
    public static FigureBuilder ParetoByHand(bool missing)
    {
        string[] names = missing
            ? ["Scratches", "Dents", "Cracks", "Stains", "Missing", "Warp", "Breaks"]
            : ["Scratches", "Dents", "Cracks", "Stains", "Breaks", "Missing", "Warp"];
        double[] counts = missing ? [142, 98, 67, 45, 18, 9, double.NaN] : [142, 98, 67, 45, 31, 18, 9];
        double total = 0;
        double[] running = counts.Select(c => double.IsNaN(c) ? double.NaN : total += c).ToArray();
        double[] cumulative = running.Select(r => r / total * 100).ToArray();
        double[] centres = Enumerable.Range(0, names.Length).Select(k => k + 0.5).ToArray();
        return Plt.Create().WithSize(900, 500).AddSubPlot(1, 1, 1, ax => ax
            .Bar(names, counts, s => { s.Color = Colors.Tab10Blue; s.Label = "Count"; })
            .SetYLim(0, total * 1.05)
            .SetYLabel("Count")
            .SetYAxisColor(Colors.Tab10Blue)
            .WithAddRightYAxis(y2 => y2
                .Plot(centres, cumulative, s =>
                {
                    s.Color = Colors.Tab10Orange; s.LineWidth = 2; s.Marker = MarkerStyle.Circle; s.MarkerSize = 7; s.Label = "Cumulative %";
                })
                .SetYLim(0, 105)
                .SetYLabel("Cumulative %")
                .SetYAxisColor(Colors.Tab10Orange))
            .WithLegend(LegendPosition.Right));
    }

    private static readonly string[] RidgeNames = ["Group A", "Group B", "Group C"];

    /// <summary>Three samples of 40 observations, made by integer arithmetic (a permutation of 0..39, scaled): a wide one, a
    /// bimodal one and a narrow one, around three different centres.</summary>
    private static NamedSample[] RidgeGroups(bool missing)
    {
        double Spread(int i) => (i * 7919 % 40) - 19.5;
        double[][] samples =
        [
            [.. Enumerable.Range(0, 40).Select(i => -1.2 + Spread(i) / 20.0)],
            [.. Enumerable.Range(0, 40).Select(i => 0.2 + (i % 2 == 0 ? -0.7 : 0.7) + Spread(i) / 60.0)],
            [.. Enumerable.Range(0, 40).Select(i => 1.3 + Spread(i) / 30.0)],
        ];
        if (missing) samples[1][5] = double.NaN;
        return [.. samples.Select((values, g) => new NamedSample(RidgeNames[g], values))];
    }

    /// <summary>One AreaSeries per ridge, built by <c>Ridgeline</c>: every ridge estimated on one shared grid with one joint
    /// bandwidth, drawn to one height scale (the tallest reaches the next baseline), the first group at the top and the group
    /// names on the Y axis.</summary>
    private static FigureBuilder Ridgeline(bool missing) =>
        Plt.Create().WithSize(900, 600).AddSubPlot(1, 1, 1, ax => ax.Ridgeline(RidgeGroups(missing)));

    /// <summary>The call chain the <c>Ridgeline</c> verb replaces, written by hand with the public building blocks: the
    /// observations that can be drawn of each group, the mean of the groups' own Silverman bandwidths (ggridges' joint
    /// bandwidth), one grid of 200 points over the
    /// pooled range padded by three bandwidths, each group's Gaussian kernel density on that grid divided by the tallest
    /// density of all, one fill per group from its baseline (the centre of a slot, the first group on the highest) up to its
    /// density, and the group names on those baselines, read from the top down. The verb must draw exactly this picture.
    /// <para>The density uses <see cref="Math.Exp"/>, so this shape is compared in-process only, never against stored
    /// bytes.</para></summary>
    /// <param name="missing">Whether one observation of the second group is missing (NaN).</param>
    public static FigureBuilder RidgelineByHand(bool missing)
    {
        const int points = 200;
        const double sqrt2Pi = 2.5066282746310002;
        double[][] samples = [.. RidgeGroups(missing).Select(g => g.Values.Where(double.IsFinite).OrderBy(v => v).ToArray())];
        double[] pooled = [.. samples.SelectMany(s => s).OrderBy(v => v)];

        // ggridges' joint bandwidth: the mean of each group's own Silverman bandwidth (every group here has a spread).
        double h = 0;
        foreach (double[] s in samples)
        {
            int n = s.Length;
            double sum = 0;
            foreach (double v in s) sum += v;
            double mean = sum / n, squares = 0;
            foreach (double v in s) { double d = v - mean; squares += d * d; }
            h += 1.06 * Math.Sqrt(squares / (n - 1)) * Math.Pow(n, -0.2);
        }
        h /= samples.Length;

        double lo = pooled[0] - 3 * h, hi = pooled[^1] + 3 * h, step = (hi - lo) / (points - 1);
        double[] grid = [.. Enumerable.Range(0, points).Select(j => lo + step * j)];
        double[][] density = [.. samples.Select(s => grid.Select(x =>
        {
            double kernels = 0;
            foreach (double v in s) { double u = (x - v) / h; kernels += Math.Exp(-0.5 * u * u); }
            return kernels / ((double)s.Length * h * sqrt2Pi);
        }).ToArray())];
        double tallest = density.Max(d => d.Max());

        return Plt.Create().WithSize(900, 600).AddSubPlot(1, 1, 1, ax =>
        {
            for (int g = 0; g < samples.Length; g++)
            {
                double baseline = samples.Length - 1 - g + 0.5;
                ax.FillBetween(grid, [.. density[g].Select(d => baseline + d / tallest)], [.. grid.Select(_ => baseline)]);
            }
            ax.SetYTickLocator(new FixedLocator([.. Enumerable.Range(0, samples.Length).Select(k => k + 0.5)]))
              .SetYTickFormatter(new CategoryFormatter(RidgeNames, reversed: true));
        });
    }

    private static readonly string[] BumpBrands = ["Alpha", "Beta", "Gamma"];

    private static readonly double[] BumpPeriods = [1, 2, 3, 4];

    private static double[][] BumpRanks(bool missing)
    {
        double[][] ranks = [[1, 2, 3, 3], [2, 1, 1, 2], [3, 3, 2, 1]];
        if (missing) ranks[1][2] = double.NaN;
        return ranks;
    }

    /// <summary>A line per brand, a marker at each rank, the name at the end of the line, the best rank on top and
    /// integer ticks, built by <c>BumpChart</c>: one line series for each brand, the Y limits from the lowest rank plus half
    /// to one half and a tick for every whole rank.</summary>
    private static FigureBuilder Bump(bool missing)
    {
        double[][] ranks = BumpRanks(missing);
        return Plt.Create().WithSize(900, 450).AddSubPlot(1, 1, 1, ax => ax
            .BumpChart(BumpPeriods, [.. BumpBrands.Select((brand, b) => new BarGroup(brand, ranks[b]))]));
    }

    /// <summary>The call chain the <c>BumpChart</c> verb replaces, written by hand with the public building blocks: a thick
    /// marked line for each brand with its name marked for the end of the line, the Y limits from the number of brands plus
    /// half to one half (rank 1 on top, a limit clips at the frame, so the half rank is the margin a marker needs) and a tick
    /// for every whole rank. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one rank ("Beta" in the third period) is missing (NaN).</param>
    public static FigureBuilder BumpByHand(bool missing)
    {
        double[][] ranks = BumpRanks(missing);
        return Plt.Create().WithSize(900, 450).AddSubPlot(1, 1, 1, ax =>
        {
            for (int b = 0; b < BumpBrands.Length; b++)
            {
                int brand = b;
                ax.Plot(BumpPeriods, ranks[brand], s => { s.LineWidth = 3; s.Marker = MarkerStyle.Circle; s.MarkerSize = 10; s.Label = BumpBrands[brand]; });
                ax.WithDirectLabel();
            }
            ax.SetYLim(BumpBrands.Length + 0.5, 0.5).SetYTickLocator(new MultipleLocator(1));
        });
    }

    private static readonly DateTime CalendarStart = new(2025, 1, 1);

    private const int CalendarDays = 365;

    /// <summary>A value for every day of 2025, made by integer arithmetic; the missing one is March 13 (day 71, a
    /// Thursday).</summary>
    private static double[] CalendarValues(bool missing)
    {
        double[] values = [.. Enumerable.Range(0, CalendarDays).Select(d => (double)(d * 37 % 11))];
        if (missing) values[71] = double.NaN;
        return values;
    }

    /// <summary>The calendar's colour map: white to dark green, as the familiar contribution chart is, with a grey bad
    /// colour. Under a map that starts at white a blank day and a zero would look alike; the grey tells them apart. Built
    /// in code, so the JSON carries it as what it is made of, not by a name.</summary>
    private static readonly IColorMap CalendarMap =
        new LinearColorMap("calendar greens", [Color.FromHex("#F7FCF5"), Color.FromHex("#74C476"), Color.FromHex("#00441B")])
        {
            BadColor = Color.FromHex("#DDDDDD"),
        };

    /// <summary>A year as seven weekday rows by week columns, built by <c>CalendarHeatmap</c>: one heatmap with Monday in
    /// the top row, the weekdays named on the row centres, each month named on the column that holds its 1st, a colour bar,
    /// and the days outside the year (and a missing day) blank, in the grey of the map's bad colour, because a day that is
    /// not there is not a zero.</summary>
    private static FigureBuilder Calendar(bool missing) =>
        Plt.Create().WithSize(1100, 300).AddSubPlot(1, 1, 1, ax => ax
            .CalendarHeatmap([.. Enumerable.Range(0, CalendarDays).Select(d => CalendarStart.AddDays(d))], CalendarValues(missing),
                configure: s => s.ColorMap = CalendarMap));

    /// <summary>The call chain the <c>CalendarHeatmap</c> verb replaces, written by hand with the public building blocks and
    /// corrected where the old recipe was wrong (it named the weekdays from the bottom up on the row edges, so "Mon" stood
    /// on the Sunday row, and the months on the column edges): the year's days in a 7 x 53 grid with Monday in row 0, the
    /// cells outside the year NaN, the weekday names read from the top down on the row centres (a heatmap draws its row 0 at
    /// the top while the Y axis counts upward), each month's name on the centre of the column that holds its 1st, and a
    /// colour bar. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one day (March 13) is missing (NaN).</param>
    public static FigureBuilder CalendarByHand(bool missing)
    {
        int lead = ((int)CalendarStart.DayOfWeek + 6) % 7;   // the days of the first week before January 1, from Monday
        int weeks = (CalendarDays + lead + 6) / 7;
        var cells = new double[7, weeks];
        for (int r = 0; r < 7; r++) for (int c = 0; c < weeks; c++) cells[r, c] = double.NaN;
        double[] values = CalendarValues(missing);
        for (int d = 0; d < CalendarDays; d++) cells[(d + lead) % 7, (d + lead) / 7] = values[d];
        string[] monthNames = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[..12];
        int[] monthColumns = [.. Enumerable.Range(1, 12).Select(m => ((new DateTime(2025, m, 1) - CalendarStart).Days + lead) / 7)];
        var labels = new string[weeks];
        for (int m = 0; m < 12; m++) labels[monthColumns[m]] = monthNames[m];
        return Plt.Create().WithSize(1100, 300).AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(cells, s => s.ColorMap = CalendarMap)
            .SetXTickLocator(new FixedLocator([.. monthColumns.Select(c => c + 0.5)]))
            .SetXTickFormatter(new CategoryFormatter(labels))
            .SetYTickLocator(new FixedLocator([.. Enumerable.Range(0, 7).Select(r => r + 0.5)]))
            .SetYTickFormatter(new CategoryFormatter(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], reversed: true))
            .SetAspect(1)
            .WithColorBar());
    }

    private const int SpyRows = 12, SpyColumns = 18;

    /// <summary>A matrix with a pattern a reader can see: the diagonal, the anti-diagonal and a scatter of cells from a
    /// modular rule, in values of both signs (positive on even rows, negative on odd ones) from 1 to 4, made by integer
    /// arithmetic; every other cell is 0. The missing one is on the diagonal, at row 4, column 4.</summary>
    private static double[,] SpyMatrix(bool missing)
    {
        var matrix = new double[SpyRows, SpyColumns];
        for (int r = 0; r < SpyRows; r++)
        {
            for (int c = 0; c < SpyColumns; c++)
            {
                bool marked = r == c || r + c == SpyColumns - 1 || (r * 5 + c * 3) % 17 == 0;
                if (marked) matrix[r, c] = (r % 2 == 0 ? 1 : -1) * (1 + (r + c) % 4);
            }
        }
        if (missing) matrix[4, 4] = double.NaN;
        return matrix;
    }

    /// <summary>The non-zero cells of a matrix as square markers, built by <c>Spy</c>: one scatter series, x the column and
    /// y the row, the axes running half a cell past the matrix on every side, the Y axis backwards so row 0 is on top, and
    /// the ticks at whole cell numbers.</summary>
    private static FigureBuilder Spy(bool missing) =>
        Plt.Create().WithSize(800, 520).AddSubPlot(1, 1, 1, ax => ax.Spy(SpyMatrix(missing)));

    /// <summary>The call chain the <c>Spy</c> verb replaces, written by hand with the public building blocks: the cells with
    /// a magnitude above zero (a missing value is not above anything) as one scatter of square markers at (column, row), the X
    /// limits from half a cell before the first column to half a cell after the last, the Y limits the same way
    /// but running downward so row 0 is on top, and whole-number ticks every second cell. The verb must draw exactly this
    /// picture.</summary>
    /// <param name="missing">Whether one cell (row 4, column 4) is missing (NaN).</param>
    public static FigureBuilder SpyByHand(bool missing)
    {
        double[,] matrix = SpyMatrix(missing);
        var columns = new List<double>();
        var rows = new List<double>();
        for (int r = 0; r < SpyRows; r++)
        {
            for (int c = 0; c < SpyColumns; c++)
            {
                if (Math.Abs(matrix[r, c]) > 0) { columns.Add(c); rows.Add(r); }
            }
        }
        return Plt.Create().WithSize(800, 520).AddSubPlot(1, 1, 1, ax => ax
            .Scatter([.. columns], [.. rows], s => s.Marker = MarkerStyle.Square)
            .SetXLim(-0.5, SpyColumns - 0.5)
            .SetYLim(SpyRows - 0.5, -0.5)
            .SetAspect(1)
            .SetXTickLocator(new MultipleLocator(2))    // 18 columns: whole numbers, at most nine intervals across
            .SetYTickLocator(new MultipleLocator(2)));  // 12 rows: the same
    }

    private static readonly DateTime[] EventDays = [new(2025, 3, 3), new(2025, 3, 3), new(2025, 4, 1)];
    private static readonly string[] EventKinds = ["deploy", "incident", "deploy"];

    /// <summary>Three events on two days: two on Monday March 3 (a deploy and an incident) and one on Tuesday April 1, a month
    /// later, built by <c>CalendarEvents</c>: a neutral cell for every day from March 3 to April 1, and a mark for each event
    /// inside the cell of its day, the two of the first day side by side.</summary>
    private static FigureBuilder CalendarEvents(bool missing) =>
        Plt.Create().WithSize(900, 300).AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(EventDays, EventKinds));

    private static double[] StemSample(bool missing)
    {
        double[] ages = [12, 15, 21, 23, 23, 38, 41, 44, 47, 52];
        return missing ? [.. ages, double.NaN] : ages;
    }

    /// <summary>Ten ages as a stem-and-leaf display, built by <c>StemAndLeaf</c>: the tens as the stems and the units as the leaves,
    /// one column for each leaf of the fullest row, and the line that says how to read it.</summary>
    private static FigureBuilder StemAndLeaf(bool missing) =>
        Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(StemSample(missing)));

    /// <summary>The call chain the <c>StemAndLeaf</c> verb replaces, written by hand with the public building blocks: the rows
    /// worked out by hand (1 | 2 5, 2 | 1 3 3, 3 | 8, 4 | 1 4 7, 5 | 2: a unit of 1, because 41 stems at a tenth outnumber the
    /// budget of 8 rows), a table of five columns of text (the stem aligned to the right, the bar and the three leaves centred, in
    /// the widths 2, 0.5, 1, 1 and 1) with no border and no fill, 20 pixels a row, and the key as an annotation pinned to the
    /// plot's lower left. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether the sample carries a missing value (NaN), which leaves the picture as it is.</param>
    public static FigureBuilder StemAndLeafByHand(bool missing)
    {
        _ = StemSample(missing);
        string[][] cells =
        [
            ["1", "|", "2", "5", ""], ["2", "|", "1", "3", "3"], ["3", "|", "8", "", ""], ["4", "|", "1", "4", "7"], ["5", "|", "2", "", ""],
        ];
        return Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Table(cells, t =>
            {
                t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Center, TextAlignment.Center, TextAlignment.Center, TextAlignment.Center];
                t.ColumnWidths = [2, 0.5, 1, 1, 1];
                t.CellHeight = 20;
                t.CellColor = Colors.Transparent;
                t.BorderColor = Colors.Transparent;
            })
            .Annotate("Key: 1 | 2 = 12", 0, 0.02, key =>
            {
                key.Coordinates = AnnotationCoordinates.AxesFraction;
                key.Alignment = TextAlignment.Left;
                key.VerticalAlignment = TextVerticalAlignment.Bottom;
                key.OffsetX = 8;
            }));
    }

    private static double[] KagiCloses(bool missing)
    {
        double[] closes = [10, 10.5, 12, 12, 13, 12, 11, 10, 11, 14, 15, 14, 12, 9, 10];
        if (missing) closes[1] = double.NaN;
        return closes;
    }

    /// <summary>Fifteen closes drawn as a Kagi chart with a reversal of 1 by the <c>Kagi</c> verb: five legs, the third thin up to
    /// the shoulder at 13 and thick above it, the fourth thick down to the waist at 10 and thin below it, all in the one colour
    /// the verb's colour group gives the thick line and the thin one.</summary>
    private static FigureBuilder Kagi(bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Kagi(KagiCloses(missing), 1));

    /// <summary>The call chain the <c>Kagi</c> verb replaces, written by hand with the public building blocks: the path of the
    /// fifteen closes worked out by hand, a leg on the centre of its column and a connector from one centre to the next at the
    /// price of the turn, as two lines of one explicit colour (the test side has no access to the internal colour group, so the
    /// same literal colour on each is the public-API equivalent), the thin one 1 point wide and the thick one 3, each stretch of
    /// one width a line of its own parted from the next by a missing value. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether the second close is missing (NaN), which leaves the picture as it is.</param>
    public static FigureBuilder KagiByHand(bool missing)
    {
        _ = KagiCloses(missing);
        var colour = Theme.Default.CycleColors[0];
        double nan = double.NaN;
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Plot([0.5, 0.5, 1.5, 1.5, 2.5, 2.5, nan, 3.5, 3.5, 4.5, 4.5], [10, 13, 13, 10, 10, 13, nan, 10, 9, 9, 10], s => { s.Color = colour; s.LineWidth = 1; })
            .Plot([2.5, 2.5, 3.5, 3.5], [13, 15, 15, 10], s => { s.Color = colour; s.LineWidth = 3; })
            .SetXTickLocator(new FixedLocator([])));
    }

    private static readonly double[] PathX = [1, 2, 4, 5, 7, 8, 9];

    private static double[] PathY(bool missing)
    {
        double[] y = [3, 5, 4, 7, 6, 9, 8];
        if (missing) y[3] = double.NaN;
        return y;
    }

    /// <summary>Seven points visited in order, built by <c>ConnectedScatter</c>: a marker on every point and an arrow from each
    /// point to the next, all in the one colour the verb's colour group gives them.</summary>
    private static FigureBuilder ConnectedScatter(bool missing) =>
        Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax.ConnectedScatter(PathX, PathY(missing)));

    /// <summary>The call chain the <c>ConnectedScatter</c> verb replaces, written by hand with the public building blocks: one
    /// quiver whose arrow <c>i</c> starts on point <c>i</c> and takes the step to point <c>i + 1</c> at a scale of one, with a
    /// head a tenth of its arrow long, then one scatter of the points over it, both in one explicit colour (the test side has no
    /// access to the internal colour group, so the same literal colour on each is the public-API equivalent). The verb must draw
    /// exactly this picture.</summary>
    /// <param name="missing">Whether the fourth point's Y is missing (NaN).</param>
    public static FigureBuilder ConnectedScatterByHand(bool missing)
    {
        double[] y = PathY(missing);
        var colour = Theme.Default.CycleColors[0];
        double[] u = [.. Enumerable.Range(0, PathX.Length - 1).Select(i => PathX[i + 1] - PathX[i])];
        double[] v = [.. Enumerable.Range(0, PathX.Length - 1).Select(i => y[i + 1] - y[i])];
        return Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax
            .Quiver(PathX[..6], y[..6], u, v, s => { s.Color = colour; s.Scale = 1; s.ArrowHeadSize = 0.1; })
            .Scatter(PathX, y, s => s.Color = colour));
    }

    private static double[] RenkoCloses(bool missing)
    {
        double[] closes = [10, 10.4, 11.0, 13.2, 12.5, 11.0, 10.4, 7.8, 8.9, 10.0, 9.0, 15.9];
        if (missing) closes[1] = double.NaN;
        return closes;
    }

    /// <summary>Twelve closes drawn as Renko bricks of 1 by the <c>Renko</c> verb: thirteen bricks, the first at 10 to 11, three
    /// up, four down, then six up, each brick a column wide and its corner on the corner of the one before. The bricks that
    /// rose wear one colour and those that fell another, and the legend names the two kinds.</summary>
    private static FigureBuilder Renko(bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.Renko(RenkoCloses(missing), 1).WithLegend());

    private const int MatShowRows = 6, MatShowColumns = 9;

    /// <summary>A matrix of small whole numbers from a modular rule (integer arithmetic only); the missing one is at row 2,
    /// column 3.</summary>
    private static double[,] MatShowMatrix(bool missing)
    {
        var matrix = new double[MatShowRows, MatShowColumns];
        for (int r = 0; r < MatShowRows; r++)
        {
            for (int c = 0; c < MatShowColumns; c++) matrix[r, c] = (r * 7 + c * 3) % 10;
        }
        if (missing) matrix[2, 3] = double.NaN;
        return matrix;
    }

    /// <summary>A matrix shown as a grid of square cells, built by <c>MatShow</c>: one heatmap with an aspect of one, the
    /// rows and the columns numbered from 0 (the first row on top, the columns along the top as well as the bottom) and a
    /// colour bar.</summary>
    private static FigureBuilder MatShow(bool missing) =>
        Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.MatShow(MatShowMatrix(missing)));

    /// <summary>The call chain the <c>MatShow</c> verb replaces, written by hand with the public building blocks: the heatmap,
    /// an aspect of one, a tick on the centre of every column and row (9 columns and 6 rows are few enough to number each)
    /// reading its own index, the rows read from the top down, the X ticks mirrored on the top edge, and a colour bar. The
    /// verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one cell (row 2, column 3) is missing (NaN).</param>
    public static FigureBuilder MatShowByHand(bool missing)
    {
        string[] columnNames = [.. Enumerable.Range(0, MatShowColumns).Select(c => c.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        string[] rowNames = [.. Enumerable.Range(0, MatShowRows).Select(r => r.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        return Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(MatShowMatrix(missing))
            .SetAspect(1)
            .SetXTickLocator(new FixedLocator([.. Enumerable.Range(0, MatShowColumns).Select(c => c + 0.5)]))
            .SetXTickFormatter(new CategoryFormatter(columnNames))
            .SetYTickLocator(new FixedLocator([.. Enumerable.Range(0, MatShowRows).Select(r => r + 0.5)]))
            .SetYTickFormatter(new CategoryFormatter(rowNames, reversed: true))
            .WithXTicksMirrored()
            .WithColorBar());
    }

    // ---- tranche B ----------------------------------------------------------------------------------------------

    private static readonly string[] SlopeCategories = ["North", "South", "East", "West", "Central"];

    private static double[] SlopeBefore(bool missing)
    {
        double[] before = [42, 55, 38, 61, 47];
        if (missing) before[4] = double.NaN;   // "Central": no start, so no start name
        return before;
    }

    private static double[] SlopeAfter(bool missing)
    {
        double[] after = [51, 49, 44, 66, 39];
        if (missing) after[2] = double.NaN;    // "East": no end, so no end name
        return after;
    }

    /// <summary>A line per category between two moments, built by <c>SlopeGraph</c>: the end name at the line end, the start
    /// name as an annotation in the text colour, the two moments on the X axis, room left of the start for the names. The
    /// missing variant holds one missing start and one missing end.</summary>
    private static FigureBuilder Slope(bool missing) =>
        Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax
            .SlopeGraph(SlopeCategories, new BarGroup("2015", SlopeBefore(missing)), new BarGroup("2025", SlopeAfter(missing))));

    /// <summary>The call chain the <c>SlopeGraph</c> verb replaces, written by hand with the public building blocks: one
    /// marked line for each category on the centres of the two slots the moments stand on, its end name marked for the line
    /// end (not when its end is missing: the end name names the end point, and a line without one has none), its start name
    /// as an annotation right-aligned just left of the start point in the text colour (not when its start is missing), the
    /// two moments as the X ticks, a quarter of the distance between the two columns as the X margin (the room the start names
    /// stand in) and no legend (the names stand at the ends). The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one start ("Central") and one end ("East") are missing (NaN).</param>
    public static FigureBuilder SlopeByHand(bool missing)
    {
        double[] before = SlopeBefore(missing), after = SlopeAfter(missing);
        return Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax =>
        {
            for (int i = 0; i < SlopeCategories.Length; i++)
            {
                int k = i;
                ax.Plot([0.5, 1.5], [before[k], after[k]], s =>
                {
                    s.LineWidth = 2; s.Marker = MarkerStyle.Circle; s.MarkerSize = 8; s.Label = SlopeCategories[k];
                });
                if (double.IsFinite(after[k])) ax.WithDirectLabel();
                if (double.IsFinite(before[k])) ax.Annotate(SlopeCategories[k], 0.5 - 0.04, before[k], a => { a.Alignment = TextAlignment.Right; a.VerticalAlignment = TextVerticalAlignment.Center; });
            }
            ax.SetXMargin(0.25).SetXTickLocator(new FixedLocator([0.5, 1.5])).SetXTickFormatter(new CategoryFormatter(["2015", "2025"]))
              .WithLegend(visible: false);
        });
    }

    private static readonly string[] DotPlotCategories = ["Ruby", "Go", "Rust", "Java", "C#", "Python", "Kotlin"];

    private static readonly double[] DotPlotOlder = [31, 48, 62, 55, 67, 80, 44];

    private static double[] DotPlotNewer(bool missing)
    {
        double[] newer = [35, 52, 60, 58, 71, 84, 50];
        if (missing) newer[3] = double.NaN;   // "Java"
        return newer;
    }

    /// <summary>One dot per category and group, built by <c>DotPlot</c>: the categories on the Y axis, the largest value of
    /// the first group on top, the guide lines being the grid.</summary>
    private static FigureBuilder DotPlot(bool missing) =>
        Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax
            .DotPlot(DotPlotCategories, [new BarGroup("2015", DotPlotOlder), new BarGroup("2025", DotPlotNewer(missing))]));

    /// <summary>The call chain the <c>DotPlot</c> verb replaces, written by hand with the public building blocks: the categories
    /// ordered by the first group, largest first (a stable order), the largest in the top row, one scatter series of circles
    /// for each group on the centres of the rows, the names as the Y ticks read from the top down, the grid drawn along the
    /// Y axis as the guide lines, and the legend in the lower right corner, which the order of the rows leaves empty. The verb
    /// must draw exactly this picture.</summary>
    /// <param name="missing">Whether one value of the second group ("Java") is missing (NaN).</param>
    public static FigureBuilder DotPlotByHand(bool missing)
    {
        double[] newer = DotPlotNewer(missing);
        int n = DotPlotCategories.Length;
        int[] order = Enumerable.Range(0, n).OrderByDescending(i => DotPlotOlder[i]).ToArray();
        string[] names = order.Select(i => DotPlotCategories[i]).ToArray();
        // Row d (0 = the largest) stands in slot n - 1 - d, so the largest is on top of an axis that counts upward.
        double[] rows = Enumerable.Range(0, n).Select(d => n - 1 - d + 0.5).ToArray();
        return Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax
            .Scatter(order.Select(i => DotPlotOlder[i]).ToArray(), rows, s => { s.Marker = MarkerStyle.Circle; s.MarkerSize = 50; s.Label = "2015"; })
            .Scatter(order.Select(i => newer[i]).ToArray(), rows, s => { s.Marker = MarkerStyle.Circle; s.MarkerSize = 50; s.Label = "2025"; })
            .SetYTickLocator(new FixedLocator(Enumerable.Range(0, n).Select(slot => slot + 0.5).ToArray()))
            .SetYTickFormatter(new CategoryFormatter(names, reversed: true))
            .WithGrid(g => g with { Visible = true, Axis = GridAxis.Y })
            .WithLegend(LegendPosition.LowerRight));
    }

    /// <summary>A sample sorted up the Y axis against the standard normal quantiles at Filliben's positions, with the
    /// least-squares line through the points as the reference, built by <c>QQPlot</c>: the points are one scatter series
    /// (named through the callback, so the legend has one entry), the reference a trendline, which takes no colour slot. The
    /// sample is made by integer arithmetic and comes unsorted (the verb sorts); the missing observation is left out.
    /// <para>The quantiles come from a logarithm, so this shape is compared in-process only, never against stored
    /// bytes.</para></summary>
    private static FigureBuilder QQ(bool missing)
    {
        double[] sample = [.. Enumerable.Range(0, 40).Select(i => 10 + (i * 7919 % 40 - 19.5) / 9.0 + i % 5 * 0.15)];
        if (missing) sample[7] = double.NaN;
        return Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax => ax
            .QQPlot(sample, parts => parts.Points.Label = "sample")
            .WithLegend());
    }

    /// <summary>A sample sorted up the Y axis against the quantiles of Student's t distribution with 3 degrees of freedom at
    /// Filliben's positions, built by <c>ProbabilityPlot</c>: the same parts as the Q-Q plot (a scatter series named through the
    /// callback and a trendline, which takes no colour slot), the X axis naming the reference. The sample is made by integer
    /// arithmetic and comes unsorted; the missing observation is left out.
    /// <para>The quantiles come from a root and an incomplete beta function, so this shape is compared in-process only, never
    /// against stored bytes.</para></summary>
    private static FigureBuilder ProbabilityPlot(bool missing)
    {
        double[] sample = [.. Enumerable.Range(0, 40).Select(i => 10 + (i * 7919 % 40 - 19.5) / 9.0 + i % 5 * 0.15)];
        if (missing) sample[7] = double.NaN;
        return Plt.Create().WithSize(600, 600).AddSubPlot(1, 1, 1, ax => ax
            .ProbabilityPlot(sample, QQReference.StudentT(3), parts => parts.Points.Label = "sample")
            .WithLegend());
    }

    private static readonly double[] ControlDays = [.. Enumerable.Range(1, 25).Select(i => (double)i)];

    /// <summary>A steady process (50 -/+ 1 in steps of a half) with one spike up (58, day 18) and one dip down (42, day 22),
    /// made by integer arithmetic: the moving range barely notices the two, so both lie beyond the estimated limits (43.70 and
    /// 56.22; with the missing value 43.70 and 56.30).</summary>
    private static double[] ControlValues(bool missing)
    {
        double[] values = [.. Enumerable.Range(0, 25).Select(i => 50 + ((i * 7) % 5 - 2) * 0.5)];
        values[17] = 58; values[21] = 42;
        if (missing) values[5] = double.NaN;
        return values;
    }

    /// <summary>An individuals chart built by <c>ControlChart</c>: the values as a marked line, the centre and the limits
    /// estimated from the average moving range as reference lines, and the two points beyond a limit as a scatter series that
    /// takes the next colour. Both series are named through the callback, so the legend has two entries.</summary>
    private static FigureBuilder ControlChart(bool missing) =>
        Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ControlChart(ControlDays, ControlValues(missing), configure: parts =>
            {
                parts.Values.Label = "measurement";
                parts.Violations.Label = "beyond a limit";
            })
            .WithLegend());

    /// <summary>The measurements of <see cref="ControlChart"/> drawn by <c>ShewhartChart</c> with all eight of Nelson's tests:
    /// the same parts (a marked line, a scatter series of the flagged points that takes the next colour, three reference
    /// lines), the scatter now holding the union of what the eight tests flag. Both series are named through the callback.</summary>
    private static FigureBuilder ShewhartChart(bool missing) =>
        Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.Individuals(ControlDays, ControlValues(missing)), ControlRules.All, configure: parts =>
            {
                parts.Values.Label = "measurement";
                parts.Violations.Label = "out of control";
            })
            .WithLegend());

    /// <summary>The moving ranges of the same measurements, built by <c>ShewhartChart</c> from
    /// <c>ControlChartData.MovingRange</c>: the distance between each value and the one before it as a marked line, the average
    /// range as the centre, 3.267 times it as the upper limit and 0 as the lower, and the ranges beyond the upper limit flagged
    /// by rule 1 (the jumps to and from the spike and the dip). Both series are named through the callback.</summary>
    private static FigureBuilder MovingRangeChart(bool missing) =>
        Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.MovingRange(ControlDays, ControlValues(missing)), configure: parts =>
            {
                parts.Values.Label = "moving range";
                parts.Violations.Label = "beyond the limit";
            })
            .WithLegend());

    /// <summary>Twenty subgroups of five measurements around 50, made by integer arithmetic (steps of a quarter), the thirteenth
    /// shifted up by 4. The variant with a missing value has one observation of the sixth subgroup lost (NaN), which leaves that
    /// subgroup out of the chart as a whole.</summary>
    private static double[][] ControlSubgroups(bool missing)
    {
        double[][] subgroups = [.. Enumerable.Range(0, 20).Select(i => Enumerable.Range(0, 5).Select(j => 50 + ((i * 7 + j * 3) % 9 - 4) * 0.25).ToArray())];
        subgroups[12] = [.. subgroups[12].Select(v => v + 4)];
        if (missing) subgroups[5][2] = double.NaN;
        return subgroups;
    }

    /// <summary>The subgroup means of <see cref="ControlSubgroups"/> built by <c>ShewhartChart</c> from <c>ControlChartData.XBar</c>:
    /// a marked line, the grand mean and A2 times the average range either side of it, the points flagged by tests 1 and 2 as the
    /// scatter series that takes the next colour. Both series are named through the callback.</summary>
    private static FigureBuilder XBarChart(bool missing) =>
        Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.XBar(ControlSubgroups(missing)), ControlRules.Nelson1 | ControlRules.Nelson2, configure: parts =>
            {
                parts.Values.Label = "subgroup mean";
                parts.Violations.Label = "out of control";
            })
            .WithLegend());

    /// <summary>The subgroup ranges of <see cref="ControlSubgroups"/> built by <c>ShewhartChart</c> from
    /// <c>ControlChartData.Range</c>: the average range and D3 and D4 times it as the limits.</summary>
    private static FigureBuilder RangeChart(bool missing) =>
        Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.Range(ControlSubgroups(missing)), ControlRules.Nelson1 | ControlRules.Nelson2, configure: parts =>
            {
                parts.Values.Label = "subgroup range";
                parts.Violations.Label = "out of control";
            })
            .WithLegend());

    /// <summary>Twenty-five samples of 200 items with 4 to 8 defectives and one sample with 19 (a spike), built by
    /// <c>ShewhartChart</c> from <c>ControlChartData.P</c>: the proportions as a marked line, the pooled proportion and its
    /// binomial limits. The variant with a missing value has no result for the sixth sample.</summary>
    private static FigureBuilder PChart(bool missing)
    {
        double[] defectives = [.. Enumerable.Range(0, 25).Select(i => (double)(4 + i * 7 % 5))];
        defectives[17] = 19;
        if (missing) defectives[5] = double.NaN;
        return Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.P(defectives, 200), configure: parts =>
            {
                parts.Values.Label = "proportion defective";
                parts.Violations.Label = "beyond a limit";
            })
            .WithLegend());
    }

    /// <summary>Twenty-five counts of 3 to 7 defects and one count of 16 (a spike), built by <c>ShewhartChart</c> from
    /// <c>ControlChartData.C</c>: the counts as a marked line, the average and its Poisson limits, the lower one at 0. The
    /// variant with a missing value has no count for the sixth sample.</summary>
    private static FigureBuilder CChart(bool missing)
    {
        double[] counts = [.. Enumerable.Range(0, 25).Select(i => (double)(3 + i * 7 % 5))];
        counts[17] = 16;
        if (missing) counts[5] = double.NaN;
        return Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.C(counts), configure: parts =>
            {
                parts.Values.Label = "defects";
                parts.Violations.Label = "beyond a limit";
            })
            .WithLegend());
    }

    /// <summary>The call chain the <c>ControlChart</c> verb replaces, written by hand with the public building blocks: the
    /// mean of the values that are there, the average moving range between neighbouring values that are there over d2 =
    /// 1.128 as sigma, the limits three sigma either side of the mean; the values as a marked line, the values strictly beyond
    /// a limit as a scatter series after it, a solid centre line and dashed limits named at their right ends. The verb must
    /// draw exactly this picture.</summary>
    /// <param name="missing">Whether one value (day 6) is missing (NaN).</param>
    public static FigureBuilder ControlChartByHand(bool missing)
    {
        double[] values = ControlValues(missing);
        double[] there = values.Where(double.IsFinite).ToArray();
        double sum = 0, ranges = 0;
        for (int i = 0; i < there.Length; i++)
        {
            sum += there[i];
            if (i > 0) ranges += Math.Abs(there[i] - there[i - 1]);
        }
        double centre = sum / there.Length;
        double sigma = ranges / (there.Length - 1) / 1.128;
        double upper = centre + 3 * sigma, lower = centre - 3 * sigma;
        int[] beyond = Enumerable.Range(0, values.Length).Where(i => values[i] > upper || values[i] < lower).ToArray();
        return Plt.Create().WithSize(800, 420).AddSubPlot(1, 1, 1, ax => ax
            .Plot(ControlDays, values, s => { s.Marker = MarkerStyle.Circle; s.Label = "measurement"; })
            .Scatter(beyond.Select(i => ControlDays[i]).ToArray(), beyond.Select(i => values[i]).ToArray(), s => s.Label = "beyond a limit")
            .AxHLine(centre, r => { r.LineStyle = LineStyle.Solid; r.Label = "CL"; })
            .AxHLine(lower, r => r.Label = "LCL")
            .AxHLine(upper, r => r.Label = "UCL")
            .WithLegend());
    }

    /// <summary>Two arms of twelve subjects, in months. The treatment arm comes unsorted (the verb sorts), with the subject
    /// censored at 14 listed before the event at 14 (it is at risk at 14 all the same), and ends on a censoring at 30, so its
    /// curve is held at its last level to 30; the control arm ends on an event at 20, where its curve reaches 0.
    /// <para>The verb refuses a missing duration (NaN): a subject without a time cannot be counted at risk, as lifelines
    /// refuses it too. So the variant with a missing value holds survival data's own missing outcome: one control subject
    /// lost to follow-up at 9, censored where its event was, which removes the step at 9.</para></summary>
    private static SurvivalGroup[] SurvivalArms(bool missing)
    {
        bool[] control = [true, true, true, false, true, true, true, false, true, true, false, true];
        if (missing) control[6] = false;   // lost to follow-up at 9
        return
        [
            new("Treatment", [14, 3, 6, 5, 9, 6, 11, 14, 18, 22, 25, 30], [false, true, true, false, false, true, true, true, true, false, true, false]),
            new("Control", [2, 3, 4, 4, 6, 7, 9, 10, 12, 15, 16, 20], control),
        ];
    }

    /// <summary>One step line per arm, built by <c>KaplanMeier</c>: the product-limit estimate from (0, 1), each arm in a
    /// colour of its own and named in the legend, the Y axis titled "Survival probability".</summary>
    private static FigureBuilder KaplanMeier(bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .KaplanMeier(SurvivalArms(missing))
            .WithLegend());

    /// <summary>The survival chart of <see cref="KaplanMeier"/> with its risk table stacked under it (height 4 to 1, the X axis
    /// shared), built by <c>RiskTable</c>: a row of counts for each arm at every fifth month, and the log-rank test below the
    /// rows. The colours and the legend are the survival chart's; the table adds annotations, which take no colour slot.</summary>
    private static FigureBuilder RiskTable(bool missing) =>
        Plt.Create().WithSize(800, 600).WithGridSpec(2, 1, heightRatios: [4, 1])
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.KaplanMeier(SurvivalArms(missing)).WithLegend(), key: "survival")
            .AddSubPlot(GridPosition.Single(1, 0), ax => ax.ShareX("survival").RiskTable(SurvivalArms(missing), [0, 5, 10, 15, 20, 25, 30]));

    /// <summary>The call chain the <c>KaplanMeier</c> verb replaces, written by hand with the public building blocks: the
    /// risk sets worked out by hand from the sorted subjects (E an event, C a censoring), each level the one before times
    /// (at risk - events) / at risk, one line per arm stepping after its points, the arm's name as its label, a Greenwood
    /// band (plain form, <see cref="Math.Sqrt"/> only) around each curve, a censor mark at each C above, and the Y axis
    /// titled. Band and marks take the curve's own colour explicitly (<see cref="Theme.Default"/>'s cycle, read by the same
    /// index the curve itself takes — no two series here share a <c>ColorGroup</c>, because the test side has no access to
    /// that internal field; setting the same literal colour on each is the public-API equivalent). The verb must draw
    /// exactly this picture.
    /// <code>
    ///   Treatment: 3E 5C 6E 6E 9C 11E 14E 14C 18E 22C 25E 30C
    ///     t        3   6   11  14  18  25   (30: held)
    ///     at risk  12  10  7   6   4   2
    ///     events   1   2   1   1   1   1
    ///   Control:   2E 3E 4E 4C 6E 7E 9E 10C 12E 15E 16C 20E    (missing: 9C)
    ///     t        2   3   4   6   7   9   12  15  20
    ///     at risk  12  11  10  8   7   6   4   3   1
    ///     events   1   1   1   1   1   1   1   1   1
    /// </code></summary>
    /// <param name="missing">Whether one control subject is lost to follow-up at 9 instead of having its event there.</param>
    public static FigureBuilder KaplanMeierByHand(bool missing)
    {
        static double[] Levels(int[] atRisk, int[] events)
        {
            double level = 1;
            return [1, .. atRisk.Select((n, i) => level *= (double)(n - events[i]) / n)];
        }

        // Plain Greenwood: the running sum of d_i / (n_i (n_i - d_i)) over event points only, S(t)^2 times that under one
        // square root, +/- 1.959963984540054 standard errors, clamped to [0, 1]. survivalWithSeed holds S(0)=1 first.
        static (double[] Lower, double[] Upper) Greenwood(double[] survivalWithSeed, int[] atRisk, int[] events)
        {
            const double z = 1.959963984540054;
            var lower = new double[survivalWithSeed.Length];
            var upper = new double[survivalWithSeed.Length];
            lower[0] = upper[0] = survivalWithSeed[0];
            double cumulative = 0;
            for (int i = 0; i < atRisk.Length; i++)
            {
                if (atRisk[i] > events[i]) cumulative += (double)events[i] / (atRisk[i] * (atRisk[i] - events[i]));
                double s = survivalWithSeed[i + 1];
                double se = Math.Sqrt(s * s * cumulative);
                lower[i + 1] = Math.Clamp(s - z * se, 0, 1);
                upper[i + 1] = Math.Clamp(s + z * se, 0, 1);
            }

            return (lower, upper);
        }

        // The level the step curve (times, survival) holds AT a given time: the last point at or before it.
        static double LevelAt(double[] times, double[] survival, double time)
        {
            double level = survival[0];
            for (int i = 0; i < times.Length && times[i] <= time; i++) level = survival[i];
            return level;
        }

        double[] treatmentTimes = [0, 3, 6, 11, 14, 18, 25];
        int[] treatmentAtRisk = [12, 10, 7, 6, 4, 2];
        int[] treatmentEvents = [1, 2, 1, 1, 1, 1];
        double[] treatment = Levels(treatmentAtRisk, treatmentEvents);
        double[] treatmentCensored = [5, 9, 14, 22, 30];

        double[] controlTimes = missing ? [0, 2, 3, 4, 6, 7, 12, 15, 20] : [0, 2, 3, 4, 6, 7, 9, 12, 15, 20];
        int[] controlAtRisk = missing ? [12, 11, 10, 8, 7, 4, 3, 1] : [12, 11, 10, 8, 7, 6, 4, 3, 1];
        int[] controlEvents = missing ? [1, 1, 1, 1, 1, 1, 1, 1] : [1, 1, 1, 1, 1, 1, 1, 1, 1];
        double[] control = Levels(controlAtRisk, controlEvents);
        double[] controlCensored = missing ? [4, 9, 10, 16] : [4, 10, 16];

        // The extension to the last follow-up: only the treatment arm is censored after its last event (30 > 25); both
        // control arms end ON an event (20), so neither extends.
        double[] treatmentTimesHeld = [.. treatmentTimes, 30];
        double[] treatmentHeld = [.. treatment, treatment[^1]];
        var (treatmentLower, treatmentUpper) = Greenwood(treatment, treatmentAtRisk, treatmentEvents);
        double[] treatmentLowerHeld = [.. treatmentLower, treatmentLower[^1]];
        double[] treatmentUpperHeld = [.. treatmentUpper, treatmentUpper[^1]];
        var (controlLower, controlUpper) = Greenwood(control, controlAtRisk, controlEvents);

        Color treatmentColour = Theme.Default.CycleColors[0];
        Color controlColour = Theme.Default.CycleColors[1];

        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .Plot(treatmentTimesHeld, treatmentHeld, s => { s.DrawStyle = DrawStyle.StepsPost; s.Label = "Treatment"; s.Color = treatmentColour; })
            .FillBetween(treatmentTimesHeld, treatmentUpperHeld, treatmentLowerHeld, s => { s.Color = treatmentColour; s.Alpha = 0.2; })
            .Scatter(treatmentCensored, [.. treatmentCensored.Select(t => LevelAt(treatmentTimesHeld, treatmentHeld, t))],
                s => { s.Marker = MarkerStyle.Plus; s.Color = treatmentColour; })
            .Plot(controlTimes, control, s => { s.DrawStyle = DrawStyle.StepsPost; s.Label = "Control"; s.Color = controlColour; })
            .FillBetween(controlTimes, controlUpper, controlLower, s => { s.Color = controlColour; s.Alpha = 0.2; })
            .Scatter(controlCensored, [.. controlCensored.Select(t => LevelAt(controlTimes, control, t))],
                s => { s.Marker = MarkerStyle.Plus; s.Color = controlColour; })
            .SetYLabel("Survival probability")
            .WithLegend());
    }

    private static readonly string[] IrisDimensions = ["sepal L", "sepal W", "petal L", "petal W"];

    private static readonly string[] IrisClasses = ["setosa", "versicolor", "virginica"];

    /// <summary>Twelve flowers, four of each class, one value per dimension. The variant with a missing value leaves the petal
    /// length of one versicolor row out.</summary>
    private static double[][] IrisRows(bool missing)
    {
        double[][] rows =
        [
            [5.1, 3.5, 1.4, 0.2], [4.9, 3.0, 1.4, 0.2], [5.0, 3.6, 1.4, 0.2], [5.4, 3.9, 1.7, 0.4],
            [7.0, 3.2, 4.7, 1.4], [6.4, 3.2, 4.5, 1.5], [6.9, 3.1, 4.9, 1.5], [5.5, 2.3, 4.0, 1.3],
            [6.3, 3.3, 6.0, 2.5], [5.8, 2.7, 5.1, 1.9], [7.1, 3.0, 5.9, 2.1], [6.5, 3.0, 5.8, 2.2],
        ];
        if (missing) rows[5][2] = double.NaN;
        return rows;
    }

    /// <summary>One line series per class, built by <c>ParallelCoordinates</c>: every row a stretch of its class's line with a
    /// missing point between rows, each column scaled to 0..1, an axis line per column, the column names on the X axis and
    /// every class named in the legend.</summary>
    private static FigureBuilder ParallelCoordinates(bool missing)
    {
        double[][] rows = IrisRows(missing);
        BarGroup[] groups = [.. rows.Select((r, i) => new BarGroup(IrisClasses[i / 4], r))];
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(IrisDimensions, groups));
    }

    /// <summary>The call chain the <c>ParallelCoordinates</c> verb replaces, written by hand with the public building blocks:
    /// each column scaled min to max over the values that are there (a missing value stays missing), a solid axis line per
    /// column in the reference line's own grey, one line series per class whose rows are stretches with a missing point
    /// between them, the class as the line's name, the column names as the X ticks on the slot centres and a Y axis without
    /// ticks. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one petal length is missing (NaN).</param>
    public static FigureBuilder ParallelCoordinatesByHand(bool missing)
    {
        double[][] rows = IrisRows(missing);
        double[] centres = Enumerable.Range(0, IrisDimensions.Length).Select(d => d + 0.5).ToArray();
        double[] low = Enumerable.Range(0, 4).Select(d => rows.Select(r => r[d]).Where(double.IsFinite).Min()).ToArray();
        double[] high = Enumerable.Range(0, 4).Select(d => rows.Select(r => r[d]).Where(double.IsFinite).Max()).ToArray();
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax =>
        {
            foreach (double centre in centres) ax.AxVLine(centre, r => r.LineStyle = LineStyle.Solid);
            for (int c = 0; c < IrisClasses.Length; c++)
            {
                var xs = new List<double>(); var ys = new List<double>();
                for (int r = c * 4; r < c * 4 + 4; r++)
                {
                    if (xs.Count > 0) { xs.Add(double.NaN); ys.Add(double.NaN); }
                    for (int d = 0; d < 4; d++)
                    {
                        xs.Add(centres[d]);
                        ys.Add(double.IsFinite(rows[r][d]) ? (rows[r][d] - low[d]) / (high[d] - low[d]) : double.NaN);
                    }
                }
                int cls = c;
                ax.Plot([.. xs], [.. ys], s => { s.LineWidth = 1.2; s.Label = IrisClasses[cls]; });
            }
            // Each column reads in its own units: the locator's numbers over the column's own range, a six-pixel mark on the
            // column line and the number to its left, at the height that value is drawn at.
            for (int d = 0; d < 4; d++)
            {
                foreach (double value in new AutoLocator().Locate(low[d], high[d]).Where(v => v >= low[d] - 1e-9 * (high[d] - low[d]) && v <= high[d] + 1e-9 * (high[d] - low[d])))
                {
                    double height = (value - low[d]) / (high[d] - low[d]);
                    ax.Annotate(value.ToString("G6", System.Globalization.CultureInfo.InvariantCulture), centres[d], height, centres[d], height, a =>
                    {
                        a.Alignment = TextAlignment.Right;
                        a.VerticalAlignment = TextVerticalAlignment.Center;
                        a.OffsetX = -6;
                        a.ArrowStyle = ArrowStyle.Simple;
                    });
                }
            }

            ax.SetXTickLocator(new FixedLocator(centres)).SetXTickFormatter(new CategoryFormatter(IrisDimensions))
              .SetYTickLocator(new FixedLocator([]));
        });
    }

    private static readonly string[] StreamNames = ["Rock", "Pop", "Jazz", "Folk"];

    /// <summary>Four streams over thirty positions. The thicknesses are multiples of a half, so every sum and every halving
    /// of them is exact and the hand recipe and the verb meet bit for bit. The variant with a missing value leaves one
    /// thickness of the second stream out.</summary>
    private static double[][] StreamLayers(bool missing)
    {
        double[][] layers = Enumerable.Range(0, 4).Select(k => Ramp(30).Select(i => 3.0 + (i * (k + 3) * 7 % 13) * 0.5 + k).ToArray()).ToArray();
        if (missing) layers[1][4] = double.NaN;
        return layers;
    }

    /// <summary>One filled layer per stream on a floating baseline, built by <c>StreamGraph</c> with its default baseline
    /// (the wiggle): each layer is an area wearing one colour of the cycle and named in the legend, and the Y axis has no
    /// ticks.</summary>
    private static FigureBuilder StreamGraph(bool missing) => StreamGraphWith(StackedBaseline.Wiggle, missing);

    /// <summary>The stream graph of <see cref="Named"/>("stream-graph") on <paramref name="baseline"/>, built by the verb.</summary>
    /// <param name="baseline">Where the stack stands.</param>
    /// <param name="missing">Whether one thickness of the second stream is missing (NaN).</param>
    public static FigureBuilder StreamGraphWith(StackedBaseline baseline, bool missing) =>
        Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax => ax
            .StreamGraph(Ramp(30), [.. StreamLayers(missing).Select((values, k) => new BarGroup(StreamNames[k], values))], baseline)
            .WithLegend());

    /// <summary>The call chain the <c>StreamGraph</c> verb replaces on its <see cref="StackedBaseline.Symmetric"/> baseline,
    /// written by hand with the public building blocks: the middle of the total on the middle line, each layer an area filled
    /// between its lower edge (the one below it) and its upper edge (its lower edge plus its thickness), the stream's name as
    /// the area's label, and a Y axis without ticks. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether one thickness of the second stream is missing (NaN).</param>
    public static FigureBuilder StreamGraphByHand(bool missing)
    {
        double[] x = Ramp(30);
        double[][] layers = StreamLayers(missing);
        return Plt.Create().WithSize(800, 450).AddSubPlot(1, 1, 1, ax =>
        {
            double[] lower = x.Select((_, j) => -0.5 * layers.Sum(l => l[j])).ToArray();
            for (int k = 0; k < layers.Length; k++)
            {
                double[] upper = lower.Select((b, j) => b + layers[k][j]).ToArray();
                int layer = k;
                ax.FillBetween(x, upper, lower, s => { s.Alpha = StreamAlpha; s.Label = StreamNames[layer]; });
                lower = upper;
            }
            ax.SetYTickLocator(new FixedLocator([])).WithLegend();
        });
    }

    /// <summary>The transparency the verb gives a stream: opaque, so each layer wears exactly the colour of its legend entry.</summary>
    private const double StreamAlpha = 1;

    private static readonly string[] MarimekkoColumns = ["Retail", "Online", "Wholesale"];

    /// <summary>Three brands over three channels, the channel totals 60, 60 and 30 of 150. The variant with a missing size
    /// leaves brand B's online size out.</summary>
    private static BarGroup[] MarimekkoSegments(bool missing) =>
    [
        new("A", [30, 15, 5]),
        new("B", [20, missing ? double.NaN : 25, 10]),
        new("C", [10, 20, 15]),
    ];

    /// <summary>A white outline along the top of each cell, set through the callback.</summary>
    private static void StyleMarimekko(AreaSeries s)
    {
        s.EdgeColor = Color.FromHex("#FFFFFF"); s.LineWidth = 1;
    }

    /// <summary>Stacked segments whose column widths are shares, built by <c>Marimekko</c>: one area per segment, one
    /// rectangle in each column, the column names on the column centres.</summary>
    private static FigureBuilder Marimekko(bool missing) =>
        Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax
            .Marimekko(MarimekkoColumns, MarimekkoSegments(missing), StyleMarimekko)
            .WithLegend(LegendPosition.Right));

    /// <summary>The call chain the <c>Marimekko</c> verb replaces, written by hand with the public building blocks: the
    /// column edges are the running sums of the column totals over the grand total and the cells of a column the running
    /// sums of its sizes over its total, both in percent; each segment is one area of two points per column (a missing
    /// point between two columns, so no column is joined to the next), opaque and named; the column names stand on the
    /// column centres, the Y axis reads percent and has no margin. A missing size adds nothing and has no cell. The verb must
    /// draw exactly this picture.</summary>
    /// <param name="missing">Whether brand B's online size is missing (NaN).</param>
    public static FigureBuilder MarimekkoByHand(bool missing)
    {
        var segments = MarimekkoSegments(missing);
        int n = MarimekkoColumns.Length;
        double[] totals = [.. Enumerable.Range(0, n).Select(c => segments.Select(s => s.Values[c]).Where(double.IsFinite).Aggregate(0.0, (sum, v) => sum + v))];
        double[] running = new double[n + 1];
        for (int c = 0; c < n; c++) running[c + 1] = running[c] + totals[c];
        double[] edges = [.. running.Select(r => r / running[n] * 100)];
        double[] centres = [.. Enumerable.Range(0, n).Select(c => (edges[c] + edges[c + 1]) / 2)];
        return Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax =>
        {
            double[] floor = new double[n];
            foreach (var segment in segments)
            {
                var x = new List<double>(); var top = new List<double>(); var bottom = new List<double>();
                for (int c = 0; c < n; c++)
                {
                    if (c > 0) { x.Add(double.NaN); top.Add(double.NaN); bottom.Add(double.NaN); }
                    double size = segment.Values[c];
                    bool drawn = double.IsFinite(size) && size > 0;
                    double low = drawn ? floor[c] / totals[c] * 100 : double.NaN;
                    if (drawn) floor[c] += size;
                    double high = drawn ? floor[c] / totals[c] * 100 : double.NaN;
                    x.AddRange([edges[c], edges[c + 1]]); top.AddRange([high, high]); bottom.AddRange([low, low]);
                }

                string label = segment.Label;
                ax.FillBetween([.. x], [.. top], [.. bottom], s => { s.Alpha = 1; s.Label = label; StyleMarimekko(s); });
            }

            ax.SetXTickLocator(new FixedLocator(centres)).SetXTickFormatter(new CategoryFormatter(MarimekkoColumns, centres))
              .SetYTickFormatter(new PercentFormatter(100)).SetYMargin(0)
              .WithLegend(LegendPosition.Right);
        });
    }

    /// <summary>Two samples, built by <c>Raincloud</c>: a half violin and a narrow box, one dataset each per sample, and
    /// one scatter of every sample's raw points, jittered a fixed amount — all three in the one colour the verb's
    /// colour group gives them.</summary>
    private static NamedSample[] RaincloudGroups(bool missing)
    {
        double[] a = [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4];
        if (missing) a[3] = double.NaN;
        return [new("A", a), new("B", [5.6, 6.1, 6.5, 6.9, 7.2, 7.4, 7.9, 8.3, 8.8, 9.1, 9.6, 10.2])];
    }

    private static FigureBuilder Raincloud(bool missing) =>
        Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.Raincloud(RaincloudGroups(missing)));

    /// <summary>The call chain the <c>Raincloud</c> verb replaces, written by hand with the public building blocks: one
    /// violin and one box carrying both samples as two datasets each (a dataset left without a position stands at its
    /// own index, so neither sets one), one scatter of both samples' points jittered the same fixed amount the verb
    /// uses, all three in one explicit colour (the test side has no access to the internal colour group, so the same
    /// literal colour on each is the public-API equivalent). Only the violin carries a label, so it alone stands for
    /// the group of three in the legend; the samples are also named along the X axis. The verb must draw exactly this
    /// picture.</summary>
    /// <param name="missing">Whether sample A's fourth observation is missing (NaN).</param>
    public static FigureBuilder RaincloudByHand(bool missing)
    {
        var groups = RaincloudGroups(missing);
        double[][] samples = [.. groups.Select(g => g.Values)];
        double[] pointX = samples.SelectMany((s, g) => s.Select((_, i) => g + 0.35 + (i % 4) * 0.03)).ToArray();
        double[] pointY = samples.SelectMany(s => s).ToArray();
        var colour = Theme.Default.CycleColors[0];
        // One tick per group at its dataset index (0, 1, ...), not the bar-style half-slot a category-name helper
        // would place it at: the violin and the box already stand at that same index, left without a Positions of
        // their own.
        double[] positions = [.. Enumerable.Range(0, groups.Length).Select(k => (double)k)];
        return Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax
            .Violin(samples, s => { s.Side = ViolinSide.High; s.Color = colour; s.Alpha = 0.5; s.Label = "Samples"; })
            .BoxPlot(samples, s => { s.Widths = 0.1; s.Color = colour; s.ShowOutliers = false; })
            .Scatter(pointX, pointY, s => { s.Color = colour; s.MarkerSize = 6; s.Alpha = 1; })
            .SetXTickLocator(new FixedLocator(positions))
            .SetXTickFormatter(new CategoryFormatter([.. groups.Select(g => g.Label)], positions)));
    }

    /// <summary>Steps up and down between an opening and a closing total, every colour set: the first and the last bar are
    /// totals (they stand on zero in the total colour), the three between are steps. With a missing refund the closing
    /// total is still known, as it states its own level.</summary>
    private static FigureBuilder WaterfallTotal(bool missing)
    {
        string[] steps = ["Start", "Sales", "Refunds", "Costs", "Total"];
        double[] values = [100, 40, -15, -30, 95];
        if (missing) values[2] = double.NaN;
        return Plt.Create().WithSize(700, 450).AddSubPlot(1, 1, 1, ax => ax
            .Waterfall(steps, values, s =>
            {
                s.IncreaseColor = Color.FromHex("#2CA02C"); s.DecreaseColor = Color.FromHex("#D62728");
                s.TotalColor = Color.FromHex("#1F77B4"); s.BarWidth = 0.5;
                s.TotalIndices = [0, 4];
            }));
    }

    /// <summary>Five integer values, signed, built by <c>Horizon</c>: 2 bands fold the non-negative part in one colour
    /// and 2 more fold the negative part in a second, h = 8 / 2 = 4.</summary>
    private static double[] HorizonValues(bool missing)
    {
        double[] values = [5, -3, 8, -1, 0];
        if (missing) values[2] = double.NaN;
        return values;
    }

    private static FigureBuilder Horizon(bool missing) =>
        Plt.Create().WithSize(700, 160).AddSubPlot(1, 1, 1, ax => ax.Horizon([0, 1, 2, 3, 4], HorizonValues(missing), bands: 2));

    /// <summary>The call chain the <c>Horizon</c> verb replaces, written by hand with the public building blocks: h = the
    /// largest absolute FINITE value over the band count (8 / 2 = 4 without the missing value, 5 / 2 = 2.5 with it, since
    /// the missing one was the largest), band k of the non-negative part is clamp(y - k x h, 0, h) in the first cycle
    /// colour, band k of the negative part is -clamp(-y - k x h, 0, h) in the second, each band's Alpha (k + 1) / 2, none
    /// of them labelled. The verb must draw exactly this picture.</summary>
    /// <param name="missing">Whether the third value (8, the largest) is missing (NaN).</param>
    public static FigureBuilder HorizonByHand(bool missing)
    {
        double[] x = [0, 1, 2, 3, 4];
        double[] y = HorizonValues(missing);
        double h = y.Where(double.IsFinite).Select(Math.Abs).Max() / 2;
        Color positiveColour = Theme.Default.CycleColors[0];
        Color negativeColour = Theme.Default.CycleColors[1];

        double[] Fold(int k, bool negative) =>
            [.. y.Select(v => !double.IsFinite(v) ? double.NaN
                : negative ? -Math.Clamp(-v - k * h, 0, h)
                : Math.Clamp(v - k * h, 0, h))];

        return Plt.Create().WithSize(700, 160).AddSubPlot(1, 1, 1, ax => ax
            .FillBetween(x, Fold(0, negative: false), null, s => { s.Color = positiveColour; s.Alpha = 0.5; })
            .FillBetween(x, Fold(1, negative: false), null, s => { s.Color = positiveColour; s.Alpha = 1.0; })
            .FillBetween(x, Fold(0, negative: true), null, s => { s.Color = negativeColour; s.Alpha = 0.5; })
            .FillBetween(x, Fold(1, negative: true), null, s => { s.Color = negativeColour; s.Alpha = 1.0; }));
    }
}
