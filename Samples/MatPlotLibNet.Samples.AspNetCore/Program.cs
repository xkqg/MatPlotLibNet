// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

// Bidirectional SignalR interactive chart sample for MatPlotLibNet v1.2.0.
//
// Server builds one figure with .WithServerInteraction(...) and registers it in the
// FigureRegistry under chartId "live-1". The browser page loads @microsoft/signalr,
// connects to /charts-hub, subscribes to "live-1", and embeds the initial SVG. When the
// user wheel-zooms, pans, or presses Home, the embedded SvgSignalRInteractionScript
// invokes OnZoom / OnPan / OnReset on the hub. The server's reader task mutates the
// figure, re-renders, and pushes the updated SVG back via IChartPublisher. The browser
// receives it via UpdateChartSvg and swaps the DOM.

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMatPlotLibNetSignalR();
builder.Services.AddSingleton<ISvgRenderer>(new MatPlotLibNet.Transforms.SvgTransform());

var app = builder.Build();

const string ChartId = "live-1";

// Build the authoritative figure once. 200 sample points of noisy sinusoid.
var xs = Enumerable.Range(0, 200).Select(i => i * 0.05).ToArray();
var rng = new Random(42);
var ys = xs.Select(x => Math.Sin(x) + 0.1 * (rng.NextDouble() - 0.5)).ToArray();

var figure = Plt.Create()
    .WithTitle("Bidirectional SignalR Demo — wheel to zoom, drag to pan, Home to reset")
    .WithSize(900, 500)
    .WithTheme(Theme.MatplotlibV2)
    .Plot(xs, ys, line => { line.Color = Css4Colors.DodgerBlue; line.Label = "sin(x) + noise"; })
    .WithServerInteraction(ChartId, i => i.All())
    .Build();

// Set explicit starting limits so the dispatcher script can compute zoom/pan.
figure.SubPlots[0].XAxis.Min = xs[0];
figure.SubPlots[0].XAxis.Max = xs[^1];
figure.SubPlots[0].YAxis.Min = -1.5;
figure.SubPlots[0].YAxis.Max = 1.5;

// Register the figure with the pub/sub pipeline.
var registry = app.Services.GetRequiredService<FigureRegistry>();
registry.Register(ChartId, figure);

// SignalR hub endpoint.
app.MapChartHub();

// Initial SVG endpoint — the HTML page fetches this once on load.
app.MapGet("/api/chart/live.svg", (ISvgRenderer svgRenderer) =>
    Results.Content(svgRenderer.Render(figure), "image/svg+xml"));

// Static file serving for wwwroot/index.html.
app.UseDefaultFiles();
app.UseStaticFiles();

Console.WriteLine("""

    MatPlotLibNet bidirectional SignalR sample

    Open http://localhost:5000 in your browser.
    Scroll-wheel to zoom, drag to pan, Home to reset.
    The server log below will print every interaction event as it arrives.

    """);

app.Run();
