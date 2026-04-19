# Treemaps & Hierarchical Charts

## Nested pie

Inner disc of departments + outer ring of product breakdown (thin alias for Sunburst):

```csharp
Plt.Create()
    .WithTitle("Nested Pie — Revenue by department and product")
    .WithSize(720, 720)
    .AddSubPlot(1, 1, 1, ax => ax.NestedPie(departments))
    .Save("nested_pie.svg");
```

![Nested pie](../images/nested_pie.png)

## Treemap with expand/collapse

Each interior node renders as a coloured rectangle with a label header along the top; children are squarified into the reduced bounds below that header, so the parent colour visually frames its descendants (matches the d3 `flare.json` nested-treemap style). The initial view shows only top-level parent rectangles — clicking a parent toggles visibility of its direct children, and clicking again collapses them. Multiple parents can be expanded simultaneously with independent state. Leaves (nodes with no children) are not clickable, so the cursor stays default when hovering them.

Font size scales with depth via `Math.Max(8, 14 - depth * 1.5)` — top-level parents render at ~12.5pt, depth-2 at ~11pt, depth-3 at ~9.5pt, with an 8pt floor so labels stay legible at any nesting level.

```csharp
Plt.Create()
    .WithTitle("Treemap — click a parent to expand, click again to collapse")
    .WithSize(900, 620)
    .WithTreemapDrilldown()
    .AddSubPlot(1, 1, 1, ax => ax.Treemap(catalogue, s => s.ShowLabels = true))
    .Save("treemap_drilldown.svg");
```

`WithBrowserInteraction()` also enables the same expand/collapse behaviour if you prefer the general-purpose fluent method.

> **v1.7.2 Phase R — click delivery in real browsers fixed.** The expand/collapse model
> itself (Phase P) is unchanged. Two compounding bugs in the click handler were fixed:
> hover used to latch the drag-suppression flag (so any cursor motion killed the next
> click), and the pan/zoom script's `setPointerCapture` redirected the synthetic click
> target to the SVG root (so the rect-walk-up returned null). Now: the move-threshold
> is gated on an `isPointerDown` flag, and the click handler falls back to
> `document.elementFromPoint` when the walk-up misses.

![Treemap drilldown](../images/treemap_drilldown.png)
