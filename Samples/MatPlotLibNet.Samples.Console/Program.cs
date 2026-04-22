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

var deserialized = ChartServices.Serializer.FromJson(json);
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

// =====================================================================
// v0.6.0 — SIMD Vectorization + Phase F
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
    .WithTitle("Phase F Indicators")
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
        ax.WilliamsR(high, low, close, 14);
        ax.WithTitle("Williams %R");
    })
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
    {
        ax.Obv(close, vol);
        ax.WithTitle("On-Balance Volume");
    })
    .AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
    {
        ax.Cci(high, low, close, 20);
        ax.WithTitle("CCI(20)");
    })
    .SaveSvgAndPng(SamplesPath("phase_f_indicators.svg"));
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
            .Plot(tSP, ySP, s => { s.Label = "e^{-0.15t} cos(2t)"; s.LineWidth = 1.2; })
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
        ("CPU %",    Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 100).ToArray()),
        ("Memory %", Enumerable.Range(0, 60).Select(_ => 40 + rng.NextDouble() * 30).ToArray()),
        ("Disk I/O", Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 500).ToArray()),
    ],
    title: "Server Metrics — Last 60s")
    .SaveSvgAndPng(SamplesPath("sparkline_dashboard.svg"));
Console.WriteLine("Saved sparkline_dashboard.svg");

// =====================================================================
// v0.8.4 — Stacked base classes (CandleIndicator, OhlcSeries, DatasetSeries, PriceIndicator)
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
// --- Phase G: True 3-D samples ---
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
    (double Y, Color Color)[] rows =
    [
        (0, Colors.Red),
        (1, Colors.Green),
        (2, Colors.Blue),
        (3, Colors.Cyan),
        (4, Colors.Gold),
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
    (double Y, Color PlaneColor)[] prows =
    [
        (0, Colors.Red),
        (1, Colors.Green),
        (2, Colors.Blue),
        (3, Colors.Cyan),
        (4, Colors.Gold),
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
    (double Y, Color PlaneColor)[] phrows =
    [
        (0, Colors.Red),
        (1, Colors.Green),
        (2, Colors.Blue),
        (3, Colors.Cyan),
        (4, Colors.Gold),
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
// v1.3.0 — 6 new 3-D series + MathText completion
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

// 28. MathText — v1.3.0 features: fractions, sqrt, accents, font variants
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

    var themes = new (string Name, Theme T)[]
    {
        ("Default", Theme.Default), ("Dark", Theme.Dark),
        ("Nord", Theme.Nord), ("Dracula", Theme.Dracula),
        ("Cyberpunk", Theme.Cyberpunk), ("Monokai", Theme.Monokai),
    };

    const int tileW = 400, tileH = 300;
    const int titleH = 40;
    const int cols = 3, rows = 2;
    int gridW = cols * tileW;
    int gridH = rows * tileH + titleH;

    // Per-tile SVG body (raw <svg> markup) — captured for both the composite SVG
    // and decoded back into bitmaps for the composite PNG.
    var tiles = new (string Name, string Svg, byte[] Png)[themes.Length];
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
        tiles[i] = (name, fig.ToSvg(), fig.ToPng());
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
{
    string[] defects = ["Scratches", "Dents", "Cracks", "Stains", "Breaks", "Missing", "Warp"];
    double[] counts = [142, 98, 67, 45, 31, 18, 9];
    double total = counts.Sum();
    double running = 0;
    double[] cumPct = counts.Select(c => { running += c; return running / total * 100; }).ToArray();
    double[] barCenters = Enumerable.Range(0, defects.Length).Select(i => i + 0.5).ToArray();

    Plt.Create()
        .WithTitle("Pareto Chart — Defect Analysis")
        .WithSize(900, 500)
        .AddSubPlot(1, 1, 1, ax => ax
            .Bar(defects, counts, s => { s.Color = Color.FromHex("#4472C4"); s.Label = "Count"; })
            .WithSecondaryYAxis(y2 => y2
                .Plot(barCenters, cumPct, s =>
                {
                    s.Color = Color.FromHex("#8B0000");
                    s.LineWidth = 2;
                    s.Marker = MarkerStyle.Circle;
                    s.MarkerSize = 7;
                    s.Label = "Cumulative %";
                })
                .SetYLim(0, 100)
                .SetYLabel("Cumulative %"))
            .SetYLabel("Defect Count")
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("pareto_chart.svg"));
    Console.WriteLine("Saved pareto_chart");
}

// --- 24. Cookbook images — lollipop chart ---
{
    // Products at indices 0-5: Alpha, Beta, Gamma, Delta, Epsilon, Zeta
    double[] scores = [82.5, 67.3, 91.1, 54.8, 76.4, 88.2];
    double[] xIdx = Enumerable.Range(0, scores.Length).Select(i => (double)i).ToArray();

    Plt.Create()
        .WithTitle("Lollipop Chart — Product Scores")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .Stem(xIdx, scores, s => { s.StemColor = Color.FromHex("#5B9BD5"); })
            .Scatter(xIdx, scores, s =>
            {
                s.Color = Color.FromHex("#5B9BD5");
                s.MarkerSize = 14;
                s.Marker = MarkerStyle.Circle;
            })
            .SetYLim(0, 110)
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
                s.BarWidth = 0.6;
            })
            .SetYLabel("P&L (€k)")
            .WithXTickLabelRotation(30))
        .SaveSvgAndPng(SamplesPath("waterfall_pnl.svg"));
    Console.WriteLine("Saved waterfall_pnl");
}

