# Interactive SVG

## Browser-interactive charts

Add `.WithBrowserInteraction()` to make SVG output interactive — pan, zoom, tooltips, legend toggle, **legend drag**, treemap drilldown, sankey hover, 3D rotation, brush selection, highlight — no .NET runtime needed on the client:

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .WithTitle("Interactive Chart")
    .Plot(x, y, s => s.Label = "Sensor A")
    .Scatter(x, y2, s => s.Label = "Sensor B")
    .WithLegend()
    .Save("interactive.svg");
```

> **One switch wires everything.** `WithBrowserInteraction()` is the only call you need.
> The library detects which scripts are relevant per chart (legend drag bails when there
> is no legend; treemap drilldown bails when there are no treemap nodes) and emits only
> those. There is no per-feature toggle for the user to manage.

Open the SVG in any browser:
- **Drag the chart** to pan (hold <kbd>x</kbd> / <kbd>y</kbd> to lock an axis — matplotlib `_base.py:format_deltas` parity)
- **Scroll** to zoom (`0.85^step` per wheel notch — matches matplotlib `NavigationToolbar2.scroll_handler`)
- **Double-click** or <kbd>Home</kbd> to reset view
- Arrow keys to nudge pan, <kbd>+</kbd>/<kbd>-</kbd> for keyboard zoom
- **Click legend items** to show/hide series (<kbd>Enter</kbd>/<kbd>Space</kbd> keyboard-equivalent, WCAG 2.1.1 Level A)
- **Press-and-hold a legend item, then drag** to reposition the legend group anywhere on the chart (release to drop). Translation is client-only — lost on full server re-render. New in v1.7.2 Phase S.
- **Hover data points** to see tooltips (focus via <kbd>Tab</kbd> for keyboard users — tooltip anchors at element bounds)

See the [Keyboard Shortcuts wiki page](https://github.com/xkqg/MatPlotLibNet/wiki/Keyboard-Shortcuts) for the complete reference.

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

Combine browser interaction with 3D rotation. Drag uses matplotlib's canonical
formula (`dazim/delev = -(dx/w or dy/h) × 180` — full-axes drag = 180°). Wheel
zoom works on every 3D chart (Phase F.3 of v1.7.2 removed the need for an
explicit `distance:`). Labels keep their outside-cube perpendicular offset
under rotation (Phase F.2). Back panes never paint over surface quads — the
depth-sort is scoped to the `mpl-3d-data` tier group (Phase F).

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .With3DRotation()          // mouse-drag to rotate the 3D view
    .AddSubPlot(1, 1, 1, ax => ax
        .WithCamera(elevation: 35, azimuth: -50)
        .Surface(x, y, z, s => s.ColorMap = ColorMaps.Viridis))
    .Save("interactive_3d.svg");
```

Keyboard: arrow keys rotate ±5° (az / el), <kbd>+</kbd>/<kbd>-</kbd> change
camera distance by 0.5, <kbd>Home</kbd> restores the initial camera state.

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

## Responsive sizing (v1.7.2 Phase L)

By default the SVG root carries an inline `style="max-width:100%;height:auto"` declaration, so the chart scales fluidly with its container while the `viewBox` preserves aspect ratio. The pixel `width` / `height` attributes stay on the element so `naturalWidth` / `naturalHeight` — relied on by client-side PNG export paths — continue to report the intrinsic pixel size.

If you need byte-identical pre-v1.7.2 SVG output (e.g. pixel-diff test fixtures), opt out:

```csharp
Plt.Create()
    .WithResponsiveSvg(false)   // emits fixed pixel width/height with no inline style
    .Plot(x, y)
    .Save("fixed.svg");
```

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
| `.WithBrowserInteraction()` | Enable all client-side interactions (pan, zoom, tooltips, legend toggle, 3D rotate, treemap drilldown, sankey hover) |
| `.WithZoomPan()` | Drag to pan, scroll to zoom, `x`/`y` axis-lock modifiers, keyboard `+`/`-`/arrows/Home |
| `.WithRichTooltips()` | Styled HTML tooltips on hover + focus (ARIA `role="tooltip"`, `aria-live="polite"`) |
| `.WithLegendToggle()` | Click legend entries (or `Enter`/`Space` for keyboard) to toggle series visibility |
| `.WithHighlight()` | Dim sibling series on hover; opacity themable via `WithInteractionTheme`; original opacity preserved across hover cycles |
| `.WithSelection()` | Shift+drag rectangular data selection; `Escape` cancels without dispatching |
| `.With3DRotation()` | Drag to rotate (matplotlib parity), arrow keys ±5°, `+`/`-` distance, wheel zoom, `Home` reset |
| `.WithTreemapDrilldown()` | Every depth visible by default (interactive view = static SVG, "steady pictures"); click a parent rect to *collapse* its entire subtree (transitive — descendants' own state preserved); click again to restore. Multiple subtrees can be collapsed independently. Z-order paints children over parents so the deepest visible label wins. (v1.7.2 Phase W; was drill-zoom + Esc-pop in v1.x, expand-on-click in Phase P.) |
| `.WithSankeyHover()` | Node hover emphasises upstream + downstream flow (ECharts `focus: adjacency` parity), keyboard via `Tab` |
| `.WithInteractionTheme(theme)` | Themable opacity / transition tokens (highlight opacity, sankey dim opacities, treemap transition ms, tooltip offset) |
| `.WithServerInteraction(id, cfg)` | Bidirectional SignalR interaction (hub methods `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` / `OnBrushSelect` / `OnHover`) |
