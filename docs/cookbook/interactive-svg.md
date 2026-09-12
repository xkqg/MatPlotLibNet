---
title: "Interactive SVG charts in C#: pan, zoom and tooltips"
description: "Make an SVG chart interactive in C#: pan, zoom, tooltips, legend toggle, brush selection and 3D rotation, with no JavaScript framework."
---

# Interactive SVG

## Browser-interactive charts

Add `.WithBrowserInteraction()` to make SVG output interactive. You get pan, zoom, tooltips, legend toggle, **legend drag**, treemap drilldown, sankey hover, 3D rotation, brush selection and highlight. The client needs no .NET runtime:

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .WithTitle("Interactive Chart")
    .Plot(x, y, s => s.Label = "Sensor A")
    .Scatter(x, y2, s => s.Label = "Sensor B")
    .WithLegend()
    .Save("interactive.svg");
```

> **`WithBrowserInteraction()` is the only call you need.** The library works out
> which scripts each chart needs and emits only those. Legend drag is left out when
> there is no legend, and treemap drilldown is left out when there are no treemap
> nodes. You do not have to manage a toggle per feature.

Open the SVG in any browser:
- **Drag the chart** to pan (hold <kbd>x</kbd> / <kbd>y</kbd> to lock an axis, the same as matplotlib `_base.py:format_deltas`)
- **Scroll** to zoom (`0.85^step` per wheel notch, which matches matplotlib `NavigationToolbar2.scroll_handler`)
- **Double-click** or press <kbd>Home</kbd> to reset the view
- Press the arrow keys to nudge the pan, and <kbd>+</kbd>/<kbd>-</kbd> to zoom from the keyboard
- **Click legend items** to show/hide series (<kbd>Enter</kbd>/<kbd>Space</kbd> does the same from the keyboard, which meets WCAG 2.1.1 Level A)
- **Press-and-hold a legend item, then drag** to reposition the legend group anywhere on the chart (release to drop). The move happens on the client only, so a full server re-render loses it. New in v1.7.2 Phase S.
- **Hover data points** to see tooltips (keyboard users focus a point with <kbd>Tab</kbd>; the tooltip anchors at the element bounds)

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

Combine browser interaction with 3D rotation. Dragging uses matplotlib's canonical
formula (`dazim/delev = -(dx/w or dy/h) × 180`), so a drag across the full axes
turns the view by 180°. Wheel zoom works on every 3D chart; Phase F.3 of v1.7.2
removed the need for an explicit `distance:`. Labels keep their perpendicular
offset outside the cube while the view rotates (Phase F.2). Back panes never paint
over surface quads, because the depth-sort is scoped to the `mpl-3d-data` tier
group (Phase F).

```csharp
Plt.Create()
    .WithBrowserInteraction()
    .With3DRotation()          // mouse-drag to rotate the 3D view
    .AddSubPlot(1, 1, 1, ax => ax
        .WithCamera(elevation: 35, azimuth: -50)
        .Surface(x, y, z, s => s.ColorMap = ColorMaps.Viridis))
    .Save("interactive_3d.svg");
```

From the keyboard: the arrow keys rotate the view by ±5° in azimuth or elevation,
<kbd>+</kbd>/<kbd>-</kbd> change the camera distance by 0.5, and <kbd>Home</kbd>
restores the initial camera state.

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

The SVG embeds its own JavaScript, with no external dependencies:

| Script | What it does |
|---|---|
| Pan/Zoom | Manipulates SVG `viewBox` on drag/scroll |
| Tooltips | Reads `data-x`/`data-y` attributes, shows floating callout |
| Legend Toggle | Toggles `display:none` on series groups, supports keyboard (Enter/Space) |
| Highlight | Dims sibling series on hover |
| Selection | Shift+drag draws selection rectangle, fires callback |

## Responsive sizing (v1.7.2 Phase L)

By default the SVG root carries an inline `style="max-width:100%;height:auto"` declaration. The chart then scales with its container, while the `viewBox` preserves the aspect ratio. The pixel `width` / `height` attributes stay on the element, so `naturalWidth` / `naturalHeight` keep reporting the intrinsic pixel size. Client-side PNG export paths rely on those two values.

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
| `.WithBrowserInteraction()` | Enables all client-side interactions (pan, zoom, tooltips, legend toggle, 3D rotate, treemap drilldown, sankey hover) |
| `.WithZoomPan()` | Drag to pan, scroll to zoom, `x`/`y` axis-lock modifiers, keyboard `+`/`-`/arrows/Home |
| `.WithRichTooltips()` | Styled HTML tooltips on hover and on focus (ARIA `role="tooltip"`, `aria-live="polite"`) |
| `.WithLegendToggle()` | Click legend entries (or press `Enter`/`Space` on the keyboard) to toggle series visibility |
| `.WithHighlight()` | Dims sibling series on hover. The opacity is themable through `WithInteractionTheme`, and the original opacity is preserved across hover cycles |
| `.WithSelection()` | Shift+drag selects data in a rectangle; `Escape` cancels without dispatching |
| `.With3DRotation()` | Drag to rotate (matplotlib parity), arrow keys ±5°, `+`/`-` distance, wheel zoom, `Home` reset |
| `.WithTreemapDrilldown()` | Every depth is visible by default, so the interactive view shows the same picture as the static SVG. Click a parent rect to *collapse* its entire subtree, all the way down; each descendant keeps its own state. Click again to restore it. You can collapse several subtrees independently. Z-order paints children over parents, so the deepest visible label wins. (v1.7.2 Phase W; was drill-zoom plus Esc-pop in v1.x, and expand-on-click in Phase P.) |
| `.WithSankeyHover()` | Hovering a node emphasises its upstream and downstream flow (ECharts `focus: adjacency` parity); keyboard users reach it with `Tab` |
| `.WithInteractionTheme(theme)` | Themable opacity and transition tokens (highlight opacity, sankey dim opacities, treemap transition ms, tooltip offset) |
| `.WithServerInteraction(id, cfg)` | Bidirectional SignalR interaction (hub methods `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` / `OnBrushSelect` / `OnHover`) |
