// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Samples.Console;

/// <summary>
/// The pictures the wiki shows that no other sample draws: the gallery tiles of Chart-Types, the control room, and the
/// picture of each code block of the Examples page. A block's picture is drawn from the block's own code, kept the same
/// as the wiki's. The sample program writes each to the repository's <c>images</c> folder, and the wiki keeps a copy of
/// each: <c>WikiPicturesTests</c> fails when a copy is older than the picture drawn here.
/// </summary>
internal static class WikiPictures
{
    /// <summary>Draws every picture and hands each to <paramref name="save"/>, with the name its file carries.</summary>
    /// <param name="save">What to do with a picture, given its name (without extension) and the figure.</param>
    public static void SaveAll(Action<string, Figure> save)
    {
        foreach (var picture in All())
        {
            save(picture.Name, picture.Figure);
        }
    }

    private readonly record struct WikiPicture(string Name, Figure Figure);

    private static IEnumerable<WikiPicture> All()
    {
        yield return new("survey_words", SurveyWords());
        yield return new("region_chord", RegionChord());
        yield return new("soil_ternary", SoilTernary());
        yield return new("piper_water", PiperWater());
        yield return new("linked_orders", LinkedOrders());
        yield return new("streaming_obv", StreamingObv());
        yield return new("rolling_transfer_entropy", RollingTransferEntropy());
        yield return new("anomaly_horizon", AnomalyHorizon());
        yield return new("arc_diagram", ArcDiagram());
        yield return new("budget_icicle", BudgetIcicle());
        yield return new("budget_waterfall", BudgetWaterfall());
        yield return new("calendar_events", CalendarEventsPicture());
        yield return new("connected_scatter", ConnectedScatterPicture());
        yield return new("fidelity_waterfall", FidelityWaterfall());
        yield return new("fill_betweenx_speed", FillBetweenXSpeed());
        yield return new("gradient_fills", GradientFills());
        yield return new("heating_waffle", HeatingWaffle());
        yield return new("kagi_closes", KagiCloses());
        yield return new("matshow_correlation", MatShowCorrelation());
        yield return new("point_and_figure_closes", PointAndFigureCloses());
        yield return new("range_frame", RangeFrame());
        yield return new("renko_closes", RenkoCloses());
        yield return new("stem_and_leaf_ages", StemAndLeafAges());
        yield return new("axis_colors", AxisColors());
        yield return new("band_spy", BandSpy());
        yield return new("cash_waterfall", CashWaterfall());
        yield return new("catalog_treemap_roundtrip", CatalogTreemapRoundtrip());
        yield return new("commits_calendar", CommitsCalendar());
        yield return new("control_room", ControlRoom());
        yield return new("defects_pareto", DefectsPareto());
        yield return new("depth_scatter3d_roundtrip", DepthScatter3dRoundtrip());
        yield return new("fill_control_chart", FillControlChart());
        yield return new("five_y_axes", FiveYAxes());
        yield return new("four_axes", FourAxes());
        yield return new("gallery_box", GalleryBox());
        yield return new("gallery_hexbin", GalleryHexbin());
        yield return new("gallery_kde", GalleryKde());
        yield return new("gallery_pie", GalleryPie());
        yield return new("gallery_polar_scatter", GalleryPolarScatter());
        yield return new("gallery_radar", GalleryRadar());
        yield return new("gallery_sankey", GallerySankey());
        yield return new("gallery_stem", GalleryStem());
        yield return new("gallery_streamplot", GalleryStreamplot());
        yield return new("gallery_treemap", GalleryTreemap());
        yield return new("gallery_violin", GalleryViolin());
        yield return new("gallery_waterfall", GalleryWaterfall());
        yield return new("genres_stream_graph", GenresStreamGraph());
        yield return new("highs_ridgeline", HighsRidgeline());
        yield return new("iris_parallel", IrisParallel());
        yield return new("languages_dot_plot", LanguagesDotPlot());
        yield return new("league_bump", LeagueBump());
        yield return new("load_threshold", LoadThreshold());
        yield return new("log_right_axis", LogRightAxis());
        yield return new("market_marimekko", MarketMarimekko());
        yield return new("mirrored_ticks", MirroredTicks());
        yield return new("regions_slope", RegionsSlope());
        yield return new("relapse_kaplan_meier", RelapseKaplanMeier());
        yield return new("relapse_kaplan_meier_band", RelapseKaplanMeierBand());
        yield return new("returns_qq", ReturnsQq());
        yield return new("right_ticks", RightTicks());
        yield return new("routes_raincloud", RoutesRaincloud());
        yield return new("satisfaction_dumbbell", SatisfactionDumbbell());
        yield return new("scores_lollipop", ScoresLollipop());
        yield return new("sensor_gap", SensorGap());
        yield return new("signal_streaming_roundtrip", SignalStreamingRoundtrip());
        yield return new("tickets_horizontal", TicketsHorizontal());
        yield return new("top_axis", TopAxis());
        yield return new("upload_log_bars", UploadLogBars());
        yield return new("williams_r_ohlcv_bars", WilliamsROhlcvBars());
    }

