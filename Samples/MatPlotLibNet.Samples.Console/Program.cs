// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Samples.Console;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Transforms;
using MatPlotLibNet.Geo;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Skia;

// Resolves a sample output filename to the canonical `images/` directory at the repo root,
// regardless of where the samples binary is invoked from. Walks upward from the binary
// directory until it finds the `MatPlotLibNet.CI.slnf` solution-filter sentinel, then
// writes into <repo>/images/<name>. Falls back to the cwd-relative `images/` directory.
static string SamplesPath(string name)
{
    var dir = AppContext.BaseDirectory;
    while (dir is not null && !File.Exists(Path.Combine(dir, "MatPlotLibNet.CI.slnf")))
        dir = Path.GetDirectoryName(dir);
    var imagesDir = dir is not null ? Path.Combine(dir, "images") : "images";
    Directory.CreateDirectory(imagesDir);
    return Path.Combine(imagesDir, name);
}

// The same walk, but for the few files that belong at the repository ROOT rather than in images/ —
// the package icon, which Directory.Build.props packs into all fifteen packages from there.
static string RepoPath(string name)
{
    var dir = AppContext.BaseDirectory;
    while (dir is not null && !File.Exists(Path.Combine(dir, "MatPlotLibNet.CI.slnf")))
        dir = Path.GetDirectoryName(dir);
    return dir is not null ? Path.Combine(dir, name) : name;
}


// --- 1. Simple line chart -> SVG ---
double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];

var figure = Plt.Create()
    .WithTitle("Sales Trend")
    .WithTheme(Theme.Seaborn)
    .WithSize(800, 500)
    .Plot(x, y, line => { line.Color = Colors.Blue; line.Label = "Revenue"; })
    .Build();

figure.Transform(new SvgTransform()).ToFile(SamplesPath("chart.svg"));
Console.WriteLine("Saved chart.svg");

// --- 2. PNG and PDF export (via Skia) ---
figure.Transform(new PngTransform()).ToFile(SamplesPath("chart.png"));
figure.Transform(new PdfTransform()).ToFile(SamplesPath("chart.pdf"));
Console.WriteLine("Saved chart.png and chart.pdf");

// --- 3. Multi-subplot figure ---
string[] categories = ["Q1", "Q2", "Q3", "Q4", "Q5"];
double[] values = [23, 45, 12, 67, 34];
double[] histData = [1.2, 2.3, 2.1, 3.4, 3.5, 3.6, 4.1, 4.8, 5.2, 5.5, 6.1, 6.3];

var multiChart = Plt.Create()
    .WithTitle("Dashboard")
    .WithTheme(Theme.Dark)
    .Bar(categories, values, bar => { bar.Color = Colors.Orange; bar.Label = "Units sold"; })
    .AddSubPlot(1, 2, 2, ax => ax.Hist(histData, 6))
    .Build();

multiChart.Transform(new SvgTransform()).ToFile(SamplesPath("dashboard.svg"));
multiChart.Transform(new PngTransform()).ToFile(SamplesPath("dashboard.png"));
Console.WriteLine("Saved dashboard.svg");

// --- 4. JSON round-trip ---
var json = figure.ToJson(indented: true);
Console.WriteLine($"JSON length: {json.Length} chars");

var deserialized = ChartSerializerDefault.Current.FromJson(json);
Console.WriteLine($"Deserialized figure: {deserialized.Title}");

// --- 5. Heatmap with colormap + colorbar ---
double[,] matrix = new double[10, 10];
for (int r = 0; r < 10; r++)
    for (int c = 0; c < 10; c++)
        matrix[r, c] = Math.Sin(r * 0.5) * Math.Cos(c * 0.5);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Heatmap — Plasma")
        .Heatmap(matrix)
        .WithColorMap("plasma")
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("heatmap_colormap.svg"));
Console.WriteLine("Saved heatmap_colormap.svg");

// --- 6. Colormap comparison (2x2 grid) ---
string[] maps = ["viridis", "turbo", "coolwarm", "greys"];
var cmpBuilder = Plt.Create()
    .WithTitle("Colormap Comparison")
    .WithSize(1200, 800);

for (int i = 0; i < maps.Length; i++)
{
    var mapName = maps[i];
    cmpBuilder.AddSubPlot(2, 2, i + 1, ax => ax
        .WithTitle(mapName)
        .Heatmap(matrix)
        .WithColorMap(mapName)
        .WithColorBar());
}

cmpBuilder.TightLayout().SaveSvgAndPng(SamplesPath("colormap_comparison.svg"));
Console.WriteLine("Saved colormap_comparison.svg");

// --- 7. GridSpec layout ---
string[] categories2 = ["A", "B", "C", "D"];
double[] catValues = [15, 42, 28, 37];

Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main plot"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y).WithTitle("Scatter"))
    .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Bar(categories2, catValues).WithTitle("Wide bar"))
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("gridspec_layout.svg"));
Console.WriteLine("Saved gridspec_layout.svg");

// --- 8. Custom tick locators ---
double[] bigX = Enumerable.Range(0, 200).Select(i => (double)i).ToArray();
double[] bigY = bigX.Select(v => Math.Sin(v * 0.1) * 100_000 + v * 500).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Engineering notation + MultipleLocator")
        .SetXLabel("Sample index")
        .SetYLabel("Amplitude")
        .Plot(bigX, bigY, line => { line.Label = "Signal"; })
        .SetYTickFormatter(new EngFormatter())           // e.g. "100k", "50k"
        .SetXTickLocator(new MultipleLocator(25))        // ticks every 25 samples
        .WithMinorTicks())
    .SaveSvgAndPng(SamplesPath("tick_locators.svg"));
Console.WriteLine("Saved tick_locators.svg");

// --- 9. Bar labels ---
string[] products = ["Alpha", "Beta", "Gamma", "Delta"];
double[] sales    = [12_500, 34_800, 8_200, 27_600];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Sales by Product")
        .SetYLabel("Revenue ($)")
        .Bar(products, sales, bar => { bar.Color = Colors.Tab10Blue; bar.Label = "Q1 Sales"; })
        .WithBarLabels("F0")                            // integer labels above bars
        .SetYTickFormatter(new EngFormatter()))
    .SaveSvgAndPng(SamplesPath("bar_labels.svg"));
Console.WriteLine("Saved bar_labels.svg");

// --- 10. LTTB downsampling for large dataset ---
double[] largeX = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
double[] largeY = largeX.Select(v => Math.Sin(v * 0.05) * Math.Exp(-v * 0.0003) + (v % 500 == 0 ? 5 : 0)).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("10 000-point signal (LTTB → 500 display points)")
        .SetXLabel("Time")
        .SetYLabel("Amplitude")
        .Plot(largeX, largeY, line => { line.Label = "Signal"; })
        .WithDownsampling(500))                         // LTTB to 500 points
    .SaveSvgAndPng(SamplesPath("lttb_downsampling.svg"));
Console.WriteLine("Saved lttb_downsampling.svg");

// --- 11. Rotated + background annotation ---
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Annotation Enhancements")
        .Plot(x, y, line => { line.Label = "Data"; })
        .Annotate("Peak", 8, 8.9, ann =>
        {
            ann.ArrowTargetX = 8;
            ann.ArrowTargetY = 8.9;
            ann.ArrowStyle   = MatPlotLibNet.Models.ArrowStyle.FancyArrow;
            ann.BackgroundColor = Colors.White;
        })
        .Annotate("Rotated label", 2, 4.5, ann =>
        {
            ann.Rotation  = -30;
            ann.Alignment = MatPlotLibNet.Rendering.TextAlignment.Center;
        }))
    .SaveSvgAndPng(SamplesPath("annotations_enhanced.svg"));
Console.WriteLine("Saved annotations_enhanced.svg");

// --- 11b. Annotations that move out of the way of the data, the legend and each other ---
{
    double[] ramp = Enumerable.Range(0, 11).Select(i => (double)i).ToArray();
    Plt.Create()
        .WithSize(700, 420)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithTitle("Annotations that move out of the way")
            .Plot(ramp, ramp, line => { line.Label = "Growth"; })
            .WithLegend(MatPlotLibNet.Models.LegendPosition.UpperRight)
            .Annotate("Start", 1.0, 1.1, ann => ann.AvoidOverlap = true)
            .Annotate("Halfway", 5.0, 5.0, ann => ann.AvoidOverlap = true)
            .Annotate("Halfway, noted twice", 5.1, 5.15, ann => ann.AvoidOverlap = true)
            .Annotate("Peak", 9.0, 9.0, ann =>
            {
                ann.AvoidOverlap = true;
                ann.ArrowTargetX = 9.0;
                ann.ArrowTargetY = 9.0;
            }))
        .SaveSvgAndPng(SamplesPath("annotations_avoid_overlap.svg"));
    Console.WriteLine("Saved annotations_avoid_overlap.svg");
}

// --- 11c. Bar value labels stand beyond the end of the bar, whichever way it points ---
{
    string[] groups = ["North", "South", "East", "West"];
    double[] change = [3.0, -2.0, 5.0, -4.0];
    Plt.Create()
        .WithSize(800, 380)
        .AddSubPlot(1, 2, 1, ax => ax
            .WithTitle("Axis running up")
            .Bar(groups, change, s => s.ShowLabels = true)
            .SetYLim(-6, 8))
        .AddSubPlot(1, 2, 2, ax => ax
            .WithTitle("Axis running down")
            .Bar(groups, change, s => s.ShowLabels = true)
            .SetYLim(8, -6))
        .SaveSvgAndPng(SamplesPath("bar_labels_beyond_the_end.svg"));
    Console.WriteLine("Saved bar_labels_beyond_the_end.svg");
}

// =====================================================================
// SIMD vectors, financial templates and technical indicators
// =====================================================================

// --- 12. Vec — SIMD-accelerated numeric computation ---
var rng = new Random(42);
double[] priceArr = new double[100];
priceArr[0] = 100;
for (int i = 1; i < priceArr.Length; i++)
    priceArr[i] = priceArr[i - 1] + (rng.NextDouble() - 0.48) * 3;

Vec prices = priceArr;                                  // implicit conversion from double[]
Vec shifted = prices.Slice(1, prices.Length - 1);
Vec prev    = prices.Slice(0, prices.Length - 1);
Vec diff    = shifted - prev;                            // SIMD-accelerated operators
Vec returns = diff.Zip(prev, (d, p) => p == 0 ? 0 : d / p * 100);

Console.WriteLine($"Vec: {prices.Length} prices, mean return={returns.Mean():F3}%, " +
                  $"std={returns.Std():F3}%, max={returns.Max():F3}%, min={returns.Min():F3}%");

// --- 13. Financial dashboard template ---
double[] open  = new double[50];
double[] high  = new double[50];
double[] low   = new double[50];
double[] close = new double[50];
double[] vol   = new double[50];
double price = 100;
for (int i = 0; i < 50; i++)
{
    double change = (rng.NextDouble() - 0.48) * 4;
    open[i]  = price;
    high[i]  = price + Math.Abs(change) + rng.NextDouble() * 2;
    low[i]   = price - Math.Abs(change) - rng.NextDouble() * 2;
    price   += change;
    close[i] = price;
    vol[i]   = 50_000 + rng.NextDouble() * 100_000;
}

FigureTemplates.FinancialDashboard(
        open, high, low, close, vol,
        title: "ACME Corp — 50 Day",
        configurePricePanel: ax => ax.BollingerBands(20),
        configureVolumePanel: ax => ax
            .SetYTickLocator(new MaxNLocator(3))
            .SetYTickFormatter(new EngFormatter()),
        configureOscillatorPanel: ax => ax
            .Rsi(close, 14)
            .SetYLim(0, 100)
            .AxHLine(70, l => { l.Color = Colors.Red;   l.LineStyle = LineStyle.Dashed; l.Label = "Overbought"; })
            .AxHLine(20, l => { l.Color = Colors.Green; l.LineStyle = LineStyle.Dashed; l.Label = "Oversold"; }))
    .WithSize(1200, 900)
    .SaveSvgAndPng(SamplesPath("financial_dashboard.svg"));
Console.WriteLine("Saved financial_dashboard.svg");

// --- 14. New indicators: WilliamsR, OBV, CCI, ParabolicSAR ---
Plt.Create()
    .WithTitle("Momentum and Volume Indicators")
    .WithSize(1000, 800)
    .WithGridSpec(2, 2)
    .TightLayout()   // required so the vertical gap between rows is measured from the
                     // top-row X-labels + bottom-row subplot titles — without it, the
                     // theme default gap is too small and labels collide with titles.
    .AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
    {
        ax.Plot(Enumerable.Range(0, close.Length).Select(i => (double)i).ToArray(), close);
        ax.ParabolicSar(high, low);
        ax.WithTitle("Parabolic SAR");
    })
    .AddSubPlot(new GridPosition(0, 1, 1, 2), ax =>
    {
        ax.WilliamsR(MatPlotLibNet.Indicators.OhlcvBars.Hlc(high, low, close), 14);
        ax.WithTitle("Williams %R");
    })
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
    {
        ax.Obv(close, vol);
        ax.WithTitle("On-Balance Volume");
    })
    .AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
    {
        ax.Cci(MatPlotLibNet.Indicators.OhlcvBars.Hlc(high, low, close), 20);
        ax.WithTitle("CCI(20)");
    })
    .SaveSvgAndPng(SamplesPath("phase_f_indicators.svg"));   // the file keeps its old name: pages link to it
Console.WriteLine("Saved phase_f_indicators.svg");

// --- 15. Contour with labels ---
double[] cx = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
double[] cy = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
var cz = new double[20, 20];
for (int r = 0; r < 20; r++)
    for (int c = 0; c < 20; c++)
        cz[r, c] = Math.Sin(cx[c]) * Math.Cos(cy[r]);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Contour with Labels")
        .Contour(cx, cy, cz, s =>
        {
            s.ShowLabels = true;
            s.LabelFormat = "F2";
            s.LabelFontSize = 9;
        })
        .WithColorMap("coolwarm"))
    .SaveSvgAndPng(SamplesPath("contour_labels.svg"));
Console.WriteLine("Saved contour_labels.svg");

// --- 16. Scientific paper template (150 DPI, hidden top/right spines, tight layout) ---
{
    double[] tSP = Enumerable.Range(0, 200).Select(i => i * 0.1).ToArray();
    double[] ySP = tSP.Select(t => Math.Exp(-0.15 * t) * Math.Cos(2 * t)).ToArray();
    FigureTemplates.ScientificPaper(
        ax => ax
            .Plot(tSP, ySP, s => { s.Label = @"$e^{-0.15t}\cos(2t)$"; s.LineWidth = 1.2; })
            .SetXLabel("t (s)")
            .SetYLabel("Amplitude")
            .WithLegend(LegendPosition.UpperRight),
        title: "Damped Oscillation")
    .SaveSvgAndPng(SamplesPath("scientific_paper.svg"));
    Console.WriteLine("Saved scientific_paper.svg");
}

// --- 17. Sparkline dashboard ---
FigureTemplates.SparklineDashboard(
    [
        new NamedSample("CPU %",    Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 100).ToArray()),
        new NamedSample("Memory %", Enumerable.Range(0, 60).Select(_ => 40 + rng.NextDouble() * 30).ToArray()),
        new NamedSample("Disk I/O", Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 500).ToArray()),
    ],
    title: "Server Metrics — Last 60s")
    .SaveSvgAndPng(SamplesPath("sparkline_dashboard.svg"));
Console.WriteLine("Saved sparkline_dashboard.svg");

// =====================================================================
// Date axes, math text, property cyclers and accessibility
// =====================================================================

// --- 18. Date axis — 90-day time series with AutoDateLocator ---
var startDate = new DateTime(2025, 1, 1);
DateTime[] dates = Enumerable.Range(0, 90)
    .Select(i => startDate.AddDays(i))
    .ToArray();
var rng2 = new Random(7);
double[] price2 = new double[90];
price2[0] = 50.0;
for (int i = 1; i < 90; i++)
    price2[i] = price2[i - 1] + (rng2.NextDouble() - 0.48) * 1.5;

Plt.Create()
    .WithTitle("Stock Price — Jan to Mar 2025")
    .WithSize(900, 400)
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel("Date")
        .SetYLabel("Price ($)")
        .Plot(dates, price2, line => { line.Color = Colors.Tab10Blue; line.Label = "ACME"; })
        .WithLegend(LegendPosition.UpperRight))
    .SaveSvgAndPng(SamplesPath("date_axis.svg"));
Console.WriteLine("Saved date_axis.svg");

// --- 19. Math text labels — Greek letters and super/subscript in titles and axes ---
double[] tMs = Enumerable.Range(0, 100).Select(i => i * 0.5).ToArray();
double[] decay = tMs.Select(t => Math.Exp(-t / 20.0) * Math.Cos(t * 0.4)).ToArray();
double[] noise = tMs.Select(t => Math.Sin(t * 1.3) * 0.3).ToArray();

Plt.Create()
    .WithTitle("$\\alpha$ decay and $\\beta$ noise — $\\omega = 0.4$ rad/ms")
    .WithSize(1000, 450)
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("R$^{2}$ = 0.97")
        .SetXLabel("$\\Delta t$ (ms)")
        .SetYLabel("$\\sigma$ (normalised)")
        .Plot(tMs, decay, line => { line.Color = Colors.Tab10Blue; line.Label = "$\\alpha$ decay"; })
        .WithLegend(LegendPosition.UpperRight))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Noise — $\\mu \\pm 2\\sigma$")
        .SetXLabel("$\\Delta t$ (ms)")
        .SetYLabel("Amplitude ($\\times 10^{-3}$)")
        .Plot(tMs, noise, line => { line.Color = Colors.Orange; line.Label = "$\\beta$ noise"; })
        .WithLegend(LegendPosition.UpperRight))
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("math_text.svg"));
Console.WriteLine("Saved math_text.svg");

