// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Transforms;

// --- 1. Simple line chart -> SVG ---
double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];

var figure = Plt.Create()
    .WithTitle("Sales Trend")
    .WithTheme(Theme.Seaborn)
    .WithSize(800, 500)
    .Plot(x, y, line => { line.Color = Colors.Blue; line.Label = "Revenue"; })
    .Build();

figure.Transform(new SvgTransform()).ToFile("chart.svg");
Console.WriteLine("Saved chart.svg");

// --- 2. PNG and PDF export (via Skia) ---
figure.Transform(new PngTransform()).ToFile("chart.png");
figure.Transform(new PdfTransform()).ToFile("chart.pdf");
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

multiChart.Transform(new SvgTransform()).ToFile("dashboard.svg");
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
    .Save("heatmap_colormap.svg");
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

cmpBuilder.Save("colormap_comparison.svg");
Console.WriteLine("Saved colormap_comparison.svg");

// --- 7. GridSpec layout ---
string[] categories2 = ["A", "B", "C", "D"];
double[] catValues = [15, 42, 28, 37];

Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main plot"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y).WithTitle("Scatter"))
    .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Bar(categories2, catValues).WithTitle("Wide bar"))
    .Save("gridspec_layout.svg");
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
    .Save("tick_locators.svg");
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
    .Save("bar_labels.svg");
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
    .Save("lttb_downsampling.svg");
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
    .Save("annotations_enhanced.svg");
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
    .Save("financial_dashboard.svg");
Console.WriteLine("Saved financial_dashboard.svg");

// --- 14. New indicators: WilliamsR, OBV, CCI, ParabolicSAR ---
Plt.Create()
    .WithTitle("Phase F Indicators")
    .WithSize(1000, 800)
    .WithGridSpec(2, 2)
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
    .Save("phase_f_indicators.svg");
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
    .Save("contour_labels.svg");
Console.WriteLine("Saved contour_labels.svg");

// --- 16. Scientific paper template (150 DPI, hidden top/right spines, tight layout) ---
FigureTemplates.ScientificPaper(1, 1, title: "Damped Oscillation")
    .Save("scientific_paper.svg");
Console.WriteLine("Saved scientific_paper.svg");

// --- 17. Sparkline dashboard ---
FigureTemplates.SparklineDashboard(
    [
        ("CPU %",    Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 100).ToArray()),
        ("Memory %", Enumerable.Range(0, 60).Select(_ => 40 + rng.NextDouble() * 30).ToArray()),
        ("Disk I/O", Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 500).ToArray()),
    ],
    title: "Server Metrics — Last 60s")
    .Save("sparkline_dashboard.svg");
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
    .Save("date_axis.svg");
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
    .Save("math_text.svg");
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
    .Save("prop_cycler.svg");
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
    .Save("accessibility_colorblind.svg");
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
    .Save("accessibility_highcontrast.svg");
Console.WriteLine("Saved accessibility_highcontrast.svg");

Console.WriteLine("Done!");