    // The gallery tiles of Chart-Types: one small figure for each chart type, drawn at the default size.
    private static Figure GalleryBox()
    {
        var random = new Random(7);
        double[][] groups = [.. Enumerable.Range(0, 4).Select(group => Normal(random, 60, group, 1))];

        return Plt.Create()
            .WithTitle("Box plot — 4 groups")
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(groups).SetXLabel("group").SetYLabel("value"))
            .Build();
    }

    private static Figure GalleryHexbin()
    {
        var random = new Random(11);

        return Plt.Create()
            .WithTitle("Hexbin — 2000 samples")
            .AddSubPlot(1, 1, 1, ax => ax.Hexbin(Normal(random, 2000, 0, 1), Normal(random, 2000, 0, 1)))
            .Build();
    }

    private static Figure GalleryKde() =>
        Plt.Create()
            .WithTitle("KDE — 500 normal samples")
            .AddSubPlot(1, 1, 1, ax => ax.Kde(Normal(new Random(3), 500, 0, 1)))
            .Build();

    private static Figure GalleryPie() =>
        Plt.Create()
            .WithTitle("Pie chart")
            .AddSubPlot(1, 1, 1, ax => ax.Pie([35, 25, 20, 15, 5], ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"], s => s.AutoPct = "%.1f%%"))
            .Build();

    private static Figure GalleryPolarScatter()
    {
        var random = new Random(5);
        double[] radius = [.. Enumerable.Range(0, 50).Select(_ => random.NextDouble())];
        double[] angle = [.. Enumerable.Range(0, 50).Select(_ => random.NextDouble() * 2 * Math.PI)];

        return Plt.Create()
            .WithTitle("Polar scatter")
            .AddSubPlot(1, 1, 1, ax => ax.PolarScatter(radius, angle))
            .Build();
    }

    private static Figure GalleryRadar() =>
        Plt.Create()
            .WithTitle("Radar — 5 axes")
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["Speed", "Power", "Range", "Accuracy", "Cost"], [8.5, 7, 7, 10, 5.5]))
            .Build();

    private static Figure GallerySankey()
    {
        SankeyNode[] nodes = [new("In1"), new("In2"), new("First"), new("Second"), new("Third"), new("Fourth"), new("Fifth")];
        SankeyLink[] links =
        [
            new(0, 2, 10), new(0, 3, 40), new(0, 4, 10),
            new(1, 3, 20), new(1, 4, 15), new(1, 5, 25), new(1, 6, 30),
        ];

        return Plt.Create()
            .WithTitle("Sankey diagram")
            .AddSubPlot(1, 1, 1, ax => ax.HideAllAxes().Sankey(nodes, links))
            .Build();
    }

    private static Figure GalleryStem()
    {
        double[] x = [.. Enumerable.Range(0, 20).Select(i => i * 2 * Math.PI / 19)];

        return Plt.Create()
            .WithTitle("Stem — sin(x)")
            .AddSubPlot(1, 1, 1, ax => ax.Stem(x, [.. x.Select(Math.Sin)]))
            .Build();
    }

    // The field of the first streamplot example of matplotlib's own documentation, so the tile can be put beside it.
    private static Figure GalleryStreamplot()
    {
        double[] axis = [.. Enumerable.Range(0, 30).Select(i => -3 + 6.0 * i / 29)];
        var u = new double[axis.Length, axis.Length];
        var v = new double[axis.Length, axis.Length];

        for (var row = 0; row < axis.Length; row++)
        {
            for (var column = 0; column < axis.Length; column++)
            {
                double x = axis[column], y = axis[row];
                u[row, column] = -1 - x * x + y;
                v[row, column] = 1 + x - y * y;
            }
        }

        return Plt.Create()
            .WithTitle("Streamplot")
            .AddSubPlot(1, 1, 1, ax => ax.Streamplot(axis, axis, u, v))
            .Build();
    }

    private static Figure GalleryTreemap() =>
        Plt.Create()
            .WithTitle("Treemap")
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(new TreeNode
            {
                Label = "all",
                Children =
                [
                    new() { Label = "A", Value = 32 }, new() { Label = "B", Value = 20 }, new() { Label = "C", Value = 15 },
                    new() { Label = "D", Value = 10 }, new() { Label = "E", Value = 8 }, new() { Label = "F", Value = 6 },
                    new() { Label = "G", Value = 5 },
                ],
            }, s => s.ShowLabels = true))
            .Build();

    private static Figure GalleryViolin()
    {
        var random = new Random(13);
        double[][] groups = [.. Enumerable.Range(0, 3).Select(group => Normal(random, 200, group * 2, 1))];

        return Plt.Create()
            .WithTitle("Violin plot — 3 groups")
            .AddSubPlot(1, 1, 1, ax => ax.Violin(groups).SetXLabel("group").SetYLabel("value"))
            .Build();
    }

    private static Figure GalleryWaterfall() =>
        Plt.Create()
            .WithTitle("Waterfall chart")
            .AddSubPlot(1, 1, 1, ax => ax.Waterfall(["Start", "Sales", "Costs", "Bonus", "Tax"], [100, 30, -20, 40, -10]))
            .Build();

    // The control-room block of Chart-Types, on a fixed clock so the picture is the same every time it is drawn.
    private static Figure ControlRoom()
    {
        var now = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        double At(int secondsAgo) => now.AddSeconds(-secondsAgo).ToOADate();

        double[] clock = [.. Enumerable.Range(0, 300).Select(i => At(300 - i))];
        double[] latency = [.. Enumerable.Range(0, 300).Select(i => 180 + (i % 40) * 3.0)];
        double[] decideHistory = [4.1, 3.9, 4.0, 3.7, 3.8, 3.6, 3.8];
        double[] latencyTail = [240, 251, 262, 270, 288, 301, 312];
        StateSegment[] services =
        [
            new(At(300), At(200), "up", Colors.Green),
            new(At(200), At(140), "degraded", Colors.Orange),
            new(At(140), At(90), "no data", Colors.Gray),
            new(At(90), At(0), "up", Colors.Green),
        ];

        return Plt.OpsDashboard()
            .WithTitle("Fleet — control room")
            .WithWindow(now, TimeSpan.FromMinutes(5))
            .WithNormalBand(0, 250)
            .AddTile(9, t => { t.Label = "Processes"; t.Format = "0"; t.Caption = "of 9 · 25 lanes"; })
            .AddTile(3.8, t =>
            {
                t.Label = "Relay (µs/msg)";
                t.Format = "0.0";
                t.Target = 6;
                t.Caption = "threshold 6" + Environment.NewLine + "2 148 msg · 1 s";
                t.Trend = decideHistory;
            })
            .AddTile(312, t =>
            {
                t.Label = "Bus latency (µs)";
                t.Format = "0";
                t.Target = 250;
                t.Caption = "threshold 250 · +62" + Environment.NewLine + "2 148 msg · 1 s";
                t.Trend = latencyTail;
                t.AccentColor = Theme.OpsNight.Alarm.Warning;
            })
            .AddTile(0, t => { t.Label = "Sent offline (msg)"; t.Format = "0"; t.Target = 0; t.Caption = "threshold 0"; })
            .AddTile(0, t =>
            {
                t.Label = "Price feed";
                t.Format = "0.0;-0.0;—";
                t.Caption = "no signal";
                t.Hatch = HatchPattern.ForwardDiagonal;
            })
            .AddTimeline(services, l => l.Label = "Bus")
            .AddTrend(clock, latency, s => s.Label = "latency µs")
            .Build()
            .WithTheme(Theme.OpsNight)
            .Build();
    }

    // A reproducible normal sample (Box–Muller), so a picture is the same every time it is drawn.
    private static double[] Normal(Random random, int count, double mean, double deviation) =>
        [.. Enumerable.Range(0, count).Select(_ =>
            mean + deviation * Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble()))];

    private static Figure AnomalyHorizon()
    {
        double[] day = [.. Enumerable.Range(0, 60).Select(i => (double)i)];
        double[] anomaly = [.. day.Select(d => 40 * Math.Sin(d / 5.0))];

        var figure = Plt.Create()
            .WithSize(700, 160)
            .WithTitle("Temperature anomaly")
            .AddSubPlot(1, 1, 1, ax => ax
                .Horizon(day, anomaly, bands: 4)
                .SetXLabel("Day"))
            .Build();
        return figure;
    }

    private static Figure AxisColors()
    {
        double[] hour = [0, 4, 8, 12, 16, 20, 24];
        double[] temperature = [12, 10, 14, 21, 23, 17, 13];
        double[] humidity = [88, 92, 75, 52, 47, 63, 81];

        var figure = Plt.Create()
            .WithSize(700, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, temperature, s => { s.Label = "Temperature"; s.Color = Colors.Crimson; }).WithDirectLabel()
                .SetYLabel("Temperature (°C)")
                .SetYAxisColor(Colors.Crimson)
                .SetXLabel("Hour of the day")
                .WithAddRightYAxis(right => right
                    .Plot(hour, humidity, s => { s.Label = "Humidity"; s.Color = Colors.SteelBlue; })
                    .WithDirectLabel()
                    .SetYLabel("Relative humidity (%)")
                    .SetYAxisColor(Colors.SteelBlue)))
            .TightLayout()
            .Build();

        return figure;
    }

    private static Figure BandSpy()
    {
        // A banded matrix: the diagonal and the two diagonals beside it.
        var band = new double[20, 20];
        for (int r = 0; r < 20; r++)
        {
            for (int c = Math.Max(0, r - 1); c <= Math.Min(19, r + 1); c++)
            {
                band[r, c] = 1;
            }
        }

        var figure = Plt.Create()
            .WithSize(500, 500)
            .WithTitle("A tridiagonal matrix")
            .AddSubPlot(1, 1, 1, ax => ax.Spy(band, configure: s => s.MarkerSize = 12))
            .Build();
        return figure;
    }

    private static Figure CashWaterfall()
    {
        string[] steps = ["Opening", "Sales", "Refunds", "Costs", "Closing"];
        double[] amounts = [500, 320, -40, -180, 600];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .AddSubPlot(1, 1, 1, ax => ax
                .Waterfall(steps, amounts, s => s.TotalIndices = [0, 4])
                .SetYLabel("Cash (k€)"))
            .Build();
        return figure;
    }

    private static Figure CatalogTreemapRoundtrip()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new TreeNode
                {
                    Label = "Fruit",
                    Value = 60,
                    Children =
                    [
                        new TreeNode { Label = "Apples", Value = 35 },
                        new TreeNode { Label = "Pears", Value = 25 },
                    ],
                },
                new TreeNode { Label = "Vegetables", Value = 40 },
            ],
        };

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root))
            .Build();

        return figure;
    }

    private static Figure CommitsCalendar()
    {
        var first = new DateTime(2025, 1, 1);
        DateTime[] days = [.. Enumerable.Range(0, 365).Select(d => first.AddDays(d))];
        double[] commits = [.. days.Select(d => (double)(d.DayOfYear * 37 % 11))];

        var figure = Plt.Create()
            .WithSize(1100, 300)
            .WithTitle("Commits in 2025")
            .AddSubPlot(1, 1, 1, ax => ax
                .CalendarHeatmap(days, commits, configure: s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        return figure;
    }

    private static Figure DefectsPareto()
    {
        string[] defects = ["Dents", "Scratches", "Stains", "Cracks", "Warp"];
        double[] counts = [98, 142, 45, 67, 9];

        var figure = Plt.Create()
            .WithSize(700, 420)
            .WithTitle("Defects by kind")
            .AddSubPlot(1, 1, 1, ax => ax
                .Pareto(defects, counts, parts => parts.Bars.Label = "Defects")
                .SetXLabel("Kind of defect"))
            .Build();
        return figure;
    }

    private static Figure DepthScatter3dRoundtrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter3D([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], [7.0, 8.0, 9.0])
                .SetZLabel("Depth")
                .WithPane3D(p => p with { FloorColor = Colors.Black })
                .WithTooltips())
            .Build();

        return figure;
    }

    private static Figure FillControlChart()
    {
        double[] day = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] fill = [50.2, 49.8, 50.1, 50.4, 49.9, 50.0, 53.9, 50.3, 49.7, 50.1];

        var figure = Plt.Create()
            .WithSize(650, 400)
            .WithTitle("Fill weight")
            .AddSubPlot(1, 1, 1, ax => ax
                .ControlChart(day, fill)
                .SetXLabel("Day")
                .SetYLabel("Weight (g)"))
            .Build();
        return figure;
    }

    private static Figure FiveYAxes()
    {
        double[] hour = [0, 4, 8, 12, 16, 20, 24];
        double[] requests = [120, 80, 340, 610, 560, 380, 150];
        double[] cpu = [18, 12, 46, 82, 74, 51, 22];
        double[] memory = [3.1, 3.0, 4.2, 6.8, 6.4, 4.9, 3.5];
        double[] queue = [0, 0, 4, 31, 22, 6, 1];
        double[] latency = [42, 40, 55, 130, 118, 71, 48];

        var figure = Plt.Create()
            .WithSize(1000, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, requests, s => { s.Label = "Requests"; s.Color = Colors.Crimson; })
                .SetXLabel("Hour of the day")
                .SetYLabel("Requests (per second)")
                .SetYAxisColor(Colors.Crimson)
                .WithAddRightYAxis(right => right
                    .Plot(hour, cpu, s => { s.Label = "CPU"; s.Color = Colors.SteelBlue; })
                    .SetYLabel("CPU (%)")
                    .SetYAxisColor(Colors.SteelBlue))
                .WithAddRightYAxis(right => right
                    .Plot(hour, memory, s => { s.Label = "Memory"; s.Color = Colors.ForestGreen; })
                    .SetYLabel("Memory (GB)")
                    .SetYAxisColor(Colors.ForestGreen))
                .WithAddLeftYAxis(left => left
                    .Plot(hour, queue, s => { s.Label = "Queue"; s.Color = Css4Colors.DarkOrange; })
                    .SetYLabel("Waiting jobs")
                    .SetYAxisColor(Css4Colors.DarkOrange))
                .WithAddLeftYAxis(left => left
                    .Plot(hour, latency, s => { s.Label = "Latency"; s.Color = Colors.RebeccaPurple; })
                    .SetYLabel("Latency (ms)")
                    .SetYAxisColor(Colors.RebeccaPurple))
                .WithYAxisTitleGap(8)
                .WithLegend())
            .TightLayout()
            .Build();

        return figure;
    }

    private static Figure FourAxes()
    {
        double[] hour = [0, 4, 8, 12, 16, 20, 24];
        double[] temperature = [12, 10, 14, 21, 23, 17, 13];
        double[] humidity    = [88, 92, 75, 52, 47, 63, 81];
        double[] windSpeed   = [8, 6, 9, 14, 18, 11, 7];
        double[] pressure    = [1013, 1015, 1012, 1008, 1006, 1010, 1014];

        var figure = Plt.Create()
            .WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, temperature, s => { s.Label = "Temperature"; s.Color = Colors.Crimson; })
                .SetYLabel("Temperature (°C)")
                .SetYAxisColor(Colors.Crimson)
                .SetXLabel("Hour of the day")
                .WithAddRightYAxis(right => right
                    .Plot(hour, humidity, s => { s.Label = "Humidity"; s.Color = Colors.SteelBlue; })
                    .SetYLabel("Relative humidity (%)")
                    .SetYAxisColor(Colors.SteelBlue))
                .WithAddRightYAxis(outer => outer
                    .Plot(hour, windSpeed, s => { s.Label = "Wind speed"; s.Color = Colors.ForestGreen; })
                    .SetYLabel("Wind speed (km/h)"))
                .WithAddLeftYAxis(left => left
                    .Plot(hour, pressure, s => { s.Label = "Pressure"; s.Color = Css4Colors.DarkOrange; })
                    .SetYLabel("Pressure (hPa)"))
                .WithLegend())
            .TightLayout()
            .Build();
        return figure;
    }

    private static Figure GenresStreamGraph()
    {
        double[] year = [2019, 2020, 2021, 2022, 2023];

        var figure = Plt.Create()
            .WithSize(700, 400)
            .WithTitle("Listening by genre")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .StreamGraph(year,
                [
                    new BarGroup("Rock", [8, 7, 6, 5, 4]),
                    new BarGroup("Pop", [3, 5, 7, 8, 9]),
                    new BarGroup("Jazz", [2, 2, 3, 3, 4]),
                ])
                .SetXTickLocator(new MultipleLocator(1))
                .SetXLabel("Year")
                .WithLegend(LegendPosition.OutsideRight))
            .Build();
        return figure;
    }

    private static Figure HighsRidgeline()
    {
        NamedSample[] months =
        [
            new("January", [2.1, 3.4, 1.8, 0.9, 2.6, 3.0, 1.2]),
            new("April", [9.8, 12.1, 11.4, 8.7, 10.9, 13.2]),
            new("July", [21.5, 24.0, 19.8, 23.1, 22.7, 25.3, 20.4, 22.0]),
        ];

        var figure = Plt.Create()
            .WithSize(600, 400)
            .WithTitle("Daily highs")
            .AddSubPlot(1, 1, 1, ax => ax
                .Ridgeline(months, configure: ridge => ridge.Alpha = 0.6)
                .SetXLabel("Temperature (°C)"))
            .Build();
        return figure;
    }

    private static Figure IrisParallel()
    {
        string[] measures = ["Sepal length", "Sepal width", "Petal length", "Petal width"];

        var figure = Plt.Create()
            .WithSize(760, 400)
            .WithTitle("Three species of iris")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .ParallelCoordinates(measures,
                [
                    new BarGroup("setosa", [5.1, 3.5, 1.4, 0.2]),
                    new BarGroup("setosa", [4.9, 3.0, 1.4, 0.2]),
                    new BarGroup("setosa", [4.7, 3.2, 1.3, 0.2]),
                    new BarGroup("versicolor", [7.0, 3.2, 4.7, 1.4]),
                    new BarGroup("versicolor", [6.4, 3.2, 4.5, 1.5]),
                    new BarGroup("versicolor", [5.5, 2.3, 4.0, 1.3]),
                    new BarGroup("virginica", [6.3, 3.3, 6.0, 2.5]),
                    new BarGroup("virginica", [5.8, 2.7, 5.1, 1.9]),
                    new BarGroup("virginica", [7.1, 3.0, 5.9, 2.1]),
                ])
                .WithLegend(LegendPosition.OutsideRight))
            .Build();
        return figure;
    }

    private static Figure LanguagesDotPlot()
    {
        string[] languages = ["Ruby", "Go", "Rust", "Java", "Python"];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .WithTitle("Programming languages")
            .AddSubPlot(1, 1, 1, ax => ax
                .DotPlot(languages,
                [
                    new BarGroup("2015", [31.0, 48.0, 62.0, 55.0, 80.0]),
                    new BarGroup("2025", [35.0, 52.0, 60.0, 58.0, 84.0]),
                ])
                .SetXLabel("Share of developers (%)"))
            .Build();
        return figure;
    }

    private static Figure LeagueBump()
    {
        double[] seasons = [2021, 2022, 2023, 2024];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .WithTitle("League table")
            .AddSubPlot(1, 1, 1, ax => ax
                .BumpChart(seasons,
                [
                    new BarGroup("Hawks", [1, 2, 3, 3]),
                    new BarGroup("Owls", [2, 1, 1, 2]),
                    new BarGroup("Wrens", [3, 3, 2, 1]),
                ])
                .SetXTickLocator(new MultipleLocator(1))
                .SetXLabel("Season")
                .SetYLabel("Rank"))
            .Build();
        return figure;
    }

    private static Figure LoadThreshold()
    {
        double[] hour = [0, 1, 2, 3, 4, 5, 6, 7];
        double[] load = [42, 55, 61, 68, 64, 59, 52, 47];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, load, s => s.Label = "CPU load %")
                .Threshold(80.0, Orientation.Horizontal, ThresholdBreach.Above, color: Colors.Red, label: "Alarm")
                .WithLegend(LegendPosition.LowerRight)
                .SetXLabel("Hour")
                .SetYLabel("Load %"))
            .Build();
        return figure;
    }

    private static Figure LogRightAxis()
    {
        double[] day = [1, 2, 3, 4, 5, 6, 7];
        double[] visitors = [120, 135, 128, 150, 162, 171, 158];
        double[] signups = [3, 12, 40, 150, 610, 2300, 9100];

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(day, visitors, s => s.Color = Colors.Crimson)
                .SetYLabel("Visitors")
                .SetXLabel("Day")
                .WithAddRightYAxis(right => right
                    .Plot(day, signups, s => s.Color = Colors.SteelBlue)
                    .SetYLabel("Sign-ups")))
            .Build();

        figure.SubPlots[0].YAxes.Right[0].Axis.Scale = AxisScale.Log;

        return figure;
    }

    private static Figure MarketMarimekko()
    {
        string[] channels = ["Retail", "Online", "Wholesale"];

        var figure = Plt.Create()
            .WithSize(700, 420)
            .WithTitle("Market by channel and brand")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Marimekko(channels,
                [
                    new BarGroup("Brand A", [30, 15, 5]),
                    new BarGroup("Brand B", [20, 25, 10]),
                    new BarGroup("Brand C", [10, 20, 15]),
                ])
                .SetXLabel("Channel (width: its share of the market)")
                .WithLegend(LegendPosition.OutsideRight))
            .Build();
        return figure;
    }

    private static Figure MirroredTicks()
    {
        double[] depth = [0, 50, 100, 150, 200, 250];
        double[] pressure = [101325, 605000, 1110000, 1615000, 2120000, 2625000];

        var figure = Plt.Create()
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(depth, pressure)
                .WithTitle("Pressure under water")
                .SetXLabel("Depth (m)")
                .SetYLabel("Pressure (Pa)")
                .WithXTicksMirrored()
                .WithYTicksMirrored())
            .TightLayout()
            .Build();
        return figure;
    }

    private static Figure RegionsSlope()
    {
        string[] regions = ["North", "South", "East", "West"];

        var figure = Plt.Create()
            .WithSize(600, 420)
            .WithTitle("Sales by region")
            .AddSubPlot(1, 1, 1, ax => ax
                .SlopeGraph(regions,
                    new BarGroup("2015", [42.0, 55.0, 38.0, 61.0]),
                    new BarGroup("2025", [51.0, 49.0, 44.0, 66.0]))
                .SetYLabel("Sales"))
            .Build();
        return figure;
    }

    private static Figure RelapseKaplanMeier()
    {
        SurvivalGroup[] arms =
        [
            new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
            new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
        ];

        var figure = Plt.Create()
            .WithSize(600, 400)
            .WithTitle("Time to relapse")
            .AddSubPlot(1, 1, 1, ax => ax
                .KaplanMeier(arms)
                .SetXLabel("Weeks"))
            .Build();
        return figure;
    }

    private static Figure RelapseKaplanMeierBand()
    {
        SurvivalGroup[] arms =
        [
            new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
            new("Placebo", [1, 2, 3, 4, 5, 8, 8, 11], [true, true, true, false, true, true, true, true]),
        ];

        var figure = Plt.Create()
            .WithSize(600, 400)
            .WithTitle("Time to relapse")
            .AddSubPlot(1, 1, 1, ax => ax
                .KaplanMeier(arms)
                .SetXLabel("Weeks"))
            .Build();
        return figure;
    }

    private static Figure ReturnsQq()
    {
        double[] returns = [0.8, -1.2, 0.3, 2.1, -0.4, 0.9, -2.6, 0.1, 1.4, -0.7, 0.5, -0.2];

        var figure = Plt.Create()
            .WithSize(600, 420)
            .WithTitle("Daily returns against the normal distribution")
            .AddSubPlot(1, 1, 1, ax => ax
                .QQPlot(returns)
                .SetYLabel("Daily return (%)"))
            .Build();
        return figure;
    }

    private static Figure RightTicks()
    {
        double[] hour = [0, 4, 8, 12, 16, 20, 24];
        double[] load = [310, 280, 420, 560, 610, 480, 330];
        double[] price = [0.21, 0.18, 0.27, 0.34, 0.38, 0.30, 0.22];

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, load)
                .SetYLabel("Load (MW)")
                .WithAddRightYAxis(right => right
                    .Plot(hour, price, s => s.Color = Colors.Tab10Orange)
                    .SetYLabel("Price (EUR/kWh)")))
            .Build();

        var right = figure.SubPlots[0].YAxes.Right[0].Axis;
        right.MajorTicks = right.MajorTicks with { Direction = TickDirection.In, Length = 8, LabelSize = 12, LabelColor = Colors.Navy };

        return figure;
    }

    private static Figure RoutesRaincloud()
    {
        NamedSample[] routes =
        [
            new("Highway", [22, 24, 23, 26, 25, 28, 24, 23, 27, 25, 30, 26]),
            new("Backroads", [18, 29, 15, 35, 20, 32, 17, 24, 21, 30, 16, 27]),
        ];

        var figure = Plt.Create()
            .WithSize(600, 400)
            .WithTitle("Commute time by route")
            .AddSubPlot(1, 1, 1, ax => ax
                .Raincloud(routes)
                .SetYLabel("Minutes"))
            .Build();
        return figure;
    }

    private static Figure SatisfactionDumbbell()
    {
        string[] products = ["A", "B", "C", "D"];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .AddSubPlot(1, 1, 1, ax => ax
                .Dumbbell(products,
                    new BarGroup("2024", [62.0, 78.5, 83.0, 55.0]),
                    new BarGroup("2025", [74.0, 82.0, 79.5, 48.0]))
                .SetYLabel("Satisfaction (%)")
                .WithLegend(LegendPosition.LowerLeft))
            .Build();
        return figure;
    }

    private static Figure ScoresLollipop()
    {
        string[] products = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];
        double[] scores = [82.5, 67.3, 91.1, 54.8, 73.0];

        var figure = Plt.Create()
            .WithSize(600, 380)
            .AddSubPlot(1, 1, 1, ax => ax
                .Lollipop(products, scores, s => s.StemColor = Colors.SteelBlue)
                .SetYLabel("Score"))
            .Build();
        return figure;
    }

    private static Figure SensorGap()
    {
        double[] hour = [0, 1, 2, 3, 4, 5, 6, 7];
        double[] reading = [12.1, 12.4, double.NaN, double.NaN, 13.0, 13.4, 13.1, 12.8];

        var figure = Plt.Create()
            .WithSize(600, 350)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(hour, reading, s => s.Marker = MarkerStyle.Circle)
                .SetXLabel("Hour")
                .SetYLabel("Reading"))
            .Build();
        return figure;
    }

    private static Figure SignalStreamingRoundtrip()
    {
        StreamingLineSeries? series = null;

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => { series = ax.StreamingPlot(capacity: 500); })
            .Build();

        for (var i = 0; i < 20; i++)
        {
            series!.AppendPoint(i, Math.Sin(i * 0.3));
        }

        return figure;
    }

    private static Figure TicketsHorizontal()
    {
        string[] teams = ["Alpha", "Bravo", "Charlie", "Delta"];
        double[] closed = [12500, 8200, 15100, 6400];

        var figure = Plt.Create()
            .WithSize(600, 320)
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(teams, closed, s => s.Orientation = BarOrientation.Horizontal)
                .WithBarLabels()
                .SetXLim(0, 18000)
                .SetXLabel("Tickets closed"))
            .Build();
        return figure;
    }

    private static Figure TopAxis()
    {
        double[] celsius = [-10, 0, 10, 20, 30, 40];
        double[] fahrenheit = [14, 32, 50, 68, 86, 104];
        double[] efficiency = [71, 80, 88, 93, 90, 82];

        var figure = Plt.Create()
            .WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(celsius, efficiency, s => s.Color = Colors.Teal)
                .SetXLabel("Outdoor temperature (°C)")
                .SetXAxisColor(Colors.Teal)
                .SetYLabel("Efficiency (%)")
                .WithTitle("Heat pump efficiency")
                .WithSecondaryXAxis(top => top
                    .ScatterXSecondary(fahrenheit, efficiency, s => s.Color = Colors.Chocolate)
                    .SetXLabel("Outdoor temperature (°F)")
                    .SetXAxisColor(Colors.Chocolate)))
            .Build();
        return figure;
    }

    private static Figure UploadLogBars()
    {
        string[] payload = ["1 MB", "10 MB", "100 MB", "1 GB"];
        double[] milliseconds = [15, 120, 1300, 9800];

        var figure = Plt.Create()
            .WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(payload, milliseconds)
                .SetYScale(AxisScale.Log)
                .SetYLabel("Upload time (ms)"))
            .Build();
        return figure;
    }

    private static Figure WilliamsROhlcvBars()
    {
        double[] close = [.. Enumerable.Range(0, 60).Select(i => 100 + 5 * Math.Sin(i / 6.0) + i * 0.2)];
        double[] open  = [.. close.Select((c, i) => i == 0 ? c : close[i - 1])];
        double[] high  = [.. close.Select((c, i) => Math.Max(c, open[i]) + 1)];
        double[] low   = [.. close.Select((c, i) => Math.Min(c, open[i]) - 1)];

        var figure = Plt.Create()
           .WithSize(800, 500)
           .AddSubPlot(2, 1, 1, ax => ax.Candlestick(open, high, low, close))
           .AddSubPlot(2, 1, 2, ax => ax.WilliamsR(OhlcvBars.Hlc(high, low, close)))
           .TightLayout()
           .Build();
        return figure;
    }

    /// <summary>The closing prices the price charts of this release draw: thirty closes that rise, fall back and rise again.</summary>
    private static readonly double[] Closes =
        [100, 102, 105, 103, 108, 112, 109, 107, 111, 115, 118, 114, 110, 106, 103, 107, 111, 116, 121, 119, 115, 112, 117, 123, 128, 125, 121, 118, 122, 127];

    private static Figure HeatingWaffle()
    {
        var figure = Plt.Create()
            .WithTitle("Households by heating")
            .WithSize(620, 420)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Waffle(["Gas", "Electric", "Heat pump", "Other"], [46, 31, 17, 6])
                .WithLegend(LegendPosition.OutsideRight))
            .Build();
        return figure;
    }

    private static Figure CalendarEventsPicture()
    {
        DateTime[] days =
        [
            new(2025, 3, 3), new(2025, 3, 3), new(2025, 3, 12), new(2025, 3, 19), new(2025, 4, 1),
            new(2025, 4, 1), new(2025, 4, 9), new(2025, 4, 22), new(2025, 5, 6),
        ];
        string[] kinds = ["deploy", "incident", "deploy", "deploy", "deploy", "incident", "incident", "deploy", "deploy"];

        var figure = Plt.Create()
            .WithTitle("Releases and incidents")
            .WithSize(900, 300)
            .AddSubPlot(1, 1, 1, ax => ax
                .CalendarEvents(days, kinds)
                .WithLegend())
            .Build();
        return figure;
    }

    private static Figure PointAndFigureCloses()
    {
        var figure = Plt.Create()
            .WithTitle("Point and Figure, boxes of 2")
            .WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .PointAndFigure(Closes, 2)
                .SetAspect(1.0 / 2))
            .Build();
        return figure;
    }

    private static Figure RenkoCloses()
    {
        var figure = Plt.Create()
            .WithTitle("Renko, bricks of 3")
            .WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .Renko(Closes, 3)
                .WithLegend())
            .Build();
        return figure;
    }

    private static Figure KagiCloses()
    {
        var figure = Plt.Create()
            .WithTitle("Kagi, a reversal of 4")
            .WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Kagi(Closes, 4))
            .Build();
        return figure;
    }

    private static Figure GradientFills()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6];
        double[] y = [2, 5, 4, 8, 6, 9, 7];

        var figure = Plt.Create()
            .WithTitle("Fills that fade")
            .WithSize(860, 380)
            .AddSubPlot(1, 2, 1, ax => ax
                .Bar(["Q1", "Q2", "Q3", "Q4"], [4, 7, -3, 9], s =>
                {
                    s.Color = Colors.Tab10Blue;
                    s.GradientColor = Color.FromHex("#C6DBEF");
                }))
            .AddSubPlot(1, 2, 2, ax => ax
                .FillBetween(x, y, null, s =>
                {
                    s.Color = Colors.Tab10Orange;
                    s.Alpha = 1;
                    s.GradientColor = Colors.Tab10Orange.WithAlpha(0);
                }))
            .Build();
        return figure;
    }

    private static Figure ConnectedScatterPicture()
    {
        double[] unemployment = [4.0, 4.4, 5.8, 7.7, 9.3, 9.6, 8.9];
        double[] inflation = [2.4, 1.6, 2.7, 0.1, 2.7, 1.5, 3.0];

        var figure = Plt.Create()
            .WithTitle("Unemployment and inflation")
            .WithSize(700, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .ConnectedScatter(unemployment, inflation)
                .SetXLabel("Unemployment (%)")
                .SetYLabel("Inflation (%)"))
            .Build();
        return figure;
    }

    private static Figure RangeFrame()
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [3.1, 3.8, 4.2, 4.0, 5.1, 5.9, 5.6, 6.8, 7.2, 7.9];

        var figure = Plt.Create()
            .WithTitle("A range frame")
            .WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y)
                .WithSpines(spines => spines with
                {
                    Bottom = spines.Bottom with { BoundsFrom = 1, BoundsTo = 10 },
                    Left = spines.Left with { BoundsFrom = 3.1, BoundsTo = 7.9 },
                    Top = spines.Top with { Visible = false },
                    Right = spines.Right with { Visible = false },
                }))
            .Build();
        return figure;
    }

    private static Figure FillBetweenXSpeed()
    {
        double[] depth = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] speed = [2, 4, 3, 6, 5, 8, 6, 9, 7, 10, 8];

        var figure = Plt.Create()
            .WithTitle("Speed by depth")
            .WithSize(500, 450)
            .AddSubPlot(1, 1, 1, ax => ax
                .FillBetweenX(depth, speed, null, s => s.Alpha = 0.8)
                .SetXLabel("Speed")
                .SetYLabel("Depth"))
            .Build();
        return figure;
    }

    private static Figure MatShowCorrelation()
    {
        var correlation = new double[,]
        {
            { 1.0, 0.8, 0.1, -0.4 },
            { 0.8, 1.0, -0.3, -0.2 },
            { 0.1, -0.3, 1.0, 0.6 },
            { -0.4, -0.2, 0.6, 1.0 },
        };

        var figure = Plt.Create()
            .WithTitle("Correlation")
            .WithSize(520, 520)
            .AddSubPlot(1, 1, 1, ax => ax.MatShow(correlation))
            .Build();
        return figure;
    }

    private static Figure BudgetIcicle()
    {
        var budget = new TreeNode
        {
            Label = "Budget",
            Children =
            [
                new TreeNode
                {
                    Label = "Engineering",
                    Children =
                    [
                        new TreeNode { Label = "Platform", Value = 40 },
                        new TreeNode { Label = "Apps", Value = 25 },
                        new TreeNode { Label = "Data", Value = 15 },
                    ],
                },
                new TreeNode
                {
                    Label = "Sales",
                    Children = [new TreeNode { Label = "Direct", Value = 30 }, new TreeNode { Label = "Partners", Value = 20 }],
                },
                new TreeNode
                {
                    Label = "Operations",
                    Children = [new TreeNode { Label = "Support", Value = 18 }, new TreeNode { Label = "Finance", Value = 12 }],
                },
            ],
        };

        var figure = Plt.Create()
            .WithTitle("Where the budget goes")
            .WithSize(760, 360)
            .AddSubPlot(1, 1, 1, ax => ax.Icicle(budget))
            .Build();
        return figure;
    }

    private static Figure ArcDiagram()
    {
        GraphNode[] nodes = [new("Ada"), new("Ben"), new("Cleo"), new("Dev"), new("Eli"), new("Fay"), new("Gus")];
        GraphEdge[] edges =
        [
            new("Ada", "Dev"), new("Ada", "Gus"), new("Ben", "Cleo"), new("Ben", "Fay"),
            new("Cleo", "Dev"), new("Dev", "Eli"), new("Eli", "Gus"), new("Cleo", "Gus"),
        ];

        var figure = Plt.Create()
            .WithTitle("Who works with whom")
            .WithSize(800, 420)
            .AddSubPlot(1, 1, 1, ax => ax
                .NetworkGraph(nodes, edges, s => s.Layout = GraphLayout.Arc)
                .SetAspect(1)
                .HideAllAxes())
            .Build();
        return figure;
    }

    private static Figure StemAndLeafAges()
    {
        double[] ages = [12, 15, 21, 23, 23, 38, 41, 44, 47, 52, 53, 58, 61];

        var figure = Plt.Create()
            .WithTitle("Ages")
            .WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf(ages))
            .Build();
        return figure;
    }

    private static Figure BudgetWaterfall()
    {
        string[] steps = ["Opening", "Q1", "Q2", "Q3", "Q4", "Closing"];
        double[] amounts = [100, 30, -20, 40, -10, 140];

        var figure = Plt.Create()
            .WithTitle("Budget against target")
            .WithSize(640, 400)
            .AddSubPlot(1, 1, 1, ax => ax
                .Waterfall(steps, amounts, s =>
                {
                    s.TotalIndices = [0, 5];
                    s.Targets = [100, 125, 110, 150, 145, 150];
                })
                .SetYLabel("Budget (k€)"))
            .Build();
        return figure;
    }

    private static Figure FidelityWaterfall()
    {
        var figure = Plt.Create()
            .WithSize(800, 600)
            .WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Waterfall chart")
                .Waterfall(["Start", "A", "B", "C", "D", "End"], [100, 30, -20, 40, -10, 140], s =>
                {
                    s.TotalIndices = [0, 5];
                    s.IncreaseColor = Color.FromHex("#2CA02C");
                    s.DecreaseColor = Color.FromHex("#D62728");
                    s.TotalColor = Color.FromHex("#1F77B4");
                    s.BarWidth = 0.8;
                }))
            .Build();
        return figure;
    }

    private static Figure SurveyWords()
    {
        string[] words = ["price", "support", "speed", "design", "docs", "bugs", "export", "themes", "charts", "install", "license", "maps"];
        double[] weights = [42, 35, 30, 24, 22, 18, 16, 14, 13, 9, 7, 5];

        var figure = Plt.Create()
            .WithTitle("What the survey answers talk about")
            .WithSize(620, 420)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WordCloud(words, weights))
            .Build();
        return figure;
    }

    private static Figure RegionChord()
    {
        string[] regions = ["North", "South", "East", "West"];
        double[,] moves =
        {
            { 0, 12, 8, 3 },
            { 9, 0, 5, 7 },
            { 4, 6, 0, 10 },
            { 2, 8, 11, 0 },
        };

        var figure = Plt.Create()
            .WithTitle("People moving between regions")
            .WithSize(560, 560)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Chord(regions, moves))
            .Build();
        return figure;
    }

    private static Figure SoilTernary()
    {
        double[] clay = [0.55, 0.30, 0.12, 0.20, 0.40, 0.08, 0.28, 0.65];
        double[] silt = [0.25, 0.40, 0.18, 0.60, 0.15, 0.12, 0.22, 0.15];
        double[] sand = [0.20, 0.30, 0.70, 0.20, 0.45, 0.80, 0.50, 0.20];

        var figure = Plt.Create()
            .WithTitle("Soil samples by texture")
            .WithSize(620, 560)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetYLabel("Clay")
                .SetZLabel("Silt")
                .SetXLabel("Sand")
                .TernaryScatter(clay, silt, sand))
            .Build();
        return figure;
    }

    private static Figure PiperWater()
    {
        WaterSample[] waters =
        [
            new() { Label = "Mountain spring", Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 },
            new() { Label = "Brine well", Ca = 400, Mg = 100, Na = 5000, K = 50, HCO3 = 150, SO4 = 300, Cl = 8000 },
            new() { Label = "Sodium spring", Ca = 10, Mg = 3, Na = 300, K = 5, HCO3 = 600, SO4 = 30, Cl = 40 },
        ];

        var figure = Plt.Create()
            .WithTitle("Three waters on a Piper diagram")
            .WithSize(820, 560)
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Piper(waters))
            .Build();
        return figure;
    }

    private static Figure LinkedOrders()
    {
        string[] orders = ["o1", "o2", "o3", "o4", "o5", "o6"];
        double[] size = [12, 30, 18, 44, 26, 38];
        double[] days = [2, 5, 3, 9, 4, 7];

        var figure = Plt.Create()
            .WithTitle("The same six orders in two charts")
            .WithSize(860, 400)
            .TightLayout()
            .LinkSelection("orders", ["o2", "o5"])
            .AddSubPlot(1, 2, 1, ax => ax
                .WithTitle("Size and delivery time")
                .SetXLabel("Order size")
                .SetYLabel("Days to deliver")
                .Scatter(size, days, s => s.RowKeys = orders))
            .AddSubPlot(1, 2, 2, ax => ax
                .WithTitle("Size by order")
                .SetXLabel("Order number")
                .SetYLabel("Order size")
                .Scatter([1.0, 2, 3, 4, 5, 6], size, s => s.RowKeys = orders))
            .Build();
        return figure;
    }

    private static Figure StreamingObv()
    {
        var figure = new Figure { Title = "Candles with a streaming average and on-balance volume", Width = 760, Height = 520 };
        var price = figure.AddSubPlot(2, 1, 1);
        var volume = figure.AddSubPlot(2, 1, 2);
        var candles = price.AddSeries(new StreamingCandlestickSeries(200));
        candles.WithStreamingSma(price, 5);
        candles.WithStreamingObv(volume);

        double close = 100;
        ulong state = 7;
        for (int i = 0; i < 60; i++)
        {
            state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
            double step = ((state >> 11) / (double)(1UL << 53) - 0.5) * 6;
            state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
            double traded = 800 + (state >> 11) / (double)(1UL << 53) * 600;
            double open = close;
            close = Math.Max(1, close + step);
            candles.AppendBar(new OhlcvBar(open, Math.Max(open, close) + 1, Math.Min(open, close) - 1, close, traded));
        }

        return figure;
    }

    private static Figure RollingTransferEntropy()
    {
        const int n = 400;
        ulong state = 7;
        double Normal()
        {
            double sum = 0;
            for (int k = 0; k < 12; k++)
            {
                state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
                sum += (state >> 11) / (double)(1UL << 53);
            }

            return sum - 6;
        }

        double[] source = new double[n];
        double[] noise = new double[n];
        for (int t = 0; t < n; t++) source[t] = Normal();
        for (int t = 0; t < n; t++) noise[t] = Normal();
        double[] target = new double[n];
        target[0] = noise[0];
        for (int t = 0; t + 1 < n; t++) target[t + 1] = (t >= 200 ? 1 : 0) * source[t] + noise[t + 1];

        var figure = Plt.Create()
            .WithTitle("Does the source lead the target? It does from bar 200")
            .WithSize(760, 380)
            .AddSubPlot(1, 1, 1, ax => ax
                .SetXLabel("Bar")
                .SetYLabel("Transfer entropy (nats)")
                .TransferEntropy(source, target, new TransferEntropyOptions(Neighbours: 4, Window: 100, Stride: 5)))
            .Build();
        return figure;
    }
}
