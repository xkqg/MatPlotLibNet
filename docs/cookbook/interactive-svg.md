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

## How it works

The SVG embeds self-contained JavaScript (no external dependencies):

| Script | What it does |
|---|---|
| Pan/Zoom | Manipulates SVG `viewBox` on drag/scroll |
| Tooltips | Reads `data-x`/`data-y` attributes, shows floating callout |
| Legend Toggle | Toggles `display:none` on series groups, supports keyboard (Enter/Space) |

## Static vs Interactive

```csharp
// Static SVG (default) — smaller file, perfect for PDF/print
Plt.Create().Plot(x, y).Save("static.svg");

// Interactive SVG — larger file, needs browser
Plt.Create().WithBrowserInteraction().Plot(x, y).Save("interactive.svg");
```

The interaction scripts add ~3KB to the SVG file size.