// --- 20. PropCycler — multi-series chart with custom color + linestyle cycling ---
var cycler = new PropCyclerBuilder()
    .WithColors(Colors.Tab10Blue, Colors.Orange, Colors.Green, Colors.Red)
    .WithLineStyles(LineStyle.Solid, LineStyle.Dashed, LineStyle.Dotted, LineStyle.DashDot)
    .Build();

double[] cx2 = Enumerable.Range(0, 60).Select(i => i * 0.2).ToArray();

Plt.Create()
    .WithTitle("PropCycler — four series, cycling color + line style")
    .WithSize(900, 450)
    .WithTheme(Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build())
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.SetXLabel("x").SetYLabel("f(x)");
        ax.Plot(cx2, cx2.Select(v => Math.Sin(v)).ToArray(),           s => s.Label = "sin(x)");
        ax.Plot(cx2, cx2.Select(v => Math.Sin(v + 1.0)).ToArray(),     s => s.Label = "sin(x+1)");
        ax.Plot(cx2, cx2.Select(v => Math.Sin(v + 2.0)).ToArray(),     s => s.Label = "sin(x+2)");
        ax.Plot(cx2, cx2.Select(v => Math.Sin(v + 3.0)).ToArray(),     s => s.Label = "sin(x+3)");
        ax.WithLegend(LegendPosition.UpperRight);
    })
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("prop_cycler.svg"));
Console.WriteLine("Saved prop_cycler.svg");

// --- 21. Accessibility — color-blind safe theme + alt text + high-contrast ---
double[] ax = Enumerable.Range(0, 12).Select(i => (double)i).ToArray();
double[] revenue = [1.2, 1.5, 1.3, 1.8, 2.1, 2.0, 2.4, 2.7, 2.5, 3.0, 3.2, 3.5];
double[] cost    = [0.9, 1.1, 1.0, 1.3, 1.5, 1.4, 1.6, 1.9, 1.8, 2.1, 2.2, 2.4];

Plt.Create()
    .WithTitle("Monthly Revenue vs Cost (2025)")
    .WithAltText("Line chart: revenue and cost trends over 12 months of 2025")
    .WithDescription("Revenue grew from $1.2M to $3.5M. Cost grew from $0.9M to $2.4M. Margin expanded each quarter.")
    .WithTheme(Theme.ColorBlindSafe)
    .WithSize(900, 450)
    .AddSubPlot(1, 1, 1, a =>
    {
        a.SetXLabel("Month").SetYLabel("$ M");
        a.Plot(ax, revenue, s => { s.Label = "Revenue"; s.LineWidth = 2.5; });
        a.Plot(ax, cost,    s => { s.Label = "Cost";    s.LineWidth = 2.5; s.LineStyle = LineStyle.Dashed; });
        a.WithLegend(LegendPosition.UpperLeft);
    })
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("accessibility_colorblind.svg"));
Console.WriteLine("Saved accessibility_colorblind.svg");

Plt.Create()
    .WithTitle("High-Contrast: Revenue Trend")
    .WithAltText("High-contrast line chart showing monthly revenue for 2025")
    .WithTheme(Theme.HighContrast)
    .WithSize(900, 450)
    .AddSubPlot(1, 1, 1, a =>
    {
        a.SetXLabel("Month").SetYLabel("$ M");
        a.Plot(ax, revenue, s => { s.Label = "Revenue"; s.LineWidth = 3.0; });
        a.WithLegend(LegendPosition.UpperLeft);
    })
    .TightLayout()
    .SaveSvgAndPng(SamplesPath("accessibility_highcontrast.svg"));
Console.WriteLine("Saved accessibility_highcontrast.svg");

// =====================================================================
// --- 3-D samples ---
// =====================================================================

// 1. Surface plot with perspective camera + directional lighting
{
    int n = 30;
    double[] sx = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
    double[] sy = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
    double[,] sz = new double[n, n];
    for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
        {
            double r = Math.Sqrt(sx[i] * sx[i] + sy[j] * sy[j]);
            sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
        }

    Plt.Create()
        .WithTitle("3D Surface — sinc(r) with perspective + lighting")
        .WithSize(800, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: 35, azimuth: -50, distance: 6.0)
            .WithLighting(dx: 0.5, dy: -0.5, dz: 1.0, ambient: 0.3, diffuse: 0.7)
            .Surface(sx, sy, sz, s =>
            {
                s.ColorMap = ColorMaps.Plasma;
                s.ShowWireframe = false;
                s.Alpha = 1.0;
            }))
        .SaveSvgAndPng(SamplesPath("threed_surface_sinc.svg"));
    Console.WriteLine("Saved threed_surface_sinc.svg");
}

// 2. Scatter3D with custom elevation/azimuth
{
    var srng = new Random(42);
    double[] xs = Enumerable.Range(0, 100).Select(_ => srng.NextDouble() * 4 - 2).ToArray();
    double[] ys = Enumerable.Range(0, 100).Select(_ => srng.NextDouble() * 4 - 2).ToArray();
    double[] zs = xs.Zip(ys, (xi, yi) => xi * xi + yi * yi + srng.NextDouble() * 0.5).ToArray();

    Plt.Create()
        .WithTitle("3D Scatter — Paraboloid with noise")
        .WithSize(700, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: 25, azimuth: -70)
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .Scatter3D(xs, ys, zs, s =>
            {
                s.Color = Colors.CornflowerBlue;
                s.MarkerSize = 5;
            }))
        .SaveSvgAndPng(SamplesPath("threed_scatter3d_paraboloid.svg"));
    Console.WriteLine("Saved threed_scatter3d_paraboloid.svg");
}

// 3. Bar3D with interactive rotation enabled
{
    double[] bx = [0, 1, 2, 0, 1, 2];
    double[] by = [0, 0, 0, 1, 1, 1];
    double[] bz = [3, 5, 2, 4, 1, 6];

    Plt.Create()
        .WithTitle("3D Bar Chart — Interactive rotation")
        .WithSize(700, 550)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: 25, azimuth: -60)
            // Light tilted so the three visible faces (front, top, right) have widely
            // separated dot products — needed because matplotlib's _shade_colors formula
            // k = 0.65 + 0.35·dot has a narrow [0.3, 1.0] range; on a light base colour
            // like tomato the differences are subtle unless the light spans most of the
            // dot range. Chosen direction puts front≈+0.8 / top≈+0.45 / right≈-0.4 →
            // k values 0.93 / 0.81 / 0.51, visibly distinct.
            .WithLighting(dx: -0.4, dy: -0.8, dz: 0.45)
            .SetXLabel("X-axis")
            .SetYLabel("Y-axis")
            .SetZLabel("Z-axis")
            .Bar3D(bx, by, bz, s =>
            {
                s.Color = Colors.Tomato;
                s.BarWidth = 0.5;
            }))
        .SaveSvgAndPng(SamplesPath("threed_bar3d_interactive.svg"));
    Console.WriteLine("Saved threed_bar3d_interactive.svg");
}

// 4. Grouped 3D Bar — matplotlib-style "skyscraper" plot: one coloured row per Y,
// thin bars along the X axis. Demonstrates that multiple Bar3D series stack on the
// same axes and participate in shared depth sorting + auto data-range detection.
{
    var grng = new Random(7);
    const int BarsPerRow = 20;
    double[] xs = Enumerable.Range(0, BarsPerRow).Select(i => (double)i).ToArray();
    // Rows can be added in any order — the 3-D axes renderer runs a single shared
    // back-to-front depth sort across all Bar3D series so insertion order doesn't
    // matter. This is the fix that matplotlib still lacks for repeated ax.bar3d().
    BarRow[] rows =
    [
        new(0, Colors.Red),
        new(1, Colors.Green),
        new(2, Colors.Blue),
        new(3, Colors.Cyan),
        new(4, Colors.Gold),
    ];

    var plt = Plt.Create()
        .WithTitle("3D Bar Chart — Grouped rows (skyscraper)")
        .WithSize(780, 620)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithCamera(elevation: 25, azimuth: -60)
              .WithLighting(dx: -0.4, dy: -0.8, dz: 0.45)
              .SetXLabel("X")
              .SetYLabel("Y")
              .SetZLabel("Z");
            foreach (var (y, color) in rows)
            {
                double[] ys = Enumerable.Repeat(y, BarsPerRow).ToArray();
                double[] zs = Enumerable.Range(0, BarsPerRow).Select(_ => grng.NextDouble()).ToArray();
                ax.Bar3D(xs, ys, zs, s =>
                {
                    s.Color = color;
                    s.BarWidth = 0.4;
                });
            }
        });
    plt.SaveSvgAndPng(SamplesPath("threed_bar3d_grouped.svg"));
    Console.WriteLine("Saved threed_bar3d_grouped.svg");
}

// 5. Planar 3D bars — matplotlib's "Create 2D bar graphs in different planes". Each row is a
// PlanarBar3D series of flat translucent rectangles stacked on discrete Y planes. Demonstrates
// the three colour-lookup modes (per-Y via single Color, per-X via Colors[], and combined).
{
    var pprng = new Random(7);
    const int PBarsPerRow = 20;
    double[] pxs = Enumerable.Range(0, PBarsPerRow).Select(i => (double)i).ToArray();
    BarRow[] prows =
    [
        new(0, Colors.Red),
        new(1, Colors.Green),
        new(2, Colors.Blue),
        new(3, Colors.Cyan),
        new(4, Colors.Gold),
    ];

    var pplt = Plt.Create()
        .WithTitle("Planar 3D Bars — 2D bars in different Y planes (translucent)")
        .WithSize(780, 620)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithCamera(elevation: 25, azimuth: -60)
              .SetXLabel("X").SetYLabel("Y").SetZLabel("Z");
            foreach (var (y, planeColor) in prows)
            {
                double[] pys = Enumerable.Repeat(y, PBarsPerRow).ToArray();
                double[] pzs = Enumerable.Range(0, PBarsPerRow).Select(_ => pprng.NextDouble()).ToArray();
                ax.PlanarBar3D(pxs, pys, pzs, s =>
                {
                    s.Color    = planeColor;      // per-Y base colour
                    s.BarWidth = 0.8;
                    s.Alpha    = 0.8;             // matplotlib default translucency
                    // Per-X override on the blue plane only: bars at x>=10 become gold,
                    // demonstrating that Colors[] composes with the per-plane Color.
                    if (y == 2)
                        s.Colors = pxs.Select(x => x >= 10 ? Colors.Gold : planeColor).ToArray();
                });
            }
        });
    pplt.SaveSvgAndPng(SamplesPath("threed_planar_bars.svg"));
    Console.WriteLine("Saved threed_planar_bars.svg");
}

// 6. Planar 3D bars with per-X highlight — all bars at x=0 are dark cyan regardless of
// which plane they sit on. Same per-Y base colours, but Colors[0] is overridden on every
// plane to show how per-X lookup composes across the whole chart.
{
    var phrng = new Random(7);
    const int PhBarsPerRow = 20;
    double[] phxs = Enumerable.Range(0, PhBarsPerRow).Select(i => (double)i).ToArray();
    var darkCyan = Color.FromHex("#008B8B");
    BarRow[] phrows =
    [
        new(0, Colors.Red),
        new(1, Colors.Green),
        new(2, Colors.Blue),
        new(3, Colors.Cyan),
        new(4, Colors.Gold),
    ];

    var phplt = Plt.Create()
        .WithTitle("Planar 3D Bars — x=0 highlighted dark cyan across all planes")
        .WithSize(780, 620)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.WithCamera(elevation: 25, azimuth: -60)
              .SetXLabel("X").SetYLabel("Y").SetZLabel("Z");
            foreach (var (y, planeColor) in phrows)
            {
                double[] phys = Enumerable.Repeat(y, PhBarsPerRow).ToArray();
                double[] phzs = Enumerable.Range(0, PhBarsPerRow).Select(_ => phrng.NextDouble()).ToArray();
                ax.PlanarBar3D(phxs, phys, phzs, s =>
                {
                    s.Color    = planeColor;
                    s.BarWidth = 0.8;
                    s.Alpha    = 0.8;
                    // Per-X override: every bar where x == 0 becomes dark cyan,
                    // all other bars inherit the plane colour via Colors fallback.
                    s.Colors = phxs.Select(x => x == 0 ? darkCyan : planeColor).ToArray();
                });
            }
        });
    phplt.SaveSvgAndPng(SamplesPath("threed_planar_bars_x0_highlight.svg"));
    Console.WriteLine("Saved threed_planar_bars_x0_highlight.svg");
}

// =====================================================================
// Six more 3-D series, and math text
// =====================================================================

// 22. Line3D — helix polyline
{
    double[] t = Enumerable.Range(0, 200).Select(i => i * 0.1).ToArray();
    double[] lx = t.Select(v => Math.Cos(v)).ToArray();
    double[] ly = t.Select(v => Math.Sin(v)).ToArray();
    double[] lz = t.Select(v => v * 0.15).ToArray();

    Plt.Create()
        .WithTitle("Line3D — Helix")
        .WithSize(700, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot3D(lx, ly, lz, s => { s.Color = Colors.Tab10Blue; s.LineWidth = 1.5; })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 25, azimuth: -60))
        .SaveSvgAndPng(SamplesPath("threed_line3d_helix.svg"));
    Console.WriteLine("Saved threed_line3d_helix.svg");
}

// 23. Trisurf3D — Delaunay triangulated surface from scattered points
{
    var trng = new Random(42);
    int np = 200;
    double[] tx = Enumerable.Range(0, np).Select(_ => trng.NextDouble() * 4 - 2).ToArray();
    double[] ty = Enumerable.Range(0, np).Select(_ => trng.NextDouble() * 4 - 2).ToArray();
    double[] tz = tx.Zip(ty, (xi, yi) => Math.Sin(xi * xi + yi * yi)).ToArray();

    Plt.Create()
        .WithTitle("Trisurf3D — Delaunay triangulated surface")
        .WithSize(700, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Trisurf(tx, ty, tz, s =>
            {
                s.ColorMap = ColorMaps.Viridis;
                s.Alpha = 0.9;
                s.ShowWireframe = true;
            })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 30, azimuth: -45))
        .SaveSvgAndPng(SamplesPath("threed_trisurf.svg"));
    Console.WriteLine("Saved threed_trisurf.svg");
}

// 24. Contour3D — marching-squares contour lines in 3-D
{
    int cn = 30;
    double[] ccx = Enumerable.Range(0, cn).Select(i => -3.0 + 6.0 * i / (cn - 1)).ToArray();
    double[] ccy = Enumerable.Range(0, cn).Select(i => -3.0 + 6.0 * i / (cn - 1)).ToArray();
    var ccz = new double[cn, cn];
    for (int i = 0; i < cn; i++)
        for (int j = 0; j < cn; j++)
        {
            double r = Math.Sqrt(ccx[i] * ccx[i] + ccy[j] * ccy[j]);
            ccz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
        }

    Plt.Create()
        .WithTitle("Contour3D — sinc(r) contour lines")
        .WithSize(700, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Contour3D(ccx, ccy, ccz, s =>
            {
                s.Levels = 12;
                s.ColorMap = ColorMaps.Coolwarm;
                s.LineWidth = 1.5;
            })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 35, azimuth: -55))
        .SaveSvgAndPng(SamplesPath("threed_contour3d.svg"));
    Console.WriteLine("Saved threed_contour3d.svg");
}

// 25. Quiver3D — 3-D vector field
{
    var qpts = new List<(double x, double y, double z, double u, double v, double w)>();
    for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
            for (int k = -1; k <= 1; k++)
                qpts.Add((i, j, k, -i * 0.3, -j * 0.3, k * 0.5));

    Plt.Create()
        .WithTitle("Quiver3D — 3-D vector field")
        .WithSize(700, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Quiver3D(
                qpts.Select(p => p.x).ToArray(),
                qpts.Select(p => p.y).ToArray(),
                qpts.Select(p => p.z).ToArray(),
                qpts.Select(p => p.u).ToArray(),
                qpts.Select(p => p.v).ToArray(),
                qpts.Select(p => p.w).ToArray(),
                s => { s.Color = Colors.Red; s.ArrowLength = 0.25; })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 25, azimuth: -60))
        .SaveSvgAndPng(SamplesPath("threed_quiver3d.svg"));
    Console.WriteLine("Saved threed_quiver3d.svg");
}

// 26. Voxels — volumetric cubes with face culling
{
    int vs = 6;
    var filled = new bool[vs, vs, vs];
    // Create an L-shaped structure
    for (int i = 0; i < vs; i++)
        for (int j = 0; j < vs; j++)
        {
            filled[i, j, 0] = true;       // base layer
            if (i < 2) filled[i, j, 1] = true;  // short wall along x=0..1
        }
    // Add a pillar
    for (int k = 0; k < vs; k++)
        filled[0, 0, k] = true;

    Plt.Create()
        .WithTitle("Voxels — face-culled cubes")
        .WithSize(700, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Voxels(filled, s => { s.Color = Colors.Orange; s.Alpha = 0.85; })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 30, azimuth: -50))
        .SaveSvgAndPng(SamplesPath("threed_voxels.svg"));
    Console.WriteLine("Saved threed_voxels.svg");
}