// --- 26. Cookbook images — ridge plot ---
{
    var rngRidge = new Random(42);
    double BoxMuller(Random r) { double u1 = 1 - r.NextDouble(), u2 = 1 - r.NextDouble(); return Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2); }
    double[] xGrid = Enumerable.Range(0, 300).Select(i => i * 0.04 - 3.0).ToArray();

    double[] GaussKde(double[] data, double bw = 0.5) =>
        xGrid.Select(xi =>
            data.Sum(d =>
            {
                double z = (xi - d) / bw;
                return Math.Exp(-0.5 * z * z) / (bw * Math.Sqrt(2 * Math.PI));
            }) / data.Length
        ).ToArray();

    string[] groups = ["Group A", "Group B", "Group C", "Group D", "Group E", "Group F"];
    double[] means = [-1.2, -0.4, 0.2, 0.7, 1.3, 1.9];
    var palette = new[] { "#E41A1C", "#377EB8", "#4DAF4A", "#984EA3", "#FF7F00", "#A65628" };

    Plt.Create()
        .WithTitle("Ridge Plot — Distribution Comparison")
        .WithSize(900, 600)
        .AddSubPlot(1, 1, 1, ax =>
        {
            for (int g = 0; g < groups.Length; g++)
            {
                double offset = g * 0.6;
                double[] data = Enumerable.Range(0, 200)
                    .Select(_ => means[g] + BoxMuller(rngRidge) * 0.6)
                    .ToArray();
                double[] kde = GaussKde(data);
                double[] kdeShifted = kde.Select(v => v + offset).ToArray();
                double[] baseline = Enumerable.Repeat(offset, xGrid.Length).ToArray();
                var col = Color.FromHex(palette[g]);
                ax.FillBetween(xGrid, kdeShifted, baseline, s =>
                {
                    s.Color = col;
                    s.Alpha = 0.6;
                    s.Label = groups[g];
                });
                ax.Plot(xGrid, kdeShifted, s => { s.Color = col; s.LineWidth = 1.5; });
            }
            ax.WithLegend();
        })
        .SaveSvgAndPng(SamplesPath("ridge_plot.svg"));
    Console.WriteLine("Saved ridge_plot");
}

