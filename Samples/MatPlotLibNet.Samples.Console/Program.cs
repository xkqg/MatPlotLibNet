// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
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
    .Plot(x, y, line => { line.Color = Color.Blue; line.Label = "Revenue"; })
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
    .Bar(categories, values, bar => { bar.Color = Color.Orange; bar.Label = "Units sold"; })
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
    .AddSubPlot(GridPosition.Span(1, 2, 0, 2), ax => ax.Bar(categories2, catValues).WithTitle("Wide bar"))
    .Save("gridspec_layout.svg");
Console.WriteLine("Saved gridspec_layout.svg");

Console.WriteLine("Done!");