// 27. Text3D — 3-D annotations on a surface
{
    int tn = 20;
    double[] tsx = Enumerable.Range(0, tn).Select(i => -2.0 + 4.0 * i / (tn - 1)).ToArray();
    double[] tsy = Enumerable.Range(0, tn).Select(i => -2.0 + 4.0 * i / (tn - 1)).ToArray();
    double[,] tsz = new double[tn, tn];
    for (int i = 0; i < tn; i++)
        for (int j = 0; j < tn; j++)
            tsz[i, j] = Math.Cos(tsx[i]) * Math.Sin(tsy[j]);

    Plt.Create()
        .WithTitle("Text3D — annotations on a surface")
        .WithSize(800, 600)
        .With3DRotation()
        .AddSubPlot(1, 1, 1, ax => ax
            .Surface(tsx, tsy, tsz, s => { s.ColorMap = ColorMaps.Plasma; s.Alpha = 0.7; })
            .Text3D(0, 0, 1.0, "Peak", s => { s.Color = Colors.Red; s.FontSize = 14; })
            .Text3D(-2, -2, -0.5, "Valley", s => { s.Color = Colors.Blue; s.FontSize = 12; })
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .WithCamera(elevation: 35, azimuth: -55))
        .SaveSvgAndPng(SamplesPath("threed_text3d.svg"));
    Console.WriteLine("Saved threed_text3d.svg");
}

// 28. MathText — fractions, sqrt, accents, font variants (the file keeps its old name: pages link to it)
{
    double[] mx = Enumerable.Range(0, 100).Select(i => i * 0.1).ToArray();
    double[] my1 = mx.Select(v => Math.Sin(v) * Math.Exp(-v / 10)).ToArray();
    double[] my2 = mx.Select(v => Math.Cos(v) * Math.Exp(-v / 10)).ToArray();

    Plt.Create()
        .WithTitle(@"MathText — $\frac{d}{dx}\sqrt{x^2+1}$ and $\hat{\alpha} \cdot \vec{F}$")
        .WithSize(1000, 500)
        .AddSubPlot(1, 2, 1, ax => ax
            .WithTitle(@"$\mathbf{y} = \frac{\mathrm{sin}(x)}{e^{x/10}}$")
            .SetXLabel(@"$\Delta t$ (s)")
            .SetYLabel(@"$\hat{y}$ (normalised)")
            .Plot(mx, my1, s => { s.Color = Colors.Tab10Blue; s.Label = @"$\mathrm{sin}$"; })
            .Plot(mx, my2, s => { s.Color = Colors.Orange; s.Label = @"$\mathrm{cos}$"; })
            .WithLegend(LegendPosition.UpperRight))
        .AddSubPlot(1, 2, 2, ax => ax
            .WithTitle(@"$\sqrt{x^2 + y^2} \leq \mathbb{R}$")
            .SetXLabel(@"$x \in \mathbb{R}$")
            .SetYLabel(@"$\bar{y} \pm \sigma$")
            .Plot(mx, my1, s => { s.Color = Colors.Green; s.Label = @"$\vec{v}$"; })
            .WithLegend(LegendPosition.UpperRight))
        .TightLayout()
        .SaveSvgAndPng(SamplesPath("math_text_v130.svg"));
    Console.WriteLine("Saved math_text_v130.svg");
}

// 7. Nested pie — inner disc of departments + outer ring of product breakdown.
// Uses the new AxesBuilder.NestedPie() wrapper (thin alias for Sunburst with InnerRadius=0).
{
    static TreeNode Leaf(string label, double value, string hex) =>
        new() { Label = label, Value = value, Color = Color.FromHex(hex) };
    static TreeNode Branch(string label, string hex, params TreeNode[] children) =>
        new() { Label = label, Color = Color.FromHex(hex), Children = children };

    var departments = new TreeNode
    {
        Label = "Revenue",
        Children = new[]
        {
            Branch("Electronics", "#4E79A7",
                Leaf("Phones",  42, "#6A95C1"),
                Leaf("Laptops", 28, "#80A8CE"),
                Leaf("Audio",   14, "#98BBDB")),
            Branch("Apparel", "#F28E2B",
                Leaf("Men",   22, "#F4A455"),
                Leaf("Women", 26, "#F6B97F"),
                Leaf("Kids",  12, "#F8CCA9")),
            Branch("Home & Garden", "#59A14F",
                Leaf("Furniture", 18, "#77B56F"),
                Leaf("Tools",     11, "#94C78F"),
                Leaf("Plants",     7, "#B2D9AF")),
            Branch("Grocery", "#E15759",
                Leaf("Fresh",  31, "#E77778"),
                Leaf("Pantry", 24, "#ED9798"),
                Leaf("Frozen", 13, "#F3B7B8")),
        },
    };

    Plt.Create()
        .WithTitle("Nested Pie — Revenue by department and product")
        .WithSize(720, 720)
        .AddSubPlot(1, 1, 1, ax => ax.NestedPie(departments))
        .SaveSvgAndPng(SamplesPath("nested_pie.svg"));
    Console.WriteLine("Saved nested_pie.svg");
}

// 8. Interactive treemap with drilldown — 3 levels deep, click a rect to zoom in.
{
    static TreeNode Leaf(string label, double value, string hex) =>
        new() { Label = label, Value = value, Color = Color.FromHex(hex) };
    static TreeNode Branch(string label, string hex, params TreeNode[] children) =>
        new() { Label = label, Color = Color.FromHex(hex), Children = children };

    var catalogue = new TreeNode
    {
        Label = "Catalogue",
        Children = new[]
        {
            Branch("Electronics", "#4E79A7",
                Branch("Phones", "#4E79A7",
                    Leaf("Flagship",  45, "#4E79A7"),
                    Leaf("Mid-range", 32, "#6A95C1"),
                    Leaf("Budget",    18, "#80A8CE")),
                Branch("Laptops", "#4E79A7",
                    Leaf("Gaming",       22, "#4E79A7"),
                    Leaf("Ultrabook",    29, "#6A95C1"),
                    Leaf("Workstation",  14, "#80A8CE"))),
            Branch("Apparel", "#F28E2B",
                Branch("Men", "#F28E2B",
                    Leaf("Shirts", 18, "#F28E2B"),
                    Leaf("Pants",  15, "#F4A455"),
                    Leaf("Shoes",  12, "#F6B97F")),
                Branch("Women", "#F28E2B",
                    Leaf("Dresses", 24, "#F28E2B"),
                    Leaf("Tops",    21, "#F4A455"),
                    Leaf("Shoes",   17, "#F6B97F"))),
            Branch("Grocery", "#E15759",
                Branch("Fresh", "#E15759",
                    Leaf("Produce",   28, "#E15759"),
                    Leaf("Bakery",    14, "#E77778"),
                    Leaf("Dairy",     19, "#ED9798")),
                Branch("Pantry", "#E15759",
                    Leaf("Canned",    11, "#E15759"),
                    Leaf("Dry goods", 13, "#E77778"),
                    Leaf("Snacks",    16, "#ED9798"))),
        },
    };

    Plt.Create()
        .WithTitle("Treemap — click a rectangle to drill down (Escape to zoom out)")
        .WithSize(900, 620)
        .WithTreemapDrilldown()
        .AddSubPlot(1, 1, 1, ax => ax.Treemap(catalogue, s => s.ShowLabels = true))
        .SaveSvgAndPng(SamplesPath("treemap_drilldown.svg"));
    Console.WriteLine("Saved treemap_drilldown.svg");
}

// 9. Sankey — process-industry product distribution (5-column cascade)
//    Raw materials → primary processing → intermediate products → warehousing → customers.
//    Demonstrates multi-column cascade, gradient link colouring, iterative relaxation
//    (20 passes here), and sub-labels carrying per-node tonnage.
{
    static SankeyNode N(string label, string ton, string hex) =>
        new(label, Color.FromHex(hex), SubLabel: ton);

    var nodes = new[]
    {
        // Column 0 — raw materials
        /* 0 */ N("Crude A",     "120 kt", "#6B8E23"),
        /* 1 */ N("Crude B",     "80 kt",  "#556B2F"),
        // Column 1 — primary processing
        /* 2 */ N("Distillation", "140 kt", "#1F77B4"),
        /* 3 */ N("Cracking",     "60 kt",  "#2E86AB"),
        // Column 2 — intermediate products
        /* 4 */ N("Gasoline",    "90 kt",  "#F4A261"),
        /* 5 */ N("Diesel",      "70 kt",  "#E76F51"),
        /* 6 */ N("Jet Fuel",    "40 kt",  "#2A9D8F"),
        // Column 3 — warehousing
        /* 7 */ N("North Hub",   "100 kt", "#8D99AE"),
        /* 8 */ N("South Hub",   "100 kt", "#2B2D42"),
        // Column 4 — customers
        /* 9 */ N("Automotive",  "90 kt",  "#EF476F"),
        /* 10 */ N("Aviation",   "40 kt",  "#06A0A7"),
        /* 11 */ N("Industrial", "70 kt",  "#FFD166"),
    };
    var links = new[]
    {
        new SankeyLink(0, 2, 100), new SankeyLink(0, 3, 20),
        new SankeyLink(1, 2, 40), new SankeyLink(1, 3, 40),
        new SankeyLink(2, 4, 70), new SankeyLink(2, 5, 50), new SankeyLink(2, 6, 20),
        new SankeyLink(3, 4, 20), new SankeyLink(3, 5, 20), new SankeyLink(3, 6, 20),
        new SankeyLink(4, 7, 50), new SankeyLink(4, 8, 40),
        new SankeyLink(5, 7, 30), new SankeyLink(5, 8, 40),
        new SankeyLink(6, 7, 20), new SankeyLink(6, 8, 20),
        new SankeyLink(7, 9, 50), new SankeyLink(7, 10, 20), new SankeyLink(7, 11, 30),
        new SankeyLink(8, 9, 40), new SankeyLink(8, 10, 20), new SankeyLink(8, 11, 40),
    };
    Plt.Create()
        .WithTitle("Sankey — Process industry product distribution (hover a node to isolate)")
        .WithSize(1000, 600)
        .WithSankeyHover()
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.NodeWidth = 24;
                s.NodePadding = 14;
                s.Iterations = 20;
                s.LinkColorMode = SankeyLinkColorMode.Gradient;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_process_distribution.svg"));
    Console.WriteLine("Saved sankey_process_distribution.svg");
}

// 10. Sankey — J&J-style Q1 income statement flow
//     Product lines → aggregated categories → Revenue → Gross profit / Cost → Net profit / Tax.
//     Demonstrates sub-labels for Y/Y change indicators coloured green (profit chain) and
//     red (cost chain) via SankeyNode.SubLabelColor. LinkColorMode = Source so flows inherit
//     the semantic colour of the node they originate from.
{
    var green = Color.FromHex("#2A9D55");
    var red = Color.FromHex("#D62828");
    static SankeyNode Profit(string label, string amount, string yoy, Color color) =>
        new(label, color, SubLabel: $"{amount}  {yoy}", SubLabelColor: Color.FromHex("#2A9D55"));
    static SankeyNode Cost(string label, string amount, string yoy, Color color) =>
        new(label, color, SubLabel: $"{amount}  {yoy}", SubLabelColor: Color.FromHex("#D62828"));

    var nodes = new SankeyNode[]
    {
        /* 0 */ Profit("Immunology",    "$4.6B",  "+2%",  green),
        /* 1 */ Profit("Oncology",      "$4.9B",  "+15%", green),
        /* 2 */ Profit("Neuroscience",  "$1.9B",  "+5%",  green),
        /* 3 */ Profit("Med Devices",   "$8.0B",  "+4%",  green),
        /* 4 */ Profit("Pharma",        "$11.4B", "+8%",  green),
        /* 5 */ Profit("MedTech",       "$8.0B",  "+4%",  green),
        /* 6 */ Profit("Revenue",       "$21.4B", "+2%",  green),
        /* 7 */ Profit("Gross Profit",  "$14.9B", "+3%",  green),
        /* 8 */ Cost  ("Cost of Sales", "$6.5B",  "+1%",  red),
        /* 9 */ Profit("Net Profit",    "$3.8B",  "+0%",  green),
        /* 10 */ Cost ("R&D",           "$3.5B",  "+0%",  red),
        /* 11 */ Cost ("SG&A",          "$5.6B",  "+5%",  red),
        /* 12 */ Cost ("Tax",           "$0.6B",  "-10%", red),
        /* 13 */ Cost ("Other",         "$1.4B",  "+3%",  red),
    };
    var links = new[]
    {
        // Product lines → Pharma
        new SankeyLink(0, 4, 4.6), new SankeyLink(1, 4, 4.9), new SankeyLink(2, 4, 1.9),
        // Med devices → MedTech
        new SankeyLink(3, 5, 8.0),
        // Categories → Revenue
        new SankeyLink(4, 6, 11.4), new SankeyLink(5, 6, 8.0),
        // Revenue → Gross Profit + Cost of Sales
        new SankeyLink(6, 7, 14.9), new SankeyLink(6, 8, 6.5),
        // Gross Profit → Net Profit + R&D + SG&A + Tax + Other
        new SankeyLink(7, 9, 3.8), new SankeyLink(7, 10, 3.5),
        new SankeyLink(7, 11, 5.6), new SankeyLink(7, 12, 0.6),
        new SankeyLink(7, 13, 1.4),
    };
    Plt.Create()
        .WithTitle("Sankey — Johnson & Johnson Q1 FY25 income statement")
        .WithSize(1100, 640)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.NodeWidth = 22;
                s.NodePadding = 16;
                s.Iterations = 20;
                s.LinkColorMode = SankeyLinkColorMode.Source;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_income_statement.svg"));
    Console.WriteLine("Saved sankey_income_statement.svg");
}

