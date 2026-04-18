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
/// 1. The Razor component just queries <see cref="Names"/> for the dropdown and calls
///    <see cref="Build"/> on selection — no per-example wiring.
/// 2. Tests can iterate every example to assert basic invariants (builds without throw,
///    respects toggles, contains expected SVG markers) — see PlaygroundExampleTests.
/// 3. Adding a new example is a single dictionary entry — Open/Closed for new examples,
///    closed for modification of existing ones.</summary>
public static class PlaygroundExamples
{
    public static IReadOnlyList<string> Names => _builders.Keys.ToList();

    public static (Figure Figure, string Code) Build(string name, PlaygroundOptions opts)
    {
        if (!_builders.TryGetValue(name, out var builder))
            throw new ArgumentException($"Unknown example '{name}'. Available: {string.Join(", ", Names)}", nameof(name));
        return builder(opts);
    }

    /// <summary>True if the example exposes line-style / marker controls in the UI.</summary>
    public static bool SupportsLineControls(string name) =>
        name is "Line Chart" or "Scatter Plot" or "Multi-Series";

    /// <summary>True if the example exposes colormap / colorbar controls in the UI.</summary>
    public static bool SupportsColormap(string name) =>
        name is "Heatmap" or "Contour Plot";

    private static readonly Dictionary<string, Func<PlaygroundOptions, (Figure, string)>> _builders =
        new()
        {
            ["Line Chart"]    = BuildLine,
            ["Bar Chart"]     = BuildBar,
            ["Scatter Plot"]  = BuildScatter,
            ["Multi-Series"]  = BuildMultiSeries,
            ["Heatmap"]       = BuildHeatmap,
            ["Pie Chart"]     = BuildPie,
            ["Histogram"]     = BuildHist,
            ["Contour Plot"]  = BuildContour,
            ["3D Surface"]    = BuildSurface,
            ["Radar Chart"]   = BuildRadar,
            ["Violin Plot"]   = BuildViolin,
            ["Candlestick"]   = BuildCandlestick,
            ["Treemap"]       = BuildTreemap,
            ["Polar Line"]    = BuildPolar,
            ["Multi-Subplot"] = BuildMulti,
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Example builders — each returns (Figure, code snippet for the user to copy)
    // ──────────────────────────────────────────────────────────────────────────

    private static (Figure, string) BuildLine(PlaygroundOptions opts)
    {
        double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];
        var marker = opts.ResolvedMarker;

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, y, s =>
                {
                    s.Color = Colors.Blue;
                    s.Label = "Revenue";
                    s.LineStyle = opts.ResolvedLineStyle;
                    s.LineWidth = opts.LineWidth;
                    if (marker.HasValue) { s.Marker = marker.Value; s.MarkerSize = opts.MarkerSize; }
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Line Chart", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Bar Chart", opts));
    }

    private static (Figure, string) BuildScatter(PlaygroundOptions opts)
    {
        var rng = new Random(42);
        double[] x = Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 10).ToArray();
        double[] y = x.Select(v => v * 0.8 + rng.NextDouble() * 3).ToArray();
        var marker = opts.ResolvedMarker;

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Scatter(x, y, s =>
                {
                    s.Color = Colors.CornflowerBlue;
                    s.MarkerSize = marker.HasValue ? opts.MarkerSize : 6;
                    s.Label = "Data";
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Scatter Plot", opts));
    }

    private static (Figure, string) BuildMultiSeries(PlaygroundOptions opts)
    {
        double[] x = Enumerable.Range(0, 50).Select(i => i * 0.2).ToArray();
        var marker = opts.ResolvedMarker;

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(x, x.Select(v => Math.Sin(v)).ToArray(), s =>
                {
                    s.Label = "sin(x)"; s.LineStyle = opts.ResolvedLineStyle; s.LineWidth = opts.LineWidth;
                    if (marker.HasValue) { s.Marker = marker.Value; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                ax.Plot(x, x.Select(v => Math.Cos(v)).ToArray(), s =>
                {
                    s.Label = "cos(x)"; s.LineWidth = opts.LineWidth;
                    if (marker.HasValue) { s.Marker = marker.Value; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                ax.Plot(x, x.Select(v => Math.Sin(v) * Math.Cos(v)).ToArray(), s =>
                {
                    s.Label = "sin·cos"; s.LineWidth = opts.LineWidth;
                    if (marker.HasValue) { s.Marker = marker.Value; s.MarkerSize = opts.MarkerSize; s.MarkEvery = 5; }
                });
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Multi-Series", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Heatmap", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Pie Chart", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Histogram", opts));
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

        var fb = opts.ApplyToFigure(Plt.Create())
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Contour(cx, cy, cz, s => { s.ShowLabels = true; s.LabelFormat = "F2"; }).WithColorMap(opts.ColorMap);
                if (opts.ShowColorBar) ax.WithColorBar();
                opts.ApplyToAxes(ax);
            });

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Contour Plot", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("3D Surface", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Radar Chart", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Violin Plot", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Candlestick", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Treemap", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Polar Line", opts));
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

        return (opts.ApplyTightLayout(fb).Build(), CodeFor("Multi-Subplot", opts));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Code snippets — kept inline so users see the EXACT call chain to copy
    // ──────────────────────────────────────────────────────────────────────────

    private static string CodeFor(string name, PlaygroundOptions opts)
    {
        // Snippet shows the active toggles so the user can copy a working repro.
        var lines = new List<string> { "Plt.Create()" };
        lines.Add($"    .WithTitle(\"{opts.Title}\")");
        lines.Add($"    .WithTheme(Theme.{opts.ThemeName})");
        lines.Add($"    .WithSize({opts.Width}, {opts.Height})");
        if (opts.BrowserInteraction) lines.Add("    .WithBrowserInteraction()");

        var body = name switch
        {
            "Line Chart"    => "    .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y, s => { s.Color = Colors.Blue; s.LineStyle = LineStyle." + opts.LineStyle + "; s.LineWidth = " + opts.LineWidth + "; }))",
            "Bar Chart"     => "    .AddSubPlot(1, 1, 1, ax => ax.Bar(cats, vals, s => s.Color = Colors.Orange).WithBarLabels())",
            "Scatter Plot"  => "    .AddSubPlot(1, 1, 1, ax => ax.Scatter(x, y, s => s.Color = Colors.CornflowerBlue))",
            "Multi-Series"  => "    .AddSubPlot(1, 1, 1, ax => { ax.Plot(x, sin); ax.Plot(x, cos); ax.Plot(x, sincos); ax.WithLegend(); })",
            "Heatmap"       => "    .AddSubPlot(1, 1, 1, ax => ax.Heatmap(m).WithColorMap(\"" + opts.ColorMap + "\")" + (opts.ShowColorBar ? ".WithColorBar()" : "") + ")",
            "Pie Chart"     => "    .Pie(sizes, labels, s => { s.AutoPct = \"%.0f%%\"; s.Shadow = true; })",
            "Histogram"     => "    .AddSubPlot(1, 1, 1, ax => ax.Hist(data, 20, s => s.Color = Colors.Teal))",
            "Contour Plot"  => "    .AddSubPlot(1, 1, 1, ax => ax.Contour(x, y, z).WithColorMap(\"" + opts.ColorMap + "\")" + (opts.ShowColorBar ? ".WithColorBar()" : "") + ")",
            "3D Surface"    => "    .AddSubPlot(1, 1, 1, ax => ax.WithCamera(35, -50).Surface(x, y, z, s => s.ColorMap = ColorMaps.Plasma))",
            "Radar Chart"   => "    .AddSubPlot(1, 1, 1, ax => { ax.Radar(cats, v1, s => { s.Alpha = 0.2; }); ax.Radar(cats, v2, s => { s.Alpha = 0.2; }); ax.WithLegend(); })",
            "Violin Plot"   => "    .AddSubPlot(1, 1, 1, ax => ax.Violin(groups, s => { s.Color = Colors.RebeccaPurple; s.Alpha = 0.6; }))",
            "Candlestick"   => "    .AddSubPlot(1, 1, 1, ax => ax.Candlestick(o, h, l, c, dates, s => { s.UpColor = Colors.Green; s.DownColor = Colors.Red; }))",
            "Treemap"       => "    .AddSubPlot(1, 1, 1, ax => ax.Treemap(root, s => s.ShowLabels = true).HideAllAxes())",
            "Polar Line"    => "    .AddSubPlot(1, 1, 1, ax => ax.PolarPlot(r, theta, s => { s.Color = Colors.Blue; s.LineWidth = 2; }))",
            "Multi-Subplot" => "    .AddSubPlot(1, 2, 1, ax => ax.Plot(x, y).WithTitle(\"Line\"))\n    .AddSubPlot(1, 2, 2, ax => ax.Bar(cats, vals).WithTitle(\"Bar\"))",
            _               => "    .Plot(x, y)",
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
