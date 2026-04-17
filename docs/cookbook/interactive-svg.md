# Interactive SVG

## Browser-interactive charts

Add `.WithBrowserInteraction()` to make SVG output interactive — pan, zoom, tooltips, legend toggle — no .NET runtime needed on the client:

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .WithTitle("Interactive Chart")
    .Plot(x, y, s => s.Label = "Sensor A")
    .Scatter(x, y2, s => s.Label = "Sensor B")
    .WithLegend()
    .Save("interactive.svg");
```

Open the SVG in any browser:
- **Drag** to pan
- **Scroll** to zoom
- **Double-click** to reset view
- **Click legend items** to show/hide series
- **Hover data points** to see values

## Individual interaction toggles

Enable only the interactions you need:

```csharp
Plt.Create()
    .WithZoomPan()              // drag to pan, scroll to zoom
    .WithRichTooltips()         // styled HTML tooltips on hover
    .WithLegendToggle()         // click legend to hide/show series
    .WithHighlight()            // dim siblings on series hover
    .WithSelection()            // Shift+drag rectangular data selection
    .Plot(x, y, s => s.Label = "Data")
    .WithLegend()
    .Save("custom_interactive.svg");
```

## Interactive financial chart

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .WithTitle("Stock Price — Hover for OHLC values")
    .AddSubPlot(1, 1, 1, ax => ax
        .Candlestick(open, high, low, close, dateLabels, s =>
        {
            s.UpColor = Colors.Green;
            s.DownColor = Colors.Red;
        })
        .SetXDateFormat("yyyy-MM-dd"))
    .Save("interactive_financial.svg");
```

## Interactive 3D with rotation

Combine browser interaction with 3D rotation:

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .With3DRotation()          // mouse-drag to rotate the 3D view
    .AddSubPlot(1, 1, 1, ax => ax
        .WithCamera(elevation: 35, azimuth: -50)
        .Surface(x, y, z, s => s.ColorMap = ColorMaps.Viridis))
    .Save("interactive_3d.svg");
```

## Server-authoritative interaction (SignalR)

For bidirectional interactive charts with server-side state:

```csharp
Plt.Create()
    .WithServerInteraction("chart-1", opt =>
    {
        opt.OnZoom = (chartId, ev) => Console.WriteLine($"Zoomed: {ev}");
        opt.OnBrushSelect = (chartId, ev) => Console.WriteLine($"Selected: {ev}");
        opt.OnHover = (chartId, ev) => Console.WriteLine($"Hover: {ev}");
    })
    .Plot(x, y, s => s.Label = "Live Data")
    .Save("server_interactive.svg");
```

## How it works

The SVG embeds self-contained JavaScript (no external dependencies):

| Script | What it does |
|---|---|
| Pan/Zoom | Manipulates SVG `viewBox` on drag/scroll |
| Tooltips | Reads `data-x`/`data-y` attributes, shows floating callout |
| Legend Toggle | Toggles `display:none` on series groups, supports keyboard (Enter/Space) |
| Highlight | Dims sibling series on hover |
| Selection | Shift+drag draws selection rectangle, fires callback |

## Static vs Interactive

```csharp
// Static SVG (default) — smaller file, perfect for PDF/print
Plt.Create().Plot(x, y).Save("static.svg");

// Interactive SVG — larger file, needs browser
Plt.Create().WithBrowserInteraction().Plot(x, y).Save("interactive.svg");
```

The interaction scripts add ~3KB to the SVG file size.

## Fluent API reference

| Method | Description |
|---|---|
| `.WithBrowserInteraction()` | Enable all client-side interactions (pan, zoom, tooltips, legend toggle) |
| `.WithZoomPan()` | Drag to pan, scroll to zoom |
| `.WithRichTooltips()` | Styled HTML tooltips on hover |
| `.WithLegendToggle()` | Click legend entries to toggle series visibility |
| `.WithHighlight()` | Dim sibling series on hover |
| `.WithSelection()` | Shift+drag rectangular data selection |
| `.With3DRotation()` | Mouse/keyboard rotation for 3D charts |
| `.WithTreemapDrilldown()` | Click-to-drill-down on treemaps |
| `.WithSankeyHover()` | Node hover emphasis for Sankey diagrams |
| `.WithServerInteraction(id, cfg)` | Bidirectional SignalR interaction |