// 11. Sankey — customer-journey alluvial with explicit column pinning
//     Four time-steps (Home → Step 2 → Step 3 → Purchase), with the same page labels
//     reappearing across columns. Uses SankeyNode.Column to pin each node to its semantic
//     timestep regardless of link topology — needed because a direct Home → Home skip
//     link would otherwise make BFS put both at column 0.
{
    static SankeyNode S(string label, int col, string hex) =>
        new(label, Color.FromHex(hex), Column: col);

    var nodes = new[]
    {
        /* 0 */ S("Home",     0, "#1F77B4"),
        /* 1 */ S("Product",  1, "#FF7F0E"),
        /* 2 */ S("Category", 1, "#2CA02C"),
        /* 3 */ S("Home",     1, "#1F77B4"),
        /* 4 */ S("Cart",     2, "#D62728"),
        /* 5 */ S("Product",  2, "#FF7F0E"),
        /* 6 */ S("Home",     2, "#1F77B4"),
        /* 7 */ S("Purchase", 3, "#9467BD"),
        /* 8 */ S("Cart",     3, "#D62728"),
        /* 9 */ S("Home",     3, "#1F77B4"),
    };
    var links = new[]
    {
        // Home → Step 2 fan-out
        new SankeyLink(0, 1, 45), new SankeyLink(0, 2, 25), new SankeyLink(0, 3, 30),
        // Step 2 → Step 3
        new SankeyLink(1, 4, 22), new SankeyLink(1, 5, 15), new SankeyLink(1, 6, 8),
        new SankeyLink(2, 5, 12), new SankeyLink(2, 6, 13),
        new SankeyLink(3, 6, 20), new SankeyLink(3, 4, 10),
        // Step 3 → Step 4
        new SankeyLink(4, 7, 24), new SankeyLink(4, 8, 8),
        new SankeyLink(5, 7, 14), new SankeyLink(5, 8, 13),
        new SankeyLink(6, 9, 30), new SankeyLink(6, 7, 11),
    };
    Plt.Create()
        .WithTitle("Sankey — Customer journey alluvial (4 timesteps)")
        .WithSize(1000, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.NodeWidth = 20;
                s.NodePadding = 12;
                s.Iterations = 12;
                s.LinkColorMode = SankeyLinkColorMode.Gradient;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_customer_journey.svg"));
    Console.WriteLine("Saved sankey_customer_journey.svg");
}

// 12. Sankey — UN expense categories → agencies (2-column baseline)
//     Minimal 2-column Sankey: an expense category splits into the UN bodies that receive
//     the funds. Demonstrates outside labels, gradient links, and a clean
//     HideAllAxes() canvas for a presentation-quality figure.
{
    static SankeyNode C(string label, string hex) => new(label, Color.FromHex(hex));
    var nodes = new[]
    {
        /* 0 */ C("Peacekeeping",     "#4472C4"),
        /* 1 */ C("Dev. Assistance",  "#70AD47"),
        /* 2 */ C("Humanitarian",     "#ED7D31"),
        /* 3 */ C("Administration",   "#A5A5A5"),
        /* 4 */ C("Climate",          "#2A9D8F"),
        // column 1 — agencies
        /* 5 */ C("DPO",     "#2B4CBA"),
        /* 6 */ C("UNDP",    "#5B9E3F"),
        /* 7 */ C("UNHCR",   "#C96420"),
        /* 8 */ C("Secretariat", "#7F7F7F"),
        /* 9 */ C("UNEP",    "#1C7A70"),
        /* 10 */ C("WFP",    "#D14A50"),
    };
    var links = new[]
    {
        new SankeyLink(0, 5, 6.5),
        new SankeyLink(1, 6, 5.0),
        new SankeyLink(1, 5, 1.2),
        new SankeyLink(2, 7, 3.8),
        new SankeyLink(2, 10, 2.4),
        new SankeyLink(3, 8, 2.6),
        new SankeyLink(4, 9, 1.9),
        new SankeyLink(4, 6, 0.6),
    };
    Plt.Create()
        .WithTitle("Sankey — UN expense categories → agencies")
        .WithSize(1000, 560)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.NodeWidth = 22;
                s.NodePadding = 18;
                s.Iterations = 12;
                s.LinkColorMode = SankeyLinkColorMode.Gradient;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_un_expenses.svg"));
    Console.WriteLine("Saved sankey_un_expenses.svg");
}

// 13. Sankey — severity-cascade state transitions (4-column cascade)
//     Patient severity states over four time points. Dense many-to-many transitions
//     where relaxation iterations really earn their keep by minimising crossings.
{
    static SankeyNode S(string label, string hex) => new(label, Color.FromHex(hex));
    var nodes = new[]
    {
        // T0
        /* 0 */ S("Mild T0",    "#4CAF50"),
        /* 1 */ S("Mod T0",     "#FFC107"),
        /* 2 */ S("Severe T0",  "#F44336"),
        // T1
        /* 3 */ S("Mild T1",    "#4CAF50"),
        /* 4 */ S("Mod T1",     "#FFC107"),
        /* 5 */ S("Severe T1",  "#F44336"),
        /* 6 */ S("Recover T1", "#2196F3"),
        // T2
        /* 7 */ S("Mild T2",    "#4CAF50"),
        /* 8 */ S("Mod T2",     "#FFC107"),
        /* 9 */ S("Severe T2",  "#F44336"),
        /* 10 */ S("Recover T2","#2196F3"),
        // T3
        /* 11 */ S("Recover T3","#2196F3"),
        /* 12 */ S("Mod T3",    "#FFC107"),
        /* 13 */ S("Severe T3", "#F44336"),
    };
    var links = new[]
    {
        // T0 → T1
        new SankeyLink(0, 3, 40), new SankeyLink(0, 4, 8),  new SankeyLink(0, 6, 12),
        new SankeyLink(1, 3, 6),  new SankeyLink(1, 4, 22), new SankeyLink(1, 5, 7), new SankeyLink(1, 6, 5),
        new SankeyLink(2, 4, 4),  new SankeyLink(2, 5, 14), new SankeyLink(2, 6, 2),
        // T1 → T2
        new SankeyLink(3, 7, 30), new SankeyLink(3, 8, 6),  new SankeyLink(3, 10, 10),
        new SankeyLink(4, 7, 8),  new SankeyLink(4, 8, 14), new SankeyLink(4, 9, 5),  new SankeyLink(4, 10, 7),
        new SankeyLink(5, 8, 4),  new SankeyLink(5, 9, 11), new SankeyLink(5, 10, 6),
        new SankeyLink(6, 10, 19),
        // T2 → T3
        new SankeyLink(7, 11, 35), new SankeyLink(7, 12, 3),
        new SankeyLink(8, 11, 10), new SankeyLink(8, 12, 12), new SankeyLink(8, 13, 2),
        new SankeyLink(9, 12, 6),  new SankeyLink(9, 13, 10),
        new SankeyLink(10, 11, 42),
    };
    Plt.Create()
        .WithTitle("Sankey — Severity cascade across four timepoints")
        .WithSize(1100, 640)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.NodeWidth = 18;
                s.NodePadding = 10;
                s.Iterations = 24;
                s.LinkColorMode = SankeyLinkColorMode.Gradient;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_severity_cascade.svg"));
    Console.WriteLine("Saved sankey_severity_cascade.svg");
}

// 13b. Sankey — vertical orientation (flow top→bottom)
{
    static SankeyNode V(string label, string hex) => new(label, Color.FromHex(hex));
    var nodes = new[]
    {
        /* 0 */ V("Website",   "#1F77B4"),
        /* 1 */ V("Search",    "#FF7F0E"),
        /* 2 */ V("Social",    "#2CA02C"),
        /* 3 */ V("Signup",    "#D62728"),
        /* 4 */ V("Trial",     "#9467BD"),
        /* 5 */ V("Paid",      "#8C564B"),
    };
    var links = new[]
    {
        new SankeyLink(0, 3, 40),
        new SankeyLink(1, 3, 25),
        new SankeyLink(2, 3, 15),
        new SankeyLink(3, 4, 60),
        new SankeyLink(4, 5, 30),
    };
    Plt.Create()
        .WithTitle("Sankey — Vertical orientation (top→bottom conversion funnel)")
        .WithSize(700, 850)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .Sankey(nodes, links, s =>
            {
                s.Orient = SankeyOrientation.Vertical;
                s.NodeWidth = 18;
                s.NodePadding = 14;
                s.Iterations = 12;
                s.LinkColorMode = SankeyLinkColorMode.Gradient;
            }))
        .SaveSvgAndPng(SamplesPath("sankey_vertical.svg"));
    Console.WriteLine("Saved sankey_vertical.svg");
}

// 14. Outside legend — the constrained-layout engine now measures the legend box via
//     LegendMeasurer and reserves right-margin space for LegendPosition.OutsideRight (and
//     the three other outside positions). Without TightLayout() the legend would clip at
//     the figure edge; with it the margin expands to host the full box.
{
    double[] xo = Enumerable.Range(0, 100).Select(i => i * 10.0 / 99).ToArray();
    Plt.Create()
        .WithTitle("Outside legend — constrained layout reserves right margin")
        .WithSize(900, 500)
        .TightLayout()
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(xo, xo.Select(v => Math.Sin(v)).ToArray(), s => s.Label = "sin(x)");
            ax.Plot(xo, xo.Select(v => Math.Cos(v)).ToArray(), s => s.Label = "cos(x)");
            ax.Plot(xo, xo.Select(v => 0.5 * Math.Sin(2 * v)).ToArray(),
                s => s.Label = "½ sin(2x)");
            ax.Plot(xo, xo.Select(v => Math.Exp(-v / 5) * Math.Cos(v)).ToArray(),
                s => s.Label = "exp(-x/5)·cos(x)");
            ax.SetXLabel("x");
            ax.SetYLabel("f(x)");
            ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight, Title = "Series" });
        })
        .SaveSvgAndPng(SamplesPath("legend_outside.svg"));
    Console.WriteLine("Saved legend_outside.svg");
}

// --- 15. Cookbook images — pie, donut ---
{
    double[] sizes = [35, 25, 20, 12, 8];
    string[] labels = ["Python", "C#", "Java", "Go", "Rust"];
    Plt.Create()
        .WithTitle("Language Popularity")
        .WithSize(600, 600)
        .Pie(sizes, labels, s => { s.AutoPct = "%.0f%%"; s.Shadow = true; })
        .SaveSvgAndPng(SamplesPath("pie_chart.svg"));
    Console.WriteLine("Saved pie_chart");

    Plt.Create()
        .WithTitle("Revenue Split")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .Donut(sizes, labels, s => { s.InnerRadius = 0.4; }))
        .SaveSvgAndPng(SamplesPath("donut_chart.svg"));
    Console.WriteLine("Saved donut_chart");
}

// --- 16. Cookbook images — distribution ---
{
    var rngDist = new Random(42);
    double[] data = Enumerable.Range(0, 1000)
        .Select(_ => rngDist.NextDouble() * 6 + rngDist.NextDouble() * 6).ToArray();
    double[][] groups = [
        Enumerable.Range(0, 60).Select(_ => rngDist.NextDouble() * 10).ToArray(),
        Enumerable.Range(0, 60).Select(_ => rngDist.NextDouble() * 8 + 2).ToArray(),
        Enumerable.Range(0, 60).Select(_ => rngDist.NextDouble() * 12 - 1).ToArray(),
    ];

    Plt.Create()
        .WithTitle("Histogram")
        .AddSubPlot(1, 1, 1, ax => ax
            .Hist(data, 30, s => { s.Color = Colors.Teal; s.EdgeColor = Colors.White; s.Label = "Distribution"; })
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("histogram.svg"));
    Console.WriteLine("Saved histogram");

    Plt.Create()
        .WithTitle("Box Plot")
        .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(groups, s => s.Color = Colors.CornflowerBlue))
        .SaveSvgAndPng(SamplesPath("boxplot.svg"));
    Console.WriteLine("Saved boxplot");

    Plt.Create()
        .WithTitle("Violin Plot")
        .AddSubPlot(1, 1, 1, ax => ax.Violin(groups, s => { s.Color = Colors.RebeccaPurple; s.Alpha = 0.6; }))
        .SaveSvgAndPng(SamplesPath("violin.svg"));
    Console.WriteLine("Saved violin");
}

// --- 17. Cookbook images — polar ---
{
    double[] theta = Enumerable.Range(0, 100).Select(i => i * 2 * Math.PI / 100).ToArray();
    double[] r = theta.Select(t => 1 + Math.Cos(3 * t)).ToArray();

    Plt.Create()
        .WithTitle("Polar Line")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot(r, theta, s => { s.Color = Colors.Blue; s.LineWidth = 2; s.Label = "r = 1 + cos(3θ)"; })
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("polar_line.svg"));
    Console.WriteLine("Saved polar_line");

    string[] cats = ["Speed", "Power", "Defense", "Range", "Accuracy"];
    double[] v1 = [85, 70, 90, 60, 95];
    double[] v2 = [70, 95, 60, 80, 75];
    Plt.Create()
        .WithTitle("Radar Comparison")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Radar(cats, v1, s => { s.Color = Colors.Blue; s.Alpha = 0.2; s.Label = "Player 1"; });
            ax.Radar(cats, v2, s => { s.Color = Colors.Red; s.Alpha = 0.2; s.Label = "Player 2"; });
            ax.WithLegend();
        })
        .SaveSvgAndPng(SamplesPath("radar_comparison.svg"));
    Console.WriteLine("Saved radar_comparison");
}

// --- 18. Cookbook images — error bars ---
{
    double[] xe = [1, 2, 3, 4, 5];
    double[] ye = [2.1, 4.5, 3.2, 6.8, 5.1];
    double[] yerrLow = [0.3, 0.2, 0.5, 0.3, 0.4];
    double[] yerrHigh = [0.8, 0.5, 1.0, 0.6, 0.9];

    Plt.Create()
        .WithTitle("Asymmetric Error Bars")
        .AddSubPlot(1, 1, 1, ax => ax
            .Scatter(xe, ye, s => { s.Color = Colors.Red; s.MarkerSize = 10; s.Label = "Data"; })
            .ErrorBar(xe, ye, yerrLow, yerrHigh, s => { s.Color = Colors.Gray; s.CapSize = 5; })
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("error_bars.svg"));
    Console.WriteLine("Saved error_bars");
}

// --- 19. Cookbook images — broken axes ---
{
    double[] xb = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
    double[] yb = xb.Select(v => v < 10 ? v * 2 : v * 2 + 80).ToArray();

    Plt.Create()
        .WithTitle("Broken Y-Axis")
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(xb, yb, s => { s.Color = Colors.Blue; s.Label = "Data"; })
            .WithYBreak(25, 85)
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("broken_y.svg"));
    Console.WriteLine("Saved broken_y");
}

// --- 20. Cookbook images — symlog ---
{
    double[] xs = Enumerable.Range(-50, 101).Select(i => (double)i).ToArray();
    double[] ys = xs.Select(v => v * v * v).ToArray();

    Plt.Create()
        .WithTitle("Symlog Y-Axis — x³")
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(xs, ys, s => { s.Color = Colors.Blue; s.Label = "x³"; })
            .WithSymlogYScale(linthresh: 100)
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("symlog.svg"));
    Console.WriteLine("Saved symlog");
}

// --- 21. Cookbook images — themes comparison ---
//
// Themes are figure-scoped (matplotlib rcParams parity), so one figure cannot mix six
// themes. The previous version of this sample built six subplots in a single figure
// and just labelled each with the theme name — every subplot rendered identically
// because the theme was never applied (figure had no .WithTheme call). Result:
// six "Default" charts pretending to be six different themes.
//
// Honest fix: render each theme as a SEPARATE small figure, then composite the six
// images into a 2x3 grid SVG (and a matching PNG via SkiaSharp). Each tile is the
// real per-theme rendering, so users see actual visual differences.
{
    double[] xt = Enumerable.Range(0, 50).Select(i => i * 0.2).ToArray();
    double[] yt1 = xt.Select(v => Math.Sin(v)).ToArray();
    double[] yt2 = xt.Select(v => Math.Cos(v)).ToArray();

    var themes = new NamedTheme[]
    {
        new("Default", Theme.Default), new("Dark", Theme.Dark),
        new("Nord", Theme.Nord), new("Dracula", Theme.Dracula),
        new("Cyberpunk", Theme.Cyberpunk), new("Monokai", Theme.Monokai),
    };

    const int tileW = 400, tileH = 300;
    const int titleH = 40;
    const int cols = 3, rows = 2;
    int gridW = cols * tileW;
    int gridH = rows * tileH + titleH;

    // Per-tile SVG body (raw <svg> markup) — captured for both the composite SVG
    // and decoded back into bitmaps for the composite PNG.
    var tiles = new RenderedTile[themes.Length];
    for (int i = 0; i < themes.Length; i++)
    {
        var (name, theme) = themes[i];
        var fb = Plt.Create()
            .WithTitle(name)
            .WithTheme(theme)
            .WithSize(tileW, tileH)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot(xt, yt1, s => s.Label = "sin(x)");
                ax.Plot(xt, yt2, s => s.Label = "cos(x)");
                ax.WithLegend(LegendPosition.UpperRight);
            })
            .TightLayout();
        var fig = fb.Build();
        tiles[i] = new(name, fig.ToSvg(), fig.ToPng());
    }

    // Compose SVG: outer <svg> with title band + 6 nested <svg> at grid offsets.
    // Each child <svg> carries its own viewport so the embedded coordinates stay valid.
    var sb = new System.Text.StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{gridW}\" height=\"{gridH}\" viewBox=\"0 0 {gridW} {gridH}\">");
    sb.Append($"<rect width=\"{gridW}\" height=\"{gridH}\" fill=\"white\"/>");
    sb.Append($"<text x=\"{gridW / 2}\" y=\"26\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"18\" font-weight=\"bold\">Theme Comparison</text>");
    for (int i = 0; i < tiles.Length; i++)
    {
        int row = i / cols, col = i % cols;
        int tx = col * tileW, ty = titleH + row * tileH;
        // Strip the XML declaration from the tile SVG before embedding (otherwise the
        // composite document is malformed). The rest of the <svg .../> element is kept
        // verbatim and embedded inside a <g transform="translate(...)"> wrapper.
        var tileSvg = System.Text.RegularExpressions.Regex.Replace(tiles[i].Svg, @"<\?xml[^>]*\?>", "").Trim();
        sb.Append($"<g transform=\"translate({tx},{ty})\">");
        sb.Append(tileSvg);
        sb.Append("</g>");
    }
    sb.Append("</svg>");
    File.WriteAllText(SamplesPath("theme_comparison.svg"), sb.ToString());

    // PNG composite via SkiaSharp — decode each tile's PNG and draw into a single bitmap.
    using var bitmap = new SkiaSharp.SKBitmap(gridW, gridH);
    using (var canvas = new SkiaSharp.SKCanvas(bitmap))
    {
        canvas.Clear(SkiaSharp.SKColors.White);
        using var titleFont = new SkiaSharp.SKFont { Size = 18, Embolden = true };
        using var titlePaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, IsAntialias = true };
        canvas.DrawText("Theme Comparison", gridW / 2f, 26, SkiaSharp.SKTextAlign.Center, titleFont, titlePaint);
        for (int i = 0; i < tiles.Length; i++)
        {
            int row = i / cols, col = i % cols;
            using var tileBmp = SkiaSharp.SKBitmap.Decode(tiles[i].Png);
            canvas.DrawBitmap(tileBmp, col * tileW, titleH + row * tileH);
        }
    }
    using var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
    File.WriteAllBytes(SamplesPath("theme_comparison.png"), data.ToArray());

    Console.WriteLine("Saved theme_comparison (6 actual themes, composited)");
}

// --- 22. Cookbook images — geographic ---
{
    var proj = GeoProjection.Robinson;
    Plt.Create()
        .WithTitle("World Map — Robinson")
        .WithSize(1000, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(proj, Color.FromHex("#1a3a5c"))
            .Land(proj, Color.FromHex("#2d5a27"))
            .Coastlines(proj, Colors.White, lineWidth: 0.5)
            .Borders(proj, Color.FromHex("#888888"), lineWidth: 0.2))
        .SaveSvgAndPng(SamplesPath("geo_robinson.svg"));
    Console.WriteLine("Saved geo_robinson");

    var globe = GeoProjection.OrthographicAt(45, -30);
    Plt.Create()
        .WithTitle("Globe — 45°N, 30°W")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(globe)
            .Ocean(globe, Color.FromHex("#87CEEB"))
            .Land(globe, Color.FromHex("#90EE90"))
            .Coastlines(globe, Colors.Navy, lineWidth: 0.8))
        .SaveSvgAndPng(SamplesPath("geo_globe.svg"));
    Console.WriteLine("Saved geo_globe");
}

// --- 23. Cookbook images — pareto chart ---
// One call: the verb sorts the counts, accumulates the shares to exactly 100 and lines both Y axes up.
{
    string[] defects = ["Scratches", "Dents", "Cracks", "Stains", "Breaks", "Missing", "Warp"];
    double[] counts  = [142, 98, 67, 45, 31, 18, 9];

    Plt.Create()
        .WithTitle("Pareto Chart — Defect Analysis")
        .WithSize(900, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Pareto(defects, counts, parts => parts.Bars.Label = "Defect count"))
        .SaveSvgAndPng(SamplesPath("pareto_chart.svg"));
    Console.WriteLine("Saved pareto_chart");
}