// --- 27. Cookbook images — dumbbell chart ---
{
    // Indices 0-5: Product A–F
    double[] before = [62.0, 78.5, 55.0, 83.0, 70.0, 48.0];
    double[] after  = [74.0, 82.0, 68.0, 79.5, 88.5, 61.0];
    double[] xIdx   = Enumerable.Range(0, before.Length).Select(i => (double)i).ToArray();
    double[] zeros    = new double[before.Length];
    double[] dumbDiff = before.Zip(after, (b, a) => Math.Abs(a - b)).ToArray();

    Plt.Create()
        .WithTitle("Dumbbell Chart — Before vs After")
        .WithSize(800, 450)
        .AddSubPlot(1, 1, 1, ax => ax
            .ErrorBar(xIdx, before, zeros, dumbDiff, s =>
            {
                s.Color = Color.FromHex("#AAAAAA");
                s.LineWidth = 3;
                s.CapSize = 0;
            })
            .Scatter(xIdx, before, s => { s.Color = Color.FromHex("#E74C3C"); s.MarkerSize = 14; s.Marker = MarkerStyle.Circle; s.Label = "Before"; })
            .Scatter(xIdx, after, s => { s.Color = Color.FromHex("#2ECC71"); s.MarkerSize = 14; s.Marker = MarkerStyle.Circle; s.Label = "After"; })
            .SetYLim(30, 100)
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
{
    var rngCal = new Random(7);
    var calData = new double[52, 7];
    for (int w = 0; w < 52; w++)
        for (int d = 0; d < 7; d++)
        {
            double base_ = (d < 5) ? rngCal.NextDouble() * 8 : rngCal.NextDouble() * 2;
            double trend = w * 0.05;
            calData[w, d] = Math.Max(0, base_ + trend + rngCal.NextDouble() * 2 - 1);
        }

    Plt.Create()
        .WithTitle("Calendar Heatmap — GitHub-style Contributions")
        .WithSize(1100, 300)
        .AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(calData, s =>
            {
                s.ColorMap = ColorMaps.Viridis;
                s.Label = "Commits";
            })
            .WithColorBar()
            .SetXLabel("Week")
            .SetYLabel("Day"))
        .SaveSvgAndPng(SamplesPath("calendar_heatmap.svg"));
    Console.WriteLine("Saved calendar_heatmap");
}

// --- 30. Cookbook images — wind rose ---
{
    double[] directions = [0, 45, 90, 135, 180, 225, 270, 315];
    double[] freqSlow   = [5.2, 4.1, 6.8, 3.5, 4.9, 7.2, 5.5, 3.8]; // 0–10 knots
    double[] freqMed    = [3.1, 2.8, 4.2, 2.1, 3.4, 4.8, 3.2, 2.5]; // 10–20 knots
    double[] freqFast   = [1.2, 0.9, 1.8, 0.7, 1.1, 2.1, 1.4, 0.8]; // 20+ knots

    Plt.Create()
        .WithTitle("Wind Rose")
        .WithSize(600, 600)
        .AddSubPlot(1, 1, 1, ax => ax
            .PolarBar(freqSlow, directions, s => { s.Color = Color.FromHex("#AED6F1"); s.Alpha = 0.85; s.Label = "0–10 kn"; s.BarWidth = 40; })
            .PolarBar(freqMed, directions, s => { s.Color = Color.FromHex("#2E86C1"); s.Alpha = 0.85; s.Label = "10–20 kn"; s.BarWidth = 40; })
            .PolarBar(freqFast, directions, s => { s.Color = Color.FromHex("#1A252F"); s.Alpha = 0.85; s.Label = "20+ kn"; s.BarWidth = 40; })
            .WithLegend())
        .SaveSvgAndPng(SamplesPath("wind_rose.svg"));
    Console.WriteLine("Saved wind_rose");
}

// --- 31. Cookbook images — bump / rank chart ---
{
    var rng3 = new Random(99);
    string[] brands = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];
    int periods = 8;
    double[] xPeriods = Enumerable.Range(1, periods).Select(i => (double)i).ToArray();
    var palette2 = new[] { "#E41A1C", "#377EB8", "#4DAF4A", "#984EA3", "#FF7F00" };

    // Build rank data: each period has a permutation of 1..5
    int[][] ranks = Enumerable.Range(0, brands.Length)
        .Select(_ => new int[periods])
        .ToArray();
    for (int p = 0; p < periods; p++)
    {
        int[] perm = Enumerable.Range(1, brands.Length).OrderBy(_ => rng3.Next()).ToArray();
        for (int b = 0; b < brands.Length; b++)
            ranks[b][p] = perm[b];
    }

    Plt.Create()
        .WithTitle("Bump Chart — Brand Ranking over Time")
        .WithSize(900, 450)
        .AddSubPlot(1, 1, 1, ax =>
        {
            for (int b = 0; b < brands.Length; b++)
            {
                double[] y = ranks[b].Select(r => (double)r).ToArray();
                ax.Plot(xPeriods, y, s =>
                {
                    s.Color = Color.FromHex(palette2[b]);
                    s.LineWidth = 3;
                    s.Marker = MarkerStyle.Circle;
                    s.MarkerSize = 10;
                    s.Label = brands[b];
                });
            }
            ax.SetYLim(brands.Length + 0.5, 0.5);
            ax.SetYLabel("Rank");
            ax.SetXLabel("Period");
            ax.WithLegend();
        })
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
                    (40,  Color.FromHex("#E74C3C")),
                    (70,  Color.FromHex("#F39C12")),
                    (100, Color.FromHex("#2ECC71")),
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
    var projections = new (string Name, IGeoProjection Proj)[]
    {
        ("Mercator",    GeoProjection.Mercator),
        ("Robinson",    GeoProjection.Robinson),
        ("Mollweide",   GeoProjection.Mollweide),
        ("Equal Earth", GeoProjection.EqualEarth),
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

Console.WriteLine("Done!");
