// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Playground;

/// <summary>Centralised registry + builder for every Playground example. Each example takes
/// a <see cref="PlaygroundOptions"/> and returns a (Figure, code-snippet) pair.
///
/// <para>Why a single static registry instead of one class per example?</para>
/// 1. The Razor component just queries <see cref="Examples"/> for the dropdown and calls
///    <see cref="Build"/> on selection — no per-example wiring.
/// 2. Tests can iterate every example to assert basic invariants (builds without throw,
///    respects toggles, contains expected SVG markers) — see PlaygroundExampleTests.
/// 3. Adding a new example is a single dictionary entry — Open/Closed for new examples,
///    closed for modification of existing ones.</summary>
public static class PlaygroundExamples
{
    /// <summary>All playground examples in display order (Phase N.1 — backed by the
    /// <see cref="PlaygroundExample"/> enum; display labels come from its
    /// <see cref="System.ComponentModel.DescriptionAttribute"/>).</summary>
    public static IReadOnlyList<PlaygroundExample> Examples =>
        Enum.GetValues<PlaygroundExample>();

    public static (Figure Figure, string Code) Build(PlaygroundExample example, PlaygroundOptions opts)
    {
        if (!_builders.TryGetValue(example, out var builder))
            throw new ArgumentException(
                $"No builder registered for {example}. This indicates a missing case after adding a new enum value.",
                nameof(example));
        return builder(opts);
    }

    /// <summary>True if the example actually draws a line — i.e. Line-style / Line-width
    /// controls have a visible effect. Scatter plots are excluded because they don't draw
    /// a line (Phase L.6 of the v1.7.2 plan).</summary>
    public static bool SupportsLineControls(PlaygroundExample example) =>
        example is PlaygroundExample.LineChart or PlaygroundExample.MultiSeries;

    /// <summary>True if the example's primary series exposes a <c>MarkerStyle</c> /
    /// <c>MarkerSize</c> — i.e. the playground's Marker / Marker-size controls should be
    /// shown. Scatter (always-on markers) + Line families (optional per-point markers).</summary>
    public static bool SupportsMarkerControls(PlaygroundExample example) =>
        example is PlaygroundExample.LineChart or PlaygroundExample.ScatterPlot or PlaygroundExample.MultiSeries;

    /// <summary>True if the example exposes colormap / colorbar controls in the UI.</summary>
    public static bool SupportsColormap(PlaygroundExample example) =>
        example is PlaygroundExample.Heatmap or PlaygroundExample.ContourPlot;