// --- 24. Cookbook images — lollipop chart ---
// One stem series: each stem stands on its category, ends in its head, and the categories are named on the X axis.
{
    string[] productNames = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta"];
    double[] scores       = [82.5, 67.3, 91.1, 54.8, 76.4, 88.2];

    Plt.Create()
        .WithTitle("Lollipop Chart — Product Scores")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Lollipop(productNames, scores, s =>
            {
                s.StemColor = Color.FromHex("#5B9BD5");
                s.MarkerColor = Color.FromHex("#5B9BD5");
            })
            .SetYLabel("Score"))
        .SaveSvgAndPng(SamplesPath("lollipop_chart.svg"));
    Console.WriteLine("Saved lollipop_chart");
}

// --- 25. Cookbook images — P&L waterfall ---
{
    string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Total"];
    double[] pnl = [120, -45, 80, -30, 95, 60, -20, 110, -55, 75, 40, 130, 0];
    // Total = sum of monthly values
    pnl[12] = pnl[..12].Sum();

    Plt.Create()
        .WithTitle("Monthly P&L Waterfall")
        .WithSize(1100, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Waterfall(months, pnl, s =>
            {
                s.IncreaseColor = Color.FromHex("#2ECC71");
                s.DecreaseColor = Color.FromHex("#E74C3C");
                s.TotalColor = Color.FromHex("#3498DB");
                s.TotalIndices = [12];   // the last bar is a total: it stands on zero and wears the total colour
                s.BarWidth = 0.6;
            })
            .SetYLabel("P&L (€k)")
            .WithXTickLabelRotation(30))
        .SaveSvgAndPng(SamplesPath("waterfall_pnl.svg"));
    Console.WriteLine("Saved waterfall_pnl");
}

// --- 26. Cookbook images — ridge plot ---
// One call: one shared grid and one bandwidth for every ridge, one height scale, the names on the Y axis.
{
    var rngRidge = new Random(42);
    // Box-Muller: a normally distributed sample built from two uniform ones.
    double Gaussian(double mean, double sd) =>
        mean + sd * Math.Sqrt(-2 * Math.Log(1 - rngRidge.NextDouble())) * Math.Cos(2 * Math.PI * rngRidge.NextDouble());

    string[] names = ["Group A", "Group B", "Group C", "Group D", "Group E", "Group F"];
    double[] means = [-1.2, -0.4, 0.2, 0.7, 1.3, 1.9];
    NamedSample[] groups = [.. names.Select((name, g) =>
        new NamedSample(name, [.. Enumerable.Range(0, 200).Select(_ => Gaussian(means[g], 0.6))]))];

    Plt.Create()
        .WithTitle("Ridgeline")
        .WithSize(900, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .Ridgeline(groups, configure: ridge => ridge.Alpha = 0.7)
            .SetXLabel("Value"))
        .SaveSvgAndPng(SamplesPath("ridge_plot.svg"));
    Console.WriteLine("Saved ridge_plot");
}

// --- 27. Cookbook images — dumbbell chart ---
// One call: two dots per category, joined from the lower value to the higher one, whichever group is higher.
{
    string[] productNames = ["Product A", "Product B", "Product C", "Product D", "Product E", "Product F"];

    Plt.Create()
        .WithTitle("Dumbbell Chart — Before vs After")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Dumbbell(productNames,
                new BarGroup("Before", [62.0, 78.5, 55.0, 83.0, 70.0, 48.0]),
                new BarGroup("After",  [74.0, 82.0, 68.0, 79.5, 88.5, 61.0]))
            .SetYLabel("Score")
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("dumbbell_chart.svg"));
    Console.WriteLine("Saved dumbbell_chart");
}

// --- 28. Cookbook images — A/B test with CI ---
{
    string[] variants = ["Control", "Variant A", "Variant B"];
    double[] rates  = [0.121, 0.143, 0.158];
    double[] errLo  = [0.008, 0.009, 0.010];
    double[] errHi  = [0.008, 0.010, 0.011];
    double[] xIdx   = [0.5, 1.5, 2.5];

    Plt.Create()
        .WithTitle("A/B Test — Conversion Rate with 95% CI")
        .WithSize(750, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Bar(variants, rates, s => { s.Color = Color.FromHex("#5B9BD5"); s.Alpha = 0.7; })
            .ErrorBar(xIdx, rates, errLo, errHi, s => { s.Color = Colors.Black; s.CapSize = 6; s.LineWidth = 2; })
            .Annotate("p < 0.001", 2.0, 0.172, s => { s.Alignment = TextAlignment.Center; })
            .SetYLabel("Conversion Rate")
            .SetYLim(0, 0.20))
        .SaveSvgAndPng(SamplesPath("ab_test.svg"));
    Console.WriteLine("Saved ab_test");
}

// --- 29. Cookbook images — calendar heatmap ---
// One call: seven weekday rows by one column per week, Monday on top, weekdays and months named.
{
    var rngCal = new Random(7);
    var first = new DateTime(2025, 1, 1);
    DateTime[] days = [.. Enumerable.Range(0, 365).Select(d => first.AddDays(d))];
    double[] commits = [.. days.Select(d =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? rngCal.Next(0, 3) : rngCal.Next(0, 12))];

    // White to dark green; a day with no value is grey, so it does not read as a zero.
    var greens = new LinearColorMap("commits", [Color.FromHex("#FFFFFF"), Color.FromHex("#00441B")])
    {
        BadColor = Color.FromHex("#DDDDDD"),
    };

    Plt.Create()
        .WithTitle("Calendar Heatmap — Contributions")
        .WithSize(1100, 300)
        .AddSubPlot(1, 1, 1, ax => ax
            .CalendarHeatmap(days, commits, configure: s => s.ColorMap = greens))
        .SaveSvgAndPng(SamplesPath("calendar_heatmap.svg"));
    Console.WriteLine("Saved calendar_heatmap");
}

// --- 30. Cookbook images — wind rose ---
// A polar bar takes its angle in radians, counter-clockwise from the right (east); a compass bearing counts clockwise
// from north, so it is turned first. One series here; the next sample stacks the speeds outward.
{
    double[] bearings  = [0, 45, 90, 135, 180, 225, 270, 315];          // N, NE, E, SE, S, SW, W, NW
    double[] frequency = [9.5, 7.8, 12.8, 6.3, 9.4, 14.1, 10.1, 7.1];   // % of the hours, all speeds
    double[] theta = [.. bearings.Select(b => (90 - b) * Math.PI / 180)];

    Plt.Create()
        .WithTitle("Wind Rose — share of the hours by direction")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .PolarBar(frequency, theta, s =>
            {
                s.Color = Color.FromHex("#2E86C1");
                s.Alpha = 0.85;
                s.BarWidth = 0.9 * Math.PI / 4;   // radians: nine tenths of the 45° between two directions
            }))
        .SaveSvgAndPng(SamplesPath("wind_rose.svg"));
    Console.WriteLine("Saved wind_rose");
}

// --- 31. Cookbook images — bump / rank chart ---
// One call: one marked line per brand, rank 1 on top, each name at the end of its line, a tick at every whole rank.
{
    var rngBump = new Random(99);
    string[] brands = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];
    double[] periods = [.. Enumerable.Range(1, 8).Select(i => (double)i)];

    // One rank per period for each brand: each period shuffles the ranks 1 to 5.
    double[][] ranks = [.. brands.Select(_ => new double[periods.Length])];
    for (int p = 0; p < periods.Length; p++)
    {
        int[] order = [.. Enumerable.Range(1, brands.Length).OrderBy(_ => rngBump.Next())];
        for (int b = 0; b < brands.Length; b++) ranks[b][p] = order[b];
    }

    Plt.Create()
        .WithTitle("Bump Chart — Brand Ranking over Time")
        .WithSize(900, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .BumpChart(periods, [.. brands.Select((brand, b) => new BarGroup(brand, ranks[b]))])
            .SetXLabel("Period")
            .SetYLabel("Rank"))
        .SaveSvgAndPng(SamplesPath("bump_chart.svg"));
    Console.WriteLine("Saved bump_chart");
}

// --- 32. Cookbook images — gauge chart ---
{
    Plt.Create()
        .WithTitle("Gauge Chart — Customer Satisfaction KPI")
        .WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax
            .Gauge(72, s =>
            {
                s.Min = 0;
                s.Max = 100;
                s.NeedleColor = Colors.Black;
                s.Ranges =
                [
                    new GaugeBand(40,  Color.FromHex("#E74C3C")),
                    new GaugeBand(70,  Color.FromHex("#F39C12")),
                    new GaugeBand(100, Color.FromHex("#2ECC71")),
                ];
            }))
        .SaveSvgAndPng(SamplesPath("gauge_chart.svg"));
    Console.WriteLine("Saved gauge_chart");
}

// --- 33. Cookbook images — funnel chart ---
{
    string[] stages = ["Visitors", "Sign-ups", "Trials", "Qualified", "Closed"];
    double[] counts2 = [12_400, 4_800, 1_950, 720, 310];

    Plt.Create()
        .WithTitle("Funnel Chart — Sales Pipeline")
        .WithSize(700, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Funnel(stages, counts2, s =>
            {
                s.Colors = [
                    Color.FromHex("#2980B9"),
                    Color.FromHex("#27AE60"),
                    Color.FromHex("#F39C12"),
                    Color.FromHex("#E67E22"),
                    Color.FromHex("#E74C3C"),
                ];
            }))
        .SaveSvgAndPng(SamplesPath("funnel_chart.svg"));
    Console.WriteLine("Saved funnel_chart");
}

// --- 34. Cookbook images — scatter with marginals ---
{
    var rng4 = new Random(17);
    double BM4(Random r) { double u1 = 1 - r.NextDouble(), u2 = 1 - r.NextDouble(); return Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2); }
    double[] xs = Enumerable.Range(0, 200).Select(_ => BM4(rng4)).ToArray();
    double[] ys = xs.Select(x => 0.6 * x + BM4(rng4) * 0.8).ToArray();

    Plt.Create()
        .WithTitle("Scatter with Marginal Histograms")
        .WithSize(800, 700)
        .WithGridSpec(2, 2, heightRatios: [3.0, 1.0], widthRatios: [3.0, 1.0])
        .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax
            .Scatter(xs, ys, s => { s.Color = Color.FromHex("#2C3E50"); s.Alpha = 0.5; s.MarkerSize = 5; })
            .SetXLabel("X")
            .SetYLabel("Y"))
        .AddSubPlot(new GridPosition(0, 1, 1, 2), ax => ax
            .Hist(ys, bins: 20, s => { s.Color = Color.FromHex("#E74C3C"); })
            .SetXLabel("Count"))
        .AddSubPlot(new GridPosition(1, 2, 0, 1), ax => ax
            .Hist(xs, bins: 20, s => { s.Color = Color.FromHex("#3498DB"); })
            .SetYLabel("Count"))
        .SaveSvgAndPng(SamplesPath("scatter_marginals.svg"));
    Console.WriteLine("Saved scatter_marginals");
}

// --- 35. Cookbook images — 4-projection comparison ---
{
    var projections = new NamedProjection[]
    {
        new("Mercator",    GeoProjection.Mercator),
        new("Robinson",    GeoProjection.Robinson),
        new("Mollweide",   GeoProjection.Mollweide),
        new("Equal Earth", GeoProjection.EqualEarth),
    };

    var fig = Plt.Create()
        .WithTitle("World Map — Four Projections")
        .WithSize(1200, 700);

    for (int i = 0; i < projections.Length; i++)
    {
        int row = i / 2, col = i % 2;
        var (name, proj) = projections[i];
        fig.AddSubPlot(2, 2, i + 1, ax => ax
            .WithTitle(name)
            .WithProjection(proj)
            .Ocean(proj, Color.FromHex("#1a3a5c"))
            .Land(proj, Color.FromHex("#2d5a27"))
            .Coastlines(proj, Colors.White, lineWidth: 0.5)
            .Borders(proj, Color.FromHex("#888888"), lineWidth: 0.2));
    }

    fig.SaveSvgAndPng(SamplesPath("geo_projection_grid.svg"));
    Console.WriteLine("Saved geo_projection_grid");
}

// --- 36. Cookbook images — night-side globe ---
{
    var proj = GeoProjection.OrthographicAt(20, 10);

    Plt.Create()
        .WithTitle("Night-side Globe")
        .WithSize(700, 700)
        .WithTheme(Theme.Dark)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(proj, Color.FromHex("#0a1628"))
            .Land(proj, Color.FromHex("#1a3a1a"))
            .Coastlines(proj, Color.FromHex("#7fbbff"), lineWidth: 0.8)
            .Borders(proj, Color.FromHex("#3a6a8a"), lineWidth: 0.25))
        .SaveSvgAndPng(SamplesPath("geo_nightside.svg"));
    Console.WriteLine("Saved geo_nightside");
}

// --- 37. Cover picture — lead-lag network of crypto pairs ---
//
// A cover picture (the file keeps its old name: pages link to it). Renders a NetworkGraphSeries
// where directed edges represent Transfer-Entropy lead-lag scores between major
// crypto symbols. Force-directed layout with a fixed seed gives a deterministic,
// visually balanced composition. ColorScalar drives node fill via Plasma, and
// SizeScalar drives node radius — bigger and brighter = more central in the flow.
{
    // Node centrality + size proxies — BTC dominant, ETH secondary hub.
    GraphNode N(string id, double centrality) =>
        new(id, ColorScalar: centrality, SizeScalar: 0.6 + centrality * 0.8, Label: id);

    var nodes = new[]
    {
        N("BTC",   1.00),
        N("ETH",   0.80),
        N("SOL",   0.55),
        N("ADA",   0.45),
        N("MATIC", 0.40),
        N("AVAX",  0.35),
        N("DOT",   0.30),
        N("LINK",  0.25),
    };

    // Synthetic Transfer-Entropy weights — BTC leads most, ETH is the secondary
    // information hub, with a handful of cross-edges for visual richness.
    var edges = new[]
    {
        new GraphEdge("BTC",   "ETH",   Weight: 0.85, IsDirected: true),
        new GraphEdge("BTC",   "SOL",   Weight: 0.65, IsDirected: true),
        new GraphEdge("BTC",   "ADA",   Weight: 0.55, IsDirected: true),
        new GraphEdge("BTC",   "MATIC", Weight: 0.50, IsDirected: true),
        new GraphEdge("BTC",   "AVAX",  Weight: 0.45, IsDirected: true),
        new GraphEdge("ETH",   "SOL",   Weight: 0.55, IsDirected: true),
        new GraphEdge("ETH",   "MATIC", Weight: 0.45, IsDirected: true),
        new GraphEdge("ETH",   "DOT",   Weight: 0.40, IsDirected: true),
        new GraphEdge("ETH",   "LINK",  Weight: 0.35, IsDirected: true),
        new GraphEdge("SOL",   "AVAX",  Weight: 0.35, IsDirected: true),
        new GraphEdge("DOT",   "ADA",   Weight: 0.30, IsDirected: true),
        new GraphEdge("AVAX",  "MATIC", Weight: 0.25, IsDirected: true),
        new GraphEdge("LINK",  "ETH",   Weight: 0.20, IsDirected: true),
    };

    Plt.Create()
        .WithTitle("Crypto Lead-Lag Network  ·  Transfer Entropy  ·  MatPlotLibNet")
        .WithSize(1600, 900)
        .WithTheme(Theme.Seaborn)
        .AddSubPlot(1, 1, 1, ax => ax
            .HideAllAxes()
            .ShowGrid(false)
            .NetworkGraph(nodes, edges, s =>
            {
                s.Layout               = GraphLayout.ForceDirected;
                s.LayoutSeed           = 42;
                s.LayoutIterations     = 250;
                s.ConvergenceThreshold = 1e-4;
                s.ColorMap             = ColorMaps.Plasma;
                s.NodeRadiusScale      = 26.0;
                s.EdgeThicknessScale   = 7.0;
                s.ShowNodeLabels       = true;
                s.ShowEdgeWeights      = false;
            }))
        .SaveSvgAndPng(SamplesPath("medium-cover-v1-10-0.svg"));
    Console.WriteLine("Saved medium-cover-v1-10-0");
}

// =====================================================================
// RelativeRotationSeries (RRG): crypto coin rotation radar
// =====================================================================

// --- 38. Relative Rotation Graph — 5 alts vs BTC benchmark ---
// Simulates 104 weekly closes (2 years) for 5 alt-coins relative to BTC.
// DualEma formula (JdK canonical). Quadrant grid shows Leading/Weakening/
// Lagging/Improving sectors. TailLength = 8 shows the last 8 weeks of rotation.
{
    var rrgRng = new Random(99);

    // Simulate weekly BTC price (benchmark)
    double[] btc = new double[104];
    btc[0] = 40_000.0;
    for (int i = 1; i < 104; i++)
        btc[i] = Math.Max(1.0, btc[i - 1] * (1.0 + (rrgRng.NextDouble() - 0.48) * 0.06));

    // Simulate 5 alt-coin weekly closes — each with distinct drift/volatility
    AltCoin[] alts =
    [
        new("ETH",  0.0050, 0.08),
        new("BNB",  0.0030, 0.07),
        new("SOL",  0.0080, 0.10),
        new("ADA", -0.0010, 0.09),
        new("XRP",  0.0020, 0.07),
    ];

    double[][] assetCloses = alts.Select(a =>
    {
        var prices = new double[104];
        prices[0] = 100.0;
        for (int i = 1; i < 104; i++)
            prices[i] = Math.Max(0.001, prices[i - 1] * (1.0 + a.Drift + (rrgRng.NextDouble() - 0.5) * a.Vol));
        return prices;
    }).ToArray();

    string[] labels = alts.Select(a => a.Symbol).ToArray();

    Plt.Create()
        .WithTitle("Crypto Coin Rotation Radar  ·  Weekly  ·  MatPlotLibNet")
        .WithSize(900, 700)
        .AddSubPlot(1, 1, 1, ax => ax
            .SetXLabel("RS-Ratio (momentum strength)")
            .SetYLabel("RS-Momentum (momentum direction)")
            .RelativeRotation(assetCloses, btc, labels, s =>
            {
                s.TailLength        = 8;
                s.ShowQuadrantGrid  = true;
                s.ColorMap          = ColorMaps.Plasma;
            }))
        .SaveSvgAndPng(SamplesPath("rrg_crypto_rotation.svg"));
    Console.WriteLine("Saved rrg_crypto_rotation.svg");
}

// =====================================================================
// Ops dashboard: StatTiles, StateTimeline, Threshold + LegendValues
// =====================================================================

// --- 39. Ops dashboard mosaic — KPI tile row + service timeline + thresholded chart ---
//
// Demonstrates all four dashboard conveniences together in one mosaic:
// a KPI row of StatTileSeries, a StateTimelineSeries showing a day of service
// health, and a line chart with a breach Threshold(...) + WithLegendValues()
// so the legend shows the live reading alongside the series name. A tile is a number with a label, not a chart,
// so each tile axes hides its axes (HideAllAxes, the call Plt.OpsDashboard() makes for its own tiles).
{
    // 24h service-state timeline: mostly Up, one Degraded blip, one short outage.
    var serviceHistory = new StateSegment[]
    {
        new(0,  6,  "Up",       Colors.Tab10Green),
        new(6,  9,  "Degraded", Colors.Tab10Orange),
        new(9,  14, "Up",       Colors.Tab10Green),
        new(14, 15, "Down",     Colors.Red),
        new(15, 24, "Up",       Colors.Tab10Green),
    };

    // Half-hourly CPU load with a late-day ramp that breaches the 80% alarm threshold.
    double[] hours = Enumerable.Range(0, 48).Select(i => i * 0.5).ToArray();
    double[] load  = hours.Select(h => 55 + 15 * Math.Sin(h * 0.4) + (h > 20 ? 20 : 0)).ToArray();

    Plt.Create()
        .WithTitle("Ops Dashboard")
        .WithTheme(Theme.Dark)
        .WithSize(1000, 750)
        .WithGridSpec(3, 3, heightRatios: [1.0, 1.0, 1.6])
        .AddSubPlot(GridPosition.Single(0, 0), ax => ax
            .StatTile(12, t => { t.Label = "Participants"; t.Format = "0"; })
            .HideAllAxes())
        .AddSubPlot(GridPosition.Single(0, 1), ax => ax
            .StatTile(1, t => { t.Label = "Alerts"; t.AccentColor = Colors.Red; t.Format = "0"; })
            .HideAllAxes())
        .AddSubPlot(GridPosition.Single(0, 2), ax => ax
            .StatTile(99.4, t => { t.Label = "Uptime %"; t.Format = "0.0"; })
            .HideAllAxes())
        .AddSubPlot(new GridPosition(1, 2, 0, 3), ax => ax
            .StateTimeline(serviceHistory, s => s.Label = "API service")
            .WithTitle("Service state — last 24h"))
        .AddSubPlot(new GridPosition(2, 3, 0, 3), ax => ax
            .Plot(hours, load, s => s.Label = "CPU load %")
            .Threshold(80.0, Orientation.Horizontal, ThresholdBreach.Above, color: Colors.Red, label: "Alarm")
            .SetXLabel("Hour")
            .SetYLabel("Load %")
            // NOTE: WithLegend(...) replaces the Legend record wholesale, so it must be
            // called BEFORE WithLegendValues() — reversing the order silently resets
            // LegendValues back to false.
            .WithLegend()
            .WithLegendValues())
        .TightLayout()
        .SaveSvgAndPng(SamplesPath("ops_dashboard_full.svg"));
    Console.WriteLine("Saved ops_dashboard_full.svg");
}

// --- 40. International text: Arabic title, Hebrew legend entry, Latin ticks ---
// Arabic and Hebrew work with the bundled font: the letters join, the words read right to left, and the Latin
// axis label and the digits stay where a reader expects them. Other scripts need SkiaFonts.Register(path).
var international = Plt.Create()
    .WithTitle("درجة الحرارة")   // "temperature"
    .WithTheme(Theme.Seaborn)
    .WithSize(800, 500)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.SetXLabel("Quarter");
        ax.SetYLabel("°C");
        ax.Plot([1, 2, 3, 4], [12.5, 14.0, 13.2, 15.1], s => { s.Label = "תל אביב"; s.LineWidth = 2; });   // "Tel Aviv"
        ax.Plot([1, 2, 3, 4], [9.8, 11.2, 10.5, 12.0], s => { s.Label = "Amsterdam"; s.LineWidth = 2; });
        ax.WithLegend(LegendPosition.UpperLeft);
    })
    .Build();
international.Transform(new SvgTransform()).ToFile(SamplesPath("international.svg"));
international.Transform(new PngTransform()).ToFile(SamplesPath("international.png"));
Console.WriteLine("Saved international.svg and international.png");

// --- 41. Latency heatmap — a real clock across, latency buckets up, counts in the cells ---
//
// The chart a control room reads to answer "did the slow requests get slower, or are there just more of them".
// A bar chart of an average cannot answer it, and a percentile line hides the shape. Pcolormesh takes the edges
// of the buckets, so the clock stays a clock and the buckets stay milliseconds.
{
    var start = new DateTime(2026, 9, 12, 14, 0, 0, DateTimeKind.Utc);
    double[] minutes = [.. Enumerable.Range(0, 31).Select(i => start.AddMinutes(i).ToOADate())];
    double[] buckets = [1, 2, 5, 10, 25, 50, 100, 250, 500, 1000, 2500];   // ms, one edge more than there are rows

    var counts = new double[buckets.Length - 1, minutes.Length - 1];
    var latencyNoise = new Random(11);
    for (int minute = 0; minute < minutes.Length - 1; minute++)
    {
        // A normal ten minutes, then a slow patch that moves mass into the high buckets, then recovery.
        double centre = minute is >= 12 and < 20 ? 7.0 : 4.0;
        for (int bucket = 0; bucket < buckets.Length - 1; bucket++)
        {
            double distance = bucket - centre;
            counts[bucket, minute] = Math.Round(420 * Math.Exp(-distance * distance / 2.2) + (latencyNoise.NextDouble() * 12));
        }
    }

    Plt.Create()
        .WithTitle("Request latency — one column per minute")
        .WithTheme(Theme.OpsPanel)
        .WithSize(1000, 520)
        .AddSubPlot(1, 1, 1, ax => ax
            .Pcolormesh(minutes, buckets, counts, s => { s.ColorMap = PerceptualColorMaps2.Rocket; s.Label = "requests"; })
            .SetXDateAxis()
            .SetYScale(AxisScale.Log)
            .SetXLabel("Time (UTC)")
            .SetYLabel("Latency (ms)")
            .WithColorBar(cb => cb with { Label = "requests" }))
        .TightLayout()
        .SaveSvgAndPng(SamplesPath("latency_heatmap.svg"));
    Console.WriteLine("Saved latency_heatmap.svg");
}

// --- 42. Topology panel — the service map beside the tiles on a control-room wall ---
//
// Which service is talking to which, and which one is the sick one. AddTopology puts it on the dashboard
// between the timelines and the trend; a fixed layout seed draws the same picture on every refresh, so an
// operator learns where each node lives.
{
    var theme = Theme.OpsPanel;
    var now = new DateTime(2026, 9, 12, 14, 30, 0, DateTimeKind.Utc);

    var services = new List<GraphNode>
    {
        new("gateway",   Label: "gateway",   SizeScalar: 2.0, ColorScalar: 0.10),
        new("auth",      Label: "auth",      SizeScalar: 1.4, ColorScalar: 0.10),
        new("orders",    Label: "orders",    SizeScalar: 1.6, ColorScalar: 0.10),
        new("payments",  Label: "payments",  SizeScalar: 1.6, ColorScalar: 0.95),   // needs attention
        new("ledger",    Label: "ledger",    SizeScalar: 1.2, ColorScalar: 0.45),
        new("search",    Label: "search",    SizeScalar: 1.2, ColorScalar: 0.10),
        new("catalogue", Label: "catalogue", SizeScalar: 1.2, ColorScalar: 0.10),
        new("mail",      Label: "mail",      SizeScalar: 1.0, ColorScalar: 0.10),
    };

    var calls = new List<GraphEdge>
    {
        new("gateway", "auth", 3.0, IsDirected: true),
        new("gateway", "orders", 4.0, IsDirected: true),
        new("gateway", "search", 2.0, IsDirected: true),
        new("orders", "payments", 3.0, IsDirected: true),
        new("orders", "catalogue", 2.0, IsDirected: true),
        new("payments", "ledger", 2.0, IsDirected: true),
        new("orders", "mail", 1.0, IsDirected: true),
        new("search", "catalogue", 1.0, IsDirected: true),
    };

    double[] clock = [.. Enumerable.Range(0, 60).Select(i => now.AddSeconds(i - 60).ToOADate())];
    double[] published = [.. Enumerable.Range(0, 60).Select(i => 2400 + (120 * Math.Sin(i * 0.2)))];

    Plt.OpsDashboard()
        .WithTitle("Payments — federation")
        .WithWindow(now, TimeSpan.FromMinutes(1))
        .AddTile(8, t => { t.Label = "Services"; t.Caption = "7 normal · 1 critical"; })
        .AddTile(184.2, t =>
        {
            t.Label = "payments p99";
            t.Format = "0.0' ms'";
            t.Target = 120;
            t.Caption = "target 120 ms · +64 over";
            t.AccentColor = theme.Alarm.Critical;
        })
        .AddTile(0.4, t => { t.Label = "ledger lag"; t.Format = "0.0' s'"; t.Target = 1; t.Caption = "threshold 1 s"; })
        .AddTopology(services, calls, s =>
        {
            s.Layout = GraphLayout.ForceDirected;
            s.LayoutSeed = 42;                  // the same picture on every refresh
            s.LayoutIterations = 300;
            s.ColorMap = theme.Alarm.Ramp;
            s.NodeRadiusScale = 18.0;
            s.EdgeThicknessScale = 3.0;
            s.ShowNodeLabels = true;
        })
        .AddTrend(clock, published, s => s.Label = "published / s")
        .ConfigureTrend(ax => ax.SetYLabel("messages / s"))
        .Build()
        .WithTheme(theme)
        .SaveSvgAndPng(SamplesPath("ops_topology.svg"));
    Console.WriteLine("Saved ops_topology.svg");
}

// --- 43. The package icon — drawn by the library it stands for ---
//
// 128x128 PNG at the repository root, packed into all fifteen packages by Directory.Build.props. It is a
// pcolormesh under viridis with one bold trace over it: the two things this library is for, in the one colour
// map that says "scientific plotting" without a word. No text — at the 16 pixels a package list shows, a word
// is a smudge. Six cells to a side is as fine as it can go and still read there.
{
    const int cells = 6;
    double[] edges = [.. Enumerable.Range(0, cells + 1).Select(i => (double)i)];

    // A diagonal ramp with a soft peak up in the bright corner, so the map runs its whole length.
    var field = new double[cells, cells];
    for (int row = 0; row < cells; row++)
    {
        for (int col = 0; col < cells; col++)
        {
            double u = (col + 0.5) / cells;
            double v = (row + 0.5) / cells;
            field[row, col] = (0.55 * u) + (0.45 * v)
                + (0.35 * Math.Exp(-(((u - 0.72) * (u - 0.72)) + ((v - 0.7) * (v - 0.7))) / 0.06));
        }
    }

    double[] traceX = [.. Enumerable.Range(0, 193).Select(i => i * cells / 192.0)];
    double[] traceY = [.. traceX.Select(t => (cells * 0.5) + (cells * 0.26 * Math.Sin(t / cells * Math.PI * 1.5)))];

    Plt.Create()
        .WithSize(128, 128)
        .WithTheme(Theme.CreateFrom(Theme.Default).WithBackground(Color.FromHex("#0B0F14")).Build())
        .AddSubPlot(1, 1, 1, ax => ax
            .Pcolormesh(edges, edges, field, s => s.ColorMap = ColorMaps.Viridis)
            .Plot(traceX, traceY, s => { s.Color = Color.FromHex("#FFFFFF"); s.LineWidth = 6.0; })
            .HideAllAxes()
            .WithGrid(g => g with { Visible = false })
            .WithLegend(visible: false))
        // No margins and no gaps: the mark fills the square edge to edge, which is what makes it read as a
        // mark rather than as a tiny chart with a frame around it.
        .WithSubPlotSpacing(sp => sp with
        {
            MarginLeft = 0, MarginRight = 0, MarginTop = 0, MarginBottom = 0,
            HorizontalGap = 0, VerticalGap = 0,
        })
        .Build()
        .Transform(new PngTransform())
        .ToFile(RepoPath("icon.png"));
    Console.WriteLine("Saved icon.png");
}


// --- 44. The link preview card — the same mark, wide enough to read ---
//
// 1200x630 PNG in images/, which the docs site publishes and every page points its preview at. A link to the
// documentation posted in a chat, a forum or an issue is rendered from this file; without it the link shows as
// bare text. Same language as the package icon — a viridis field with one bold trace over it — but landscape,
// with the field kept dark on the left so the name can sit on top of it and still be read at thumbnail size.
{
    const int columns = 20;
    const int rows = 10;
    double[] xEdges = [.. Enumerable.Range(0, columns + 1).Select(i => (double)i)];
    double[] yEdges = [.. Enumerable.Range(0, rows + 1).Select(i => (double)i)];

    // The ramp runs left to right, so the left half stays dark enough to carry white text and the right half
    // shows what the colour map actually looks like.
    var field = new double[rows, columns];
    for (int row = 0; row < rows; row++)
    {
        for (int col = 0; col < columns; col++)
        {
            double u = (col + 0.5) / columns;
            double v = (row + 0.5) / rows;
            field[row, col] = (0.80 * u * u) + (0.18 * v)
                + (0.30 * Math.Exp(-(((u - 0.86) * (u - 0.86)) + ((v - 0.62) * (v - 0.62))) / 0.03));
        }
    }

    double[] traceX = [.. Enumerable.Range(0, 241).Select(i => i * columns / 240.0)];
    double[] traceY = [.. traceX.Select(t => (rows * 0.5) + (rows * 0.30 * Math.Sin(t / columns * Math.PI * 1.5)))];

    Plt.Create()
        .WithSize(1200, 630)
        .WithTheme(Theme.CreateFrom(Theme.Default).WithBackground(Color.FromHex("#0B0F14")).Build())
        .AddSubPlot(1, 1, 1, ax => ax
            .Pcolormesh(xEdges, yEdges, field, s => s.ColorMap = ColorMaps.Viridis)
            .Plot(traceX, traceY, s => { s.Color = Color.FromHex("#FFFFFF"); s.LineWidth = 7.0; })
            .Annotate("MatPlotLibNet", 0.05, 0.60, a =>
            {
                a.Coordinates = AnnotationCoordinates.AxesFraction;
                a.Font = new Font { Family = "sans-serif", Size = 86, Weight = FontWeight.Bold };
                a.Color = Color.FromHex("#FFFFFF");
            })
            .Annotate("matplotlib for .NET", 0.055, 0.45, a =>
            {
                a.Coordinates = AnnotationCoordinates.AxesFraction;
                a.Font = new Font { Family = "sans-serif", Size = 38 };
                a.Color = Color.FromHex("#9BE7C4");
            })
            .Annotate("83 chart types in C#  ·  SVG, PNG, PDF, GIF", 0.055, 0.31, a =>
            {
                a.Coordinates = AnnotationCoordinates.AxesFraction;
                a.Font = new Font { Family = "sans-serif", Size = 26 };
                a.Color = Color.FromHex("#C8D3DE");
            })
            .HideAllAxes()
            .WithGrid(g => g with { Visible = false })
            .WithLegend(visible: false))
        .WithSubPlotSpacing(sp => sp with
        {
            MarginLeft = 0, MarginRight = 0, MarginTop = 0, MarginBottom = 0,
            HorizontalGap = 0, VerticalGap = 0,
        })
        .Build()
        .Transform(new PngTransform())
        .ToFile(SamplesPath("social-card.png"));
    Console.WriteLine("Saved social-card.png");
}