    private static readonly Dictionary<PlaygroundExample, Func<PlaygroundOptions, (Figure, string)>> _builders =
        new()
        {
            [PlaygroundExample.LineChart]    = BuildLine,
            [PlaygroundExample.BarChart]     = BuildBar,
            [PlaygroundExample.ScatterPlot]  = BuildScatter,
            [PlaygroundExample.MultiSeries]  = BuildMultiSeries,
            [PlaygroundExample.Heatmap]      = BuildHeatmap,
            [PlaygroundExample.PieChart]     = BuildPie,
            [PlaygroundExample.Histogram]    = BuildHist,
            [PlaygroundExample.ContourPlot]  = BuildContour,
            [PlaygroundExample.Surface3D]    = BuildSurface,
            [PlaygroundExample.RadarChart]   = BuildRadar,
            [PlaygroundExample.ViolinPlot]   = BuildViolin,
            [PlaygroundExample.Candlestick]  = BuildCandlestick,
            [PlaygroundExample.Treemap]      = BuildTreemap,
            [PlaygroundExample.SankeyFlow]   = BuildSankey,
            [PlaygroundExample.PolarLine]    = BuildPolar,
            [PlaygroundExample.MultiSubplot] = BuildMulti,
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Example builders — each returns (Figure, code snippet for the user to copy)
    // ──────────────────────────────────────────────────────────────────────────

    private static (Figure, string) BuildLine(PlaygroundOptions opts)
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, y, s =>
                {
                    s.Color = Colors.Blue;
                    s.Label = "Revenue";
                    s.LineStyle = opts.LineStyle;
                    s.LineWidth = opts.LineWidth;
                    if (opts.Marker != MarkerStyle.None) { s.Marker = opts.Marker; s.MarkerSize = opts.MarkerSize; }
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.LineChart, opts));
    }

    private static (Figure, string) BuildBar(PlaygroundOptions opts)
    {
        string[] cats = ["Q1", "Q2", "Q3", "Q4"];
        double[] vals = [23, 45, 12, 67];

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Bar(cats, vals, s => { s.Color = Colors.Orange; s.Label = "Units"; });
                ax.WithBarLabels();
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.BarChart, opts));
    }

    private static (Figure, string) BuildScatter(PlaygroundOptions opts)
    {
        var rng = new Random(42);
        double[] x = Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 10).ToArray();
        double[] y = x.Select(v => v * 0.8 + rng.NextDouble() * 3).ToArray();

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Scatter(x, y, s =>
                {
                    s.Color = Colors.CornflowerBlue;
                    s.Marker = opts.Marker == MarkerStyle.None ? MarkerStyle.Circle : opts.Marker;
                    s.MarkerSize = opts.MarkerSize;
                    s.Label = "Data";
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.ScatterPlot, opts));
    }

    private static (Figure, string) BuildMultiSeries(PlaygroundOptions opts)
    {
        double[] x = Enumerable.Range(0, 50).Select(i => i * 0.2).ToArray();
        bool hasMarker = opts.Marker != MarkerStyle.None;

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, x.Select(v => Math.Sin(v)).ToArray(), s =>
                {
                    s.Label = "sin(x)"; s.LineStyle = opts.LineStyle; s.LineWidth = opts.LineWidth;
                    if (hasMarker) { s.Marker = opts.Marker; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                ax.Plot(x, x.Select(v => Math.Cos(v)).ToArray(), s =>
                {
                    s.Label = "cos(x)"; s.LineWidth = opts.LineWidth;
                    if (hasMarker) { s.Marker = opts.Marker; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                ax.Plot(x, x.Select(v => Math.Sin(v) * Math.Cos(v)).ToArray(), s =>
                {
                    s.Label = "sin·cos"; s.LineWidth = opts.LineWidth;
                    if (hasMarker) { s.Marker = opts.Marker; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.MultiSeries, opts));
    }

    private static (Figure, string) BuildHeatmap(PlaygroundOptions opts)
    {
        var matrix = new double[10, 10];
        for (int r = 0; r < 10; r++)
            for (int c = 0; c < 10; c++)
                matrix[r, c] = Math.Sin(r * 0.5) * Math.Cos(c * 0.5);

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Heatmap(matrix).WithColorMap(opts.ColorMap);
                if (opts.ShowColorBar) ax.WithColorBar(cb => cb with { Label = "Intensity" });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.Heatmap, opts));
    }

    private static (Figure, string) BuildPie(PlaygroundOptions opts)
    {
        string[] labels = ["Python", "C#", "Java", "Go", "Rust"];
        double[] sizes = [35, 25, 20, 12, 8];

        var fb = opts.ApplyToFigure(Plt.Create())
            .Pie(sizes, labels, s =>
            {
                s.AutoPct = "%.0f%%";
                s.Shadow = true;
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.PieChart, opts));
    }

    private static (Figure, string) BuildHist(PlaygroundOptions opts)
    {
        var rng = new Random(42);
        double[] data = Enumerable.Range(0, 500).Select(_ => rng.NextDouble() * 6 + rng.NextDouble() * 6).ToArray();

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Hist(data, 20, s => { s.Color = Colors.Teal; s.EdgeColor = Colors.White; s.Label = "Distribution"; });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.Histogram, opts));
    }

    private static (Figure, string) BuildContour(PlaygroundOptions opts)
    {
        int n = 20;
        double[] cx = Enumerable.Range(0, n).Select(i => i * 0.5 - 5.0).ToArray();
        double[] cy = Enumerable.Range(0, n).Select(i => i * 0.5 - 5.0).ToArray();
        var cz = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                cz[r, c] = Math.Sin(cx[c]) * Math.Cos(cy[r]);

        // Phase L.9 — colormap lives on the series directly (set inside the Contour lambda
        // below). Phase N.1 further tightened this: opts.ColorMap is now a typed IColorMap
        // instance, not a string; no registry lookup or exception throw needed at the
        // call site.

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Contour(cx, cy, cz, s =>
                {
                    s.ShowLabels = true;
                    s.LabelFormat = "F2";
                    s.ColorMap = opts.ColorMap;
                });
                if (opts.ShowColorBar) ax.WithColorBar();
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.ContourPlot, opts));
    }

    private static (Figure, string) BuildSurface(PlaygroundOptions opts)
    {
        int n = 20;
        double[] sx = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        double[] sy = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sz = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double r = Math.Sqrt(sx[i] * sx[i] + sy[j] * sy[j]);
                sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
            }

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50)
                .WithLighting(dx: 0.5, dy: -0.5, dz: 1.0)
                .Surface(sx, sy, sz, s => { s.ColorMap = ColorMaps.Plasma; s.ShowWireframe = false; }));

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.Surface3D, opts));
    }

    private static (Figure, string) BuildRadar(PlaygroundOptions opts)
    {
        string[] categories = ["Speed", "Power", "Defense", "Range", "Accuracy"];
        double[] v1 = [85, 70, 90, 60, 95];
        double[] v2 = [70, 95, 60, 80, 75];

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Radar(categories, v1, s => { s.Color = Colors.Blue; s.Alpha = 0.2; s.Label = "Player 1"; });
                ax.Radar(categories, v2, s => { s.Color = Colors.Red; s.Alpha = 0.2; s.Label = "Player 2"; });
                if (opts.ShowLegend) ax.WithLegend();
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.RadarChart, opts));
    }

    private static (Figure, string) BuildViolin(PlaygroundOptions opts)
    {
        var rng = new Random(42);
        double[][] groups = [
            Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 10).ToArray(),
            Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 8 + 2).ToArray(),
            Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 12 - 1).ToArray(),
        ];

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Violin(groups, s => { s.Color = Colors.RebeccaPurple; s.Alpha = 0.6; });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.ViolinPlot, opts));
    }

    private static (Figure, string) BuildCandlestick(PlaygroundOptions opts)
    {
        var rng = new Random(42);
        double price = 100;
        int n = 30;
        double[] open = new double[n], high = new double[n], low = new double[n], close = new double[n];
        string[] dates = new string[n];
        for (int i = 0; i < n; i++)
        {
            open[i] = price;
            double change = (rng.NextDouble() - 0.48) * 5;
            close[i] = price + change;
            high[i] = Math.Max(open[i], close[i]) + rng.NextDouble() * 3;
            low[i] = Math.Min(open[i], close[i]) - rng.NextDouble() * 3;
            dates[i] = new DateTime(2025, 1, 1).AddDays(i).ToString("MMM dd");
            price = close[i];
        }

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Candlestick(open, high, low, close, dates, s =>
                {
                    s.UpColor = Colors.Green;
                    s.DownColor = Colors.Red;
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.Candlestick, opts));
    }

    /// <summary>Phase G.7 of v1.7.2 follow-on — Sankey flow example with browser-side
    /// hover emphasis. Hovering a node dims every link not reachable upstream or
    /// downstream from that node (ECharts focus:adjacency parity). Keyboard users
    /// get the same behaviour via Tab+focus.</summary>
    private static (Figure, string) BuildSankey(PlaygroundOptions opts)
    {
        var nodes = new[]
        {
            new SankeyNode("Coal"),
            new SankeyNode("Gas"),
            new SankeyNode("Solar"),
            new SankeyNode("Grid"),
            new SankeyNode("Industry"),
            new SankeyNode("Homes"),
        };
        var links = new[]
        {
            new SankeyLink(0, 3, 60),
            new SankeyLink(1, 3, 40),
            new SankeyLink(2, 3, 25),
            new SankeyLink(3, 4, 70),
            new SankeyLink(3, 5, 55),
        };
        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links).HideAllAxes());
        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.SankeyFlow, opts));
    }

    private static (Figure, string) BuildTreemap(PlaygroundOptions opts)
    {
        var root = new TreeNode
        {
            Label = "Revenue",
            Children =
            [
                new() { Label = "Electronics", Value = 42, Color = Colors.Blue },
                new() { Label = "Apparel", Value = 28, Color = Colors.Orange },
                new() { Label = "Grocery", Value = 30, Color = Colors.Green },
                new() { Label = "Home", Value = 18, Color = Colors.RebeccaPurple },
            ]
        };

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.ShowLabels = true).HideAllAxes());

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.Treemap, opts));
    }

    private static (Figure, string) BuildPolar(PlaygroundOptions opts)
    {
        double[] theta = Enumerable.Range(0, 100).Select(i => i * 2 * Math.PI / 100).ToArray();
        double[] r = theta.Select(t => 1 + Math.Cos(3 * t)).ToArray();

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.PolarPlot(r, theta, s => { s.Color = Colors.Blue; s.LineWidth = 2; s.Label = "r = 1 + cos(3θ)"; });
                if (opts.ShowLegend) ax.WithLegend();
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.PolarLine, opts));
    }

    private static (Figure, string) BuildMulti(PlaygroundOptions opts)
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8];
        double[] y1 = [2, 4, 3, 5, 4, 6, 5, 7];
        string[] cats = ["A", "B", "C", "D"];
        double[] vals = [15, 42, 28, 37];

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 2, 1, ax =>
            {
                ax.Plot(x, y1, s => { s.Color = Colors.Blue; s.Label = "Line"; });
                ax.WithTitle("Line");
                opts.ApplyToAxes(ax);
            })
            .AddSubPlot(1, 2, 2, ax =>
            {
                ax.Bar(cats, vals, s => { s.Color = Colors.Orange; s.Label = "Bars"; });
                ax.WithTitle("Bar");
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor(PlaygroundExample.MultiSubplot, opts));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Code snippets — kept inline so users see the EXACT call chain to copy
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Converts a colormap's registry-case name ("viridis") to the PascalCase
    /// static-property name (<c>ColorMaps.Viridis</c>) used in the code-snippet generator.
    /// Keeps the displayed C# valid for copy-paste.</summary>
    private static string ColorMapName(Styling.ColorMaps.IColorMap map)
    {
        var n = map.Name;
        if (n.Length == 0) return "Viridis";
        return char.ToUpperInvariant(n[0]) + n.Substring(1);
    }

    private static string CodeFor(PlaygroundExample example, PlaygroundOptions opts)
    {
        // Snippet shows the active toggles so the user can copy a working repro.
        var lines = new List<string> { "Plt.Create()" };
        lines.Add($"    .WithTitle(\"{opts.Title}\")");
        lines.Add($"    .WithTheme(Theme.{opts.Theme.Name})");
        lines.Add($"    .WithSize({opts.Width}, {opts.Height})");
        if (opts.BrowserInteraction) lines.Add("    .WithBrowserInteraction()");

        // Phase N.1 — exhaustive enum switch with ArgumentOutOfRangeException default;
        // a new PlaygroundExample member without a matching case fails loudly at runtime
        // (eventually fails at compile time when C# adds required-exhaustive switches).
        var body = example switch
        {
            PlaygroundExample.LineChart    => $"    .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y, s => {{ s.Color = Colors.Blue; s.LineStyle = LineStyle.{opts.LineStyle}; s.LineWidth = {opts.LineWidth}; }}))",
            PlaygroundExample.BarChart     => "    .AddSubPlot(1, 1, 1, ax => ax.Bar(cats, vals, s => s.Color = Colors.Orange).WithBarLabels())",
            PlaygroundExample.ScatterPlot  => "    .AddSubPlot(1, 1, 1, ax => ax.Scatter(x, y, s => s.Color = Colors.CornflowerBlue))",
            PlaygroundExample.MultiSeries  => "    .AddSubPlot(1, 1, 1, ax => { ax.Plot(x, sin); ax.Plot(x, cos); ax.Plot(x, sincos); ax.WithLegend(); })",
            PlaygroundExample.Heatmap      => $"    .AddSubPlot(1, 1, 1, ax => ax.Heatmap(m).WithColorMap(ColorMaps.{ColorMapName(opts.ColorMap)}){(opts.ShowColorBar ? ".WithColorBar()" : string.Empty)})",
            PlaygroundExample.PieChart     => "    .Pie(sizes, labels, s => { s.AutoPct = \"%.0f%%\"; s.Shadow = true; })",
            PlaygroundExample.Histogram    => "    .AddSubPlot(1, 1, 1, ax => ax.Hist(data, 20, s => s.Color = Colors.Teal))",
            PlaygroundExample.ContourPlot  => $"    .AddSubPlot(1, 1, 1, ax => ax.Contour(x, y, z, s => s.ColorMap = ColorMaps.{ColorMapName(opts.ColorMap)}){(opts.ShowColorBar ? ".WithColorBar()" : string.Empty)})",
            PlaygroundExample.Surface3D    => "    .AddSubPlot(1, 1, 1, ax => ax.WithCamera(35, -50).Surface(x, y, z, s => s.ColorMap = ColorMaps.Plasma))",
            PlaygroundExample.RadarChart   => "    .AddSubPlot(1, 1, 1, ax => { ax.Radar(cats, v1, s => { s.Alpha = 0.2; }); ax.Radar(cats, v2, s => { s.Alpha = 0.2; }); ax.WithLegend(); })",
            PlaygroundExample.ViolinPlot   => "    .AddSubPlot(1, 1, 1, ax => ax.Violin(groups, s => { s.Color = Colors.RebeccaPurple; s.Alpha = 0.6; }))",
            PlaygroundExample.Candlestick  => "    .AddSubPlot(1, 1, 1, ax => ax.Candlestick(o, h, l, c, dates, s => { s.UpColor = Colors.Green; s.DownColor = Colors.Red; }))",
            PlaygroundExample.Treemap      => "    .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.ShowLabels = true).HideAllAxes())",
            PlaygroundExample.SankeyFlow   => "    .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links).HideAllAxes())",
            PlaygroundExample.PolarLine    => "    .AddSubPlot(1, 1, 1, ax => ax.PolarPlot(r, theta, s => { s.Color = Colors.Blue; s.LineWidth = 2; }))",
            PlaygroundExample.MultiSubplot => "    .AddSubPlot(1, 2, 1, ax => ax.Plot(x, y).WithTitle(\"Line\"))\n    .AddSubPlot(1, 2, 2, ax => ax.Bar(cats, vals).WithTitle(\"Bar\"))",
            _                              => throw new ArgumentOutOfRangeException(nameof(example), example,
                                                  $"CodeFor: no snippet registered for new enum value {example}; update the switch."),
        };
        lines.Add(body);
        if (opts.ShowLegend) lines.Add("    // .WithLegend() — already chained inside subplot");
        if (opts.HideTopSpine || opts.HideRightSpine || opts.TightMargins || !opts.ShowGrid)
        {
            var ax = new List<string>();
            if (!opts.ShowGrid)      ax.Add(".ShowGrid(false)");
            if (opts.HideTopSpine)   ax.Add(".HideTopSpine()");
            if (opts.HideRightSpine) ax.Add(".HideRightSpine()");
            if (opts.TightMargins)   ax.Add(".WithTightMargins()");
            lines.Add($"    // axes options applied: {string.Join(" ", ax)}");
        }
        if (opts.TightLayout) lines.Add("    .TightLayout()");
        lines.Add("    .Save(\"chart.svg\");");
        return string.Join("\n", lines);
    }
}