// --- 45. Direct labels and axis colours — a line named where it ends, an axis in its line's colour ---
//
// The two pictures of the cookbook page "Direct labels and axis colours". WithDirectLabel() draws the name of the
// series added last at the end of its line and takes it out of the legend; SetYAxisColor colours an axis (spine,
// ticks, tick labels, title) like the line it measures. Both axes keep a title, so the colour is never the only cue.
{
    double[] months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    double[] web    = [42, 45, 47, 50, 53, 55, 54, 57, 59, 60, 62, 61];
    double[] app    = [12, 15, 19, 22, 27, 31, 36, 40, 45, 49, 54, 58];
    double[] store  = [30, 29, 31, 30, 28, 27, 29, 28, 26, 27, 25, 24];

    Plt.Create()
        .WithTitle("Visitors per month")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(months, web, s => s.Label = "Web").WithDirectLabel()
            .Plot(months, app, s => s.Label = "App").WithDirectLabel()
            .Plot(months, store, s => s.Label = "Store").WithDirectLabel()
            .SetXLabel("Month")
            .SetYLabel("Visitors (thousands)"))
        .SaveSvgAndPng(SamplesPath("direct_labels.svg"));
    Console.WriteLine("Saved direct_labels.svg");

    double[] hours       = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24];
    double[] temperature = [14.2, 13.5, 13.1, 14.0, 17.5, 21.8, 25.4, 27.1, 26.3, 22.9, 19.0, 16.4, 15.0];
    double[] humidity    = [88, 90, 91, 89, 78, 64, 52, 46, 49, 60, 72, 81, 85];
    var warm = Colors.Tab10Orange;
    var cool = Colors.Tab10Blue;

    Plt.Create()
        .WithTitle("Greenhouse, one day")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(hours, temperature, s => { s.Label = "Temperature"; s.Color = warm; })
            .WithDirectLabel()
            .SetXLabel("Hour of the day")
            .SetYLabel("Temperature (°C)")
            .SetYAxisColor(warm)
            .WithAddRightYAxis(right => right
                .Plot(hours, humidity, s => { s.Label = "Humidity"; s.Color = cool; })
                .WithDirectLabel()
                .SetYLabel("Relative humidity (%)")
                .SetYAxisColor(cool)))
        .SaveSvgAndPng(SamplesPath("direct_labels_dual_axis.svg"));
    Console.WriteLine("Saved direct_labels_dual_axis.svg");
}

// =====================================================================
// Horizontal bars, and the charts drawn by one call (the cookbook pictures of each)
// =====================================================================

// --- 46. Horizontal bars — values along X, the names on the Y axis, the first category at the bottom ---
// The products and sales of sample 9.
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(products, sales, s =>
        {
            s.Orientation = BarOrientation.Horizontal;
            s.Color = Colors.Teal;
        })
        .WithBarLabels("F0")
        .SetXMargin(0.1)                                // room past the longest bar for its label
        .SetXLabel("Revenue ($)"))
    .SaveSvgAndPng(SamplesPath("horizontal_bars.svg"));
Console.WriteLine("Saved horizontal_bars.svg");

// --- 47. Grouped horizontal bars — GroupedBar laid down by its one configuration action ---
{
    string[] quarters = ["Q1", "Q2", "Q3", "Q4"];
    BarGroup[] regions =
    [
        new("north", [12, 18, 15, 21]),
        new("south", [ 9, 14, 17, 16]),
        new("east",  [ 7, 11, 13, 19]),
    ];

    Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax
            .GroupedBar(quarters, regions, s => s.Orientation = BarOrientation.Horizontal)
            .WithLegend(LegendPosition.LowerRight))
        .SaveSvgAndPng(SamplesPath("grouped_horizontal_bars.svg"));
    Console.WriteLine("Saved grouped_horizontal_bars.svg");
}

// --- 48. Slope graph — one line per category between two moments, named at both ends ---
{
    string[] regions = ["North", "South", "East", "West"];

    Plt.Create()
        .WithTitle("Sales by region")
        .WithSize(700, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .SlopeGraph(regions,
                new BarGroup("2015", [42.0, 55.0, 38.0, 61.0]),
                new BarGroup("2025", [51.0, 49.0, 44.0, 66.0]))
            .SetYLabel("Sales"))
        .SaveSvgAndPng(SamplesPath("slope_graph.svg"));
    Console.WriteLine("Saved slope_graph.svg");
}

// --- 49. Dot plot — the categories down the Y axis, sorted by the first group, largest on top ---
{
    string[] languages = ["Ruby", "Go", "Rust", "Java", "Python"];

    Plt.Create()
        .WithTitle("Programming languages")
        .AddSubPlot(1, 1, 1, ax => ax
            .DotPlot(languages,
            [
                new BarGroup("2015", [31.0, 48.0, 62.0, 55.0, 80.0]),
                new BarGroup("2025", [35.0, 52.0, 60.0, 58.0, 84.0]),
            ])
            .SetXLabel("Share of developers (%)"))
        .SaveSvgAndPng(SamplesPath("dot_plot.svg"));
    Console.WriteLine("Saved dot_plot.svg");
}

// --- 50. Parallel coordinates — one axis per dimension, one line per row, one colour per class ---
{
    string[] measures = ["Sepal length", "Sepal width", "Petal length", "Petal width"];

    Plt.Create()
        .WithTitle("Three species of iris")
        .WithSize(900, 500)
        .TightLayout()
        .AddSubPlot(1, 1, 1, ax => ax
            .ParallelCoordinates(measures,
            [
                new BarGroup("setosa",     [5.1, 3.5, 1.4, 0.2]),
                new BarGroup("setosa",     [4.9, 3.0, 1.4, 0.2]),
                new BarGroup("setosa",     [4.7, 3.2, 1.3, 0.2]),
                new BarGroup("setosa",     [5.0, 3.6, 1.4, 0.2]),
                new BarGroup("versicolor", [7.0, 3.2, 4.7, 1.4]),
                new BarGroup("versicolor", [6.4, 3.2, 4.5, 1.5]),
                new BarGroup("versicolor", [6.9, 3.1, 4.9, 1.5]),
                new BarGroup("versicolor", [5.5, 2.3, 4.0, 1.3]),
                new BarGroup("virginica",  [6.3, 3.3, 6.0, 2.5]),
                new BarGroup("virginica",  [5.8, 2.7, 5.1, 1.9]),
                new BarGroup("virginica",  [7.1, 3.0, 5.9, 2.1]),
                new BarGroup("virginica",  [6.5, 3.0, 5.8, 2.2]),
            ])
            .WithLegend(LegendPosition.OutsideRight))
        .SaveSvgAndPng(SamplesPath("parallel_coordinates.svg"));
    Console.WriteLine("Saved parallel_coordinates.svg");
}

// --- 51. Marimekko — columns as wide as their share, each divided into its segments' shares ---
{
    string[] channels = ["Retail", "Online", "Wholesale"];

    Plt.Create()
        .WithTitle("Market by channel and brand")
        .WithSize(800, 500)
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
        .SaveSvgAndPng(SamplesPath("marimekko.svg"));
    Console.WriteLine("Saved marimekko.svg");
}

// --- 52. Q-Q plot — a sample against the normal distribution, with a least-squares reference line ---
{
    double[] dailyReturns = [0.8, -1.2, 0.3, 2.1, -0.4, 0.9, -2.6, 0.1, 1.4, -0.7, 0.5, -0.2,
                             1.1, -0.9, 0.6, -3.4, 0.2, 1.7, -0.5, 0.4];

    Plt.Create()
        .WithTitle("Daily returns against the normal distribution")
        .WithSize(700, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .QQPlot(dailyReturns, parts => parts.Points.Alpha = 0.8)
            .SetYLabel("Daily return (%)"))
        .SaveSvgAndPng(SamplesPath("qq_plot.svg"));
    Console.WriteLine("Saved qq_plot.svg");
}

// --- 53. Kaplan-Meier — the product-limit survival curve of each group ---
{
    SurvivalGroup[] arms =
    [
        new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
        new("Placebo",   [1, 2, 3, 4, 5, 8, 8, 11],      [true, true, true, false, true, true, true, true]),
    ];

    Plt.Create()
        .WithTitle("Time to relapse")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .KaplanMeier(arms)
            .SetXLabel("Weeks"))
        .SaveSvgAndPng(SamplesPath("kaplan_meier.svg"));
    Console.WriteLine("Saved kaplan_meier.svg");
}

// --- 54. Control chart — an individuals chart, its limits estimated, the values beyond them marked ---
{
    double[] day  = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
    double[] fill = [50.2, 49.8, 50.1, 50.4, 49.9, 50.0, 54.6, 50.3, 49.7, 50.1, 50.5, 49.6, 50.2, 45.6, 50.0];

    Plt.Create()
        .WithTitle("Fill weight")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .ControlChart(day, fill, configure: parts => parts.Violations.Label = "beyond a limit")
            .SetXLabel("Day")
            .SetYLabel("Weight (g)"))
        .SaveSvgAndPng(SamplesPath("control_chart.svg"));
    Console.WriteLine("Saved control_chart.svg");
}

// --- 55. Stream graph — a stacked area floating around the middle, one stream per layer ---
{
    double[] year = [2019, 2020, 2021, 2022, 2023];

    Plt.Create()
        .WithTitle("Listening by genre")
        .WithSize(800, 450)
        .TightLayout()
        .AddSubPlot(1, 1, 1, ax => ax
            .StreamGraph(year,
            [
                new BarGroup("Rock",       [8, 7, 6, 5, 4]),
                new BarGroup("Pop",        [3, 5, 7, 8, 9]),
                new BarGroup("Jazz",       [2, 2, 3, 3, 4]),
                new BarGroup("Electronic", [1, 2, 4, 6, 7]),
            ])
            .SetXTickLocator(new MultipleLocator(1))
            .SetXLabel("Year")
            .WithLegend(LegendPosition.OutsideRight))
        .SaveSvgAndPng(SamplesPath("stream_graph.svg"));
    Console.WriteLine("Saved stream_graph.svg");
}

// --- 56. Spy plot — a marker on every cell of a matrix that is not zero ---
{
    // A banded matrix: the diagonal and the two diagonals beside it, and one dense block.
    var band = new double[20, 20];
    for (int r = 0; r < 20; r++)
    {
        for (int c = Math.Max(0, r - 1); c <= Math.Min(19, r + 1); c++)
        {
            band[r, c] = 1;
        }
    }
    for (int r = 14; r < 18; r++)
    {
        for (int c = 2; c < 6; c++)
        {
            band[r, c] = -0.5;
        }
    }

    Plt.Create()
        .WithTitle("A tridiagonal matrix with one block")
        .WithSize(500, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Spy(band, configure: s => s.MarkerSize = 12))
        .SaveSvgAndPng(SamplesPath("spy_plot.svg"));
    Console.WriteLine("Saved spy_plot.svg");
}

// --- 57. Raincloud — a half violin, a box and a group's raw points, all in one colour ---
{
    NamedSample[] routes =
    [
        new("Highway", [22, 24, 23, 26, 25, 28, 24, 23, 27, 25, 30, 26]),
        new("Backroads", [18, 29, 15, 35, 20, 32, 17, 24, 21, 30, 16, 27]),
    ];

    Plt.Create()
        .WithTitle("Commute time by route")
        .WithSize(600, 400)
        .AddSubPlot(1, 1, 1, ax => ax
            .Raincloud(routes)
            .SetYLabel("Minutes"))
        .SaveSvgAndPng(SamplesPath("raincloud.svg"));
    Console.WriteLine("Saved raincloud.svg");
}

// --- 58. Horizon chart — a signed series folded into bands of two hues ---
{
    double[] day = [.. Enumerable.Range(0, 60).Select(i => (double)i)];
    double[] anomaly = [.. day.Select(d => 40 * Math.Sin(d / 5.0))];

    Plt.Create()
        .WithTitle("Temperature anomaly")
        .WithSize(900, 180)
        .AddSubPlot(1, 1, 1, ax => ax
            .Horizon(day, anomaly, bands: 4)
            .SetXLabel("Day"))
        .SaveSvgAndPng(SamplesPath("horizon_chart.svg"));
    Console.WriteLine("Saved horizon_chart.svg");
}

// --- 60. Cookbook images — a Likert scale: bars stacked on both sides of zero ---
// The answers that disagree are negative and grow down from zero, the ones that agree are positive and grow up from it, each
// kind from zero outward, so the eye reads the balance of a question at the middle line.
{
    string[] questions = ["Easy to use", "Fast", "Reliable", "Good value", "Would recommend"];
    double[] stronglyDisagree = [-6, -4, -3, -10, -5];
    double[] disagree = [-12, -8, -6, -15, -9];
    double[] neutral = [14, 12, 10, 18, 11];
    double[] agree = [41, 44, 47, 38, 43];
    double[] stronglyAgree = [27, 32, 34, 19, 32];

    Plt.Create()
        .WithTitle("How users rate the product (share of answers, %)")
        .WithSize(900, 480)
        .TightLayout()
        .AddSubPlot(1, 1, 1, ax => ax
            .Bar(questions, stronglyDisagree, s => { s.Label = "Strongly disagree"; s.Color = Color.FromHex("#B2182B"); })
            .Bar(questions, disagree, s => { s.Label = "Disagree"; s.Color = Color.FromHex("#EF8A62"); })
            .Bar(questions, neutral, s => { s.Label = "Neutral"; s.Color = Color.FromHex("#D9D9D9"); })
            .Bar(questions, agree, s => { s.Label = "Agree"; s.Color = Color.FromHex("#67A9CF"); })
            .Bar(questions, stronglyAgree, s => { s.Label = "Strongly agree"; s.Color = Color.FromHex("#2166AC"); })
            .SetBarMode(BarMode.Stacked)
            .AxHLine(0, line => { line.Color = Colors.Black; line.LineWidth = 1; })
            .SetYLabel("Share of answers (%)")
            .WithLegend(LegendPosition.OutsideRight))
        .SaveSvgAndPng(SamplesPath("likert_diverging_bars.svg"));
    Console.WriteLine("Saved likert_diverging_bars");
}

// --- 61. Cookbook images — a wind rose with the speeds stacked outward ---
// One polar bar series for each speed class on one axes, set to stack: each class starts where the one below it ends, and the
// rings read the total share of the hours in a direction.
{
    double[] bearings = [0, 45, 90, 135, 180, 225, 270, 315];
    double[] theta = [.. bearings.Select(b => (90 - b) * Math.PI / 180)];
    double[] calm = [3.1, 2.4, 4.0, 2.1, 3.0, 4.4, 3.3, 2.2];       // % of the hours, under 5 m/s
    double[] breeze = [4.2, 3.6, 5.5, 2.9, 4.1, 6.0, 4.5, 3.2];     // 5 to 10 m/s
    double[] strong = [2.2, 1.8, 3.3, 1.3, 2.3, 3.7, 2.3, 1.7];     // over 10 m/s

    Plt.Create()
        .WithTitle("Wind Rose — share of the hours by direction and speed")
        .WithSize(640, 640)
        .AddSubPlot(1, 1, 1, ax => ax
            .PolarBar(calm, theta, s => { s.Color = Color.FromHex("#9ECAE1"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "under 5 m/s"; })
            .PolarBar(breeze, theta, s => { s.Color = Color.FromHex("#3182BD"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "5 to 10 m/s"; })
            .PolarBar(strong, theta, s => { s.Color = Color.FromHex("#08306B"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "over 10 m/s"; })
            .SetBarMode(BarMode.Stacked)
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("wind_rose_stacked.svg"));
    Console.WriteLine("Saved wind_rose_stacked");
}

// --- 62. Cookbook images — a filled contour fills its grid, whatever the limits are ---
// The same field twice, cut to the same window: the fill follows the limits, and its edges are the lines.
{
    double[] gx = Enumerable.Range(0, 41).Select(i => -4.0 + i * 0.2).ToArray();
    double[] gy = Enumerable.Range(0, 41).Select(i => -4.0 + i * 0.2).ToArray();
    var gz = new double[41, 41];
    for (int r = 0; r < 41; r++)
        for (int c = 0; c < 41; c++)
            gz[r, c] = Math.Exp(-((gx[c] - 0.8) * (gx[c] - 0.8) + (gy[r] + 0.4) * (gy[r] + 0.4)) / 3)
                       - 0.6 * Math.Exp(-((gx[c] + 1.4) * (gx[c] + 1.4) + (gy[r] - 1.2) * (gy[r] - 1.2)) / 2);

    Plt.Create()
        .WithTitle("A filled contour cut to a window")
        .WithSize(1000, 460)
        .AddSubPlot(1, 2, 1, ax => ax
            .WithTitle("Contour, Filled")
            .Contour(gx, gy, gz, s => { s.Filled = true; s.Levels = 8; })
            .SetXLim(-2, 2).SetYLim(-2, 2)
            .WithColorMap("viridis"))
        .AddSubPlot(1, 2, 2, ax => ax
            .WithTitle("Contourf")
            .Contourf(gx, gy, gz, s => s.Levels = 8)
            .SetXLim(-2, 2).SetYLim(-2, 2)
            .WithColorMap("viridis"))
        .SaveSvgAndPng(SamplesPath("contour_filled_limits.svg"));
    Console.WriteLine("Saved contour_filled_limits");
}

// --- 63. Cookbook images — a stack plot with named layers and a line after it ---
// The layers are legend entries of their own, each in its colour, and the line drawn after the stack wears the next
// colour of the cycle, not one a layer already wears.
{
    double[] months = Enumerable.Range(1, 12).Select(m => (double)m).ToArray();
    double[][] layers =
    [
        [.. months.Select(m => 20 + 2 * m)],
        [.. months.Select(m => 15 + 3 * Math.Sin(m / 2.0) + m)],
        [.. months.Select(m => 10 + 1.5 * m)],
    ];
    double[] target = [.. months.Select(m => 55 + 3.5 * m)];

    Plt.Create()
        .WithTitle("Revenue by product line against the target")
        .WithSize(900, 480)
        .AddSubPlot(1, 1, 1, ax => ax
            .StackPlot(months, layers, s => s.Labels = ["Hardware", "Software", "Services"])
            .Plot(months, target, s => { s.Label = "Target"; s.LineWidth = 2; })
            .SetXLabel("Month")
            .SetYLabel("Revenue (k€)")
            .WithLegend(LegendPosition.UpperLeft))
        .SaveSvgAndPng(SamplesPath("stackplot_legend_line.svg"));
    Console.WriteLine("Saved stackplot_legend_line");
}

// --- 64. Cookbook images — a map coloured by value, with a colour bar ---
// Each country takes the colour its value has in the colour map; the countries without a value (here: none the 110m data
// lacks) would take the map's bad colour. The values are the base-10 logarithm of the population the data carries.
{
    var proj = GeoProjection.Robinson;
    var populationOf = NaturalEarth110m.Countries()
        .Where(country => country.NumberOf("POP_EST") > 0)
        .ToDictionary(country => country.Properties["ADM0_A3"], country => Math.Log10(country.NumberOf("POP_EST")));

    Plt.Create()
        .WithTitle("Population by country (log10)")
        .WithSize(1000, 560)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(Color.FromHex("#dfeaf5"))
            .Choropleth(populationOf, ColorMaps.Plasma)
            .WithColorBar()
            .WithLegend(visible: false))
        .SaveSvgAndPng(SamplesPath("choropleth_world.svg"));
    Console.WriteLine("Saved choropleth_world");
}

// --- 65. Cookbook images — bubbles on a map ---
// A bubble stands at the projected position of a place and its area is in proportion to its value.
{
    var proj = GeoProjection.Robinson;
    double[] cityLat = [35.68, 28.61, 31.23, -23.55, 19.43, 30.04, 19.08, 39.90, 23.81, 40.71, 6.52, 41.01, 51.51, -33.87];
    double[] cityLon = [139.69, 77.21, 121.47, -46.63, -99.13, 31.24, 72.88, 116.41, 90.41, -74.01, 3.38, 28.98, -0.13, 151.21];
    double[] cityMillions = [37.4, 31.0, 27.1, 22.0, 21.8, 21.3, 20.7, 20.5, 21.0, 18.8, 15.4, 15.5, 9.5, 5.3];

    Plt.Create()
        .WithTitle("Largest cities — population in millions")
        .WithSize(1000, 520)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(Color.FromHex("#1a3a5c"))
            .Land(Color.FromHex("#2d5a27"))
            .Borders(Color.FromHex("#4c7a45"), 0.2)
            .Bubbles(cityLat, cityLon, cityMillions, s => { s.Color = Color.FromHex("#FFB000"); s.Alpha = 0.75; })
            .WithLegend(visible: false))
        .SaveSvgAndPng(SamplesPath("bubble_map.svg"));
    Console.WriteLine("Saved bubble_map");
}

// --- 66. Cookbook images — flows between places on a map ---
// A flow follows the great circle between its ends and is as wide as its quantity; the one over the Pacific is cut where
// it leaves the map on one side and starts again on the other.
{
    var proj = GeoProjection.Robinson;
    GeoFlow[] routes =
    [
        new(new GeoCoordinate(35.68, 139.69), new GeoCoordinate(37.77, -122.42), 9),   // Tokyo to San Francisco
        new(new GeoCoordinate(51.51, -0.13), new GeoCoordinate(40.71, -74.01), 7),     // London to New York
        new(new GeoCoordinate(-33.87, 151.21), new GeoCoordinate(51.51, -0.13), 4),    // Sydney to London
        new(new GeoCoordinate(-23.55, -46.63), new GeoCoordinate(6.52, 3.38), 3),      // São Paulo to Lagos
        new(new GeoCoordinate(31.23, 121.47), new GeoCoordinate(25.2, 55.27), 5),      // Shanghai to Dubai
    ];

    Plt.Create()
        .WithTitle("Freight routes — width by tonnes")
        .WithSize(1000, 520)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(Color.FromHex("#1a3a5c"))
            .Land(Color.FromHex("#2d5a27"))
            .Flows(routes, Color.FromHex("#FFB000"), maxWidth: 4)
            .WithLegend(visible: false))
        .SaveSvgAndPng(SamplesPath("flow_map.svg"));
    Console.WriteLine("Saved flow_map");
}

// --- 67. Cookbook images — regions resized by a value, and a bubble at each ---
{
    var proj = GeoProjection.Robinson;
    var populationOf = NaturalEarth110m.Countries()
        .Where(country => country.NumberOf("POP_EST") > 0)
        .ToDictionary(country => country.Properties["ADM0_A3"], country => country.NumberOf("POP_EST"));

    Plt.Create()
        .WithTitle("Population — countries resized, and a bubble at each")
        .WithSize(1000, 960)
        .AddSubPlot(2, 1, 1, ax => ax
            .WithProjection(proj)
            .Ocean(Color.FromHex("#dfeaf5"))
            .Cartogram(populationOf, s => { s.Color = Color.FromHex("#d95f02"); s.StrokeColor = Colors.White; })
            .WithLegend(visible: false))
        .AddSubPlot(2, 1, 2, ax => ax
            .WithProjection(proj)
            .Ocean(Color.FromHex("#dfeaf5"))
            .Land(Color.FromHex("#cfcfcf"))
            .Dorling(populationOf, s => { s.Color = Color.FromHex("#d95f02"); s.Alpha = 0.7; })
            .WithLegend(visible: false))
        .SaveSvgAndPng(SamplesPath("cartogram_dorling.svg"));
    Console.WriteLine("Saved cartogram_dorling");
}

// --- 68. Cookbook images — a sample against the exponential distribution ---
{
    double[] waits = [2.1, 0.4, 5.6, 1.3, 0.9, 3.8, 0.2, 1.7, 2.6, 0.6, 8.4, 1.1,
                      0.7, 4.2, 1.9, 0.3, 3.1, 1.5, 0.8, 2.3];

    Plt.Create()
        .WithTitle("Minutes between arrivals against the exponential distribution")
        .WithSize(700, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .ProbabilityPlot(waits, QQReference.Exponential, parts => parts.Points.Alpha = 0.8)
            .SetYLabel("Minutes between arrivals"))
        .SaveSvgAndPng(SamplesPath("probability_plot.svg"));
    Console.WriteLine("Saved probability_plot.svg");
}

// --- 69. Cookbook images — a survival curve with the number at risk and the log-rank test under it ---
{
    SurvivalGroup[] arms =
    [
        new("Treatment", [6, 7, 10, 15, 19, 25, 32, 34], [true, true, false, true, false, true, false, false]),
        new("Placebo",   [1, 2, 3, 4, 5, 8, 8, 11],      [true, true, true, false, true, true, true, true]),
    ];

    Plt.Create()
        .WithTitle("Time to relapse")
        .WithSize(800, 600)
        .WithGridSpec(2, 1, heightRatios: [4, 1])
        .AddSubPlot(GridPosition.Single(0, 0), ax => ax.KaplanMeier(arms).WithLegend(), key: "survival")
        .AddSubPlot(GridPosition.Single(1, 0), ax => ax.ShareX("survival").RiskTable(arms).SetXLabel("Weeks"))
        .SaveSvgAndPng(SamplesPath("risk_table.svg"));
    Console.WriteLine("Saved risk_table.svg");
}

// --- 70. Cookbook images — an X-bar chart held to the limits of an earlier period, marked by Nelson's tests ---
{
    // Twenty subgroups of five fills; from the eleventh on the process drifts up by half a gram.
    double[][] subgroups = [.. Enumerable.Range(0, 20).Select(i =>
        Enumerable.Range(0, 5).Select(j => 50 + ((i * 7 + j * 3) % 9 - 4) * 0.25 + (i >= 10 ? 0.5 : 0)).ToArray())];

    Plt.Create()
        .WithTitle("Fill weight, mean of five")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .ShewhartChart(ControlChartData.XBar(subgroups), ControlRules.Nelson1 | ControlRules.Nelson2,
                new ControlLimits(Center: 50, Lower: 49.2, Upper: 50.8),
                parts => { parts.Values.Label = "subgroup mean"; parts.Violations.Label = "out of control"; })
            .SetXLabel("Subgroup")
            .SetXTickLocator(new MultipleLocator(5))
            .SetYLabel("Weight (g)")
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("xbar_chart.svg"));
    Console.WriteLine("Saved xbar_chart.svg");
}

// --- 71. Cookbook images — where the volume traded, and a VWAP anchored at a bar ---
{
    var walk = new Random(7);
    int barCount = 60;
    double[] closes = new double[barCount], highs = new double[barCount], lows = new double[barCount], volumes = new double[barCount];
    double level = 100;
    for (int i = 0; i < barCount; i++)
    {
        level += (walk.NextDouble() - 0.5) * 2;
        closes[i] = level;
        highs[i] = level + walk.NextDouble();
        lows[i] = level - walk.NextDouble();
        volumes[i] = 1000 + walk.Next(2000);
    }

    double[] barIndex = [.. Enumerable.Range(0, barCount).Select(i => (double)i)];
    Plt.Create()
        .WithTitle("Volume by price, and the VWAP since bar 30")
        .WithSize(900, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(barIndex, closes, line => line.Label = "Close")
            .Vwap(MatPlotLibNet.Indicators.OhlcvBars.Hlcv(highs, lows, closes, volumes), anchor: 30, vwap => vwap.Label = "VWAP from bar 30")
            .VolumeProfile(MatPlotLibNet.Indicators.OhlcvBars.Cv(closes, volumes))
            .SetXLabel("Bar")
            .SetYLabel("Price")
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("volume_profile.svg"));
    Console.WriteLine("Saved volume_profile.svg");
}

// --- 72. Cookbook images — a colour bar whose ticks stand where the log normaliser puts their colours ---
{
    double[,] counts = new double[6, 8];
    for (int r = 0; r < 6; r++)
        for (int c = 0; c < 8; c++)
            counts[r, c] = Math.Pow(10, 2 + (r * 8 + c) / 48.0);

    Plt.Create()
        .WithTitle("Requests per cell, 100 to 1000 on a log scale")
        .WithSize(700, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(counts)
            .WithColorMap("plasma")
            .WithNormalizer(new LogNormalizer())
            .WithColorBar(cb => cb with { Label = "requests" }))
        .SaveSvgAndPng(SamplesPath("heatmap_log_colorbar.svg"));
    Console.WriteLine("Saved heatmap_log_colorbar.svg");
}

// --- 73. Cookbook images — the same bars under two lights, one with no ambient part and one with no direct part ---
{
    double[] barColumn = [0, 1, 2, 0, 1, 2];
    double[] barRow = [0, 0, 0, 1, 1, 1];
    double[] barHeight = [3, 5, 2, 4, 6, 1];

    Plt.Create()
        .WithTitle("A light with no ambient part: ambient 0, diffuse 1")
        .WithSize(640, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: 25, azimuth: -60)
            .WithLighting(dx: -0.4, dy: -0.8, dz: 0.45, ambient: 0.0, diffuse: 1.0)
            .Bar3D(barColumn, barRow, barHeight, s => { s.Color = Colors.Tomato; s.BarWidth = 0.6; }))
        .SaveSvgAndPng(SamplesPath("light_direct.svg"));
    Console.WriteLine("Saved light_direct.svg");

    Plt.Create()
        .WithTitle("A light with no direct part: ambient 1, diffuse 0")
        .WithSize(640, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: 25, azimuth: -60)
            .WithLighting(dx: -0.4, dy: -0.8, dz: 0.45, ambient: 1.0, diffuse: 0.0)
            .Bar3D(barColumn, barRow, barHeight, s => { s.Color = Colors.Tomato; s.BarWidth = 0.6; }))
        .SaveSvgAndPng(SamplesPath("light_ambient.svg"));
    Console.WriteLine("Saved light_ambient.svg");
}

// --- 74. Cookbook images — reference lines wear the grey the theme owns ---
{
    double[] weekNumber = [1, 2, 3, 4, 5, 6, 7, 8];
    double[] throughput = [3.1, 3.9, 3.4, 4.8, 5.2, 4.9, 6.3, 6.8];

    Plt.Create()
        .WithTheme(Theme.Nord)
        .WithTitle("Reference lines under Nord")
        .WithSize(700, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(weekNumber, throughput, s => s.Label = "Throughput")
            .AxHLine(5.0, line => line.Label = "Target")
            .AxHLine(3.0, line => line.Label = "Floor")
            .SetXLabel("Week")
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("neutral_colour.svg"));
    Console.WriteLine("Saved neutral_colour.svg");
}

// --- 75. Cookbook images — a relative rotation graph whose assets all lead ---
{
    int rotationWeeks = 80;
    var rotationNoise = new Random(3);
    double[] rotationBenchmark = new double[rotationWeeks];
    double rotationLevel = 100;
    for (int week = 0; week < rotationWeeks; week++)
    {
        rotationLevel *= 1 + (rotationNoise.NextDouble() - 0.5) * 0.004;
        rotationBenchmark[week] = rotationLevel;
    }

    double[] Leader(double pull) =>
        [.. rotationBenchmark.Select((b, week) => b * (1 + pull * Math.Pow(week / (double)rotationWeeks, 2)))];

    Plt.Create()
        .WithTitle("Three assets that all lead their benchmark")
        .WithSize(800, 650)
        .AddSubPlot(1, 1, 1, ax => ax
            .SetXLabel("RS-Ratio")
            .SetYLabel("RS-Momentum")
            .RelativeRotation([Leader(0.6), Leader(0.4), Leader(0.25)], rotationBenchmark, ["Alpha", "Beta", "Gamma"],
                s => s.TailLength = 12))
        .SaveSvgAndPng(SamplesPath("rrg_centre.svg"));
    Console.WriteLine("Saved rrg_centre.svg");
}

// --- 76. Cookbook images — an inset built where it stands keeps its legend ---
{
    double[] signalX = [.. Enumerable.Range(0, 50).Select(i => i / 5.0)];
    double[] signalY = [.. signalX.Select(v => Math.Sin(3 * v) * Math.Exp(-v / 4))];
    double[] zoomX = [.. signalX.Where(v => v <= 1.5)];
    double[] zoomY = [.. zoomX.Select(v => Math.Sin(3 * v) * Math.Exp(-v / 4))];

    Plt.Create()
        .WithTitle("An inset that keeps its legend")
        .WithSize(750, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot(signalX, signalY, s => s.Label = "Full signal")
            .WithLegend(LegendPosition.LowerLeft)
            .AddInset(0.5, 0.45, 0.45, 0.45, inset => inset
                .Plot(zoomX, zoomY, s => { s.Label = "Detail"; s.Color = Colors.Crimson; })
                .WithLegend()))
        .SaveSvgAndPng(SamplesPath("inset_legend.svg"));
    Console.WriteLine("Saved inset_legend.svg");
}

// --- 77. Cookbook images — a violin drawn from the Gaussian density of twelve observations ---
{
    double[][] twelve = [[2.1, 2.4, 2.6, 2.9, 3.0, 3.1, 3.4, 3.6, 3.8, 4.2, 5.1, 6.3]];

    Plt.Create()
        .WithTitle("A violin of twelve observations")
        .WithSize(500, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Violin(twelve, s => { s.Color = Colors.RebeccaPurple; s.Alpha = 0.6; })
            .SetYLabel("Value"))
        .SaveSvgAndPng(SamplesPath("violin_density.svg"));
    Console.WriteLine("Saved violin_density.svg");
}

// --- 78. Cookbook images — a polar chart whose rings and spokes follow WithGrid ---
{
    double[] gridAngle = [.. Enumerable.Range(0, 100).Select(i => i * 2 * Math.PI / 100)];
    double[] gridRadius = [.. gridAngle.Select(a => 1 + Math.Cos(3 * a))];

    Plt.Create()
        .WithTitle("A polar chart on a dashed crimson grid")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot(gridRadius, gridAngle, s => { s.Color = Colors.Blue; s.LineWidth = 2; })
            .WithGrid(g => g with { Color = Colors.Crimson, LineStyle = LineStyle.Dashed }))
        .SaveSvgAndPng(SamplesPath("polar_grid.svg"));
    Console.WriteLine("Saved polar_grid.svg");
}

// --- 59. The pictures the wiki shows that no sample above draws ---
// The wiki keeps a copy of each of them; WikiPicturesTests holds the copies to what is drawn here.
WikiPictures.SaveAll((name, figure) =>
{
    File.WriteAllBytes(SamplesPath(name + ".png"), figure.ToPng());
    Console.WriteLine($"Saved {name}.png");
});

Console.WriteLine("Done!");

/// <summary>One row of a 3-D bar sample: the Y plane it stands on and its colour.</summary>
file readonly record struct BarRow(double Y, Color Color);

/// <summary>A theme of the theme comparison, with the name its tile is titled with.</summary>
file readonly record struct NamedTheme(string Name, Theme Theme);

/// <summary>One tile of the theme comparison, drawn: its name, its SVG and its PNG.</summary>
file readonly record struct RenderedTile(string Name, string Svg, byte[] Png);

/// <summary>A map projection of the projection comparison, with the name its panel is titled with.</summary>
file readonly record struct NamedProjection(string Name, IGeoProjection Projection);

/// <summary>A simulated coin of the rotation sample: its symbol, its weekly drift and its volatility.</summary>
file readonly record struct AltCoin(string Symbol, double Drift, double Vol);
