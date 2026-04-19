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

Labels render at a single readable 12 pt size at every depth (v1.7.2 Phase W). Children paint OVER parents (Shneiderman z-order), so the deepest visible label is what the user sees in any overlapping region. For static SVG output with deep trees, call `.WithAutoSize(root)` on the `FigureBuilder` to pick a canvas big enough to fit every label cleanly without overflow; for interactive output, the user pans/zooms to read overflowing labels.

```csharp
var catalogue = new TreeNode
{
    Label = "Revenue",
    Children =
    [
        new()
        {
            Label = "Electronics", Value = 42, Color = Colors.Blue,
            Children =
            [
                new()
                {
                    Label = "Phones", Value = 22, Color = Colors.CornflowerBlue,
                    Children =
                    [
                        new() { Label = "iPhone", Value = 12, Color = Colors.RoyalBlue },
                        new() { Label = "Galaxy", Value = 7,  Color = Colors.SteelBlue },
                        new() { Label = "Pixel",  Value = 3,  Color = Colors.CornflowerBlue },
                    ]
                },
                new() { Label = "Laptops", Value = 14, Color = Colors.SteelBlue },
                new() { Label = "TVs",     Value = 6,  Color = Colors.RoyalBlue },
            ]
        },
        new() { Label = "Apparel", Value = 28, Color = Colors.Orange,
            Children = [
                new() { Label = "Men's",   Value = 11, Color = Colors.Chocolate },
                new() { Label = "Women's", Value = 13, Color = Colors.Tomato },
                new() { Label = "Kids'",   Value = 4,  Color = Colors.Coral },
            ]
        },
        new() { Label = "Grocery", Value = 30, Color = Colors.Green,
            Children = [
                new() { Label = "Fresh",  Value = 13, Color = Colors.ForestGreen },
                new() { Label = "Frozen", Value = 9,  Color = Colors.Teal },
                new() { Label = "Pantry", Value = 8,  Color = Colors.Tab10Green },
            ]
        },
    ]
};

Plt.Create()
    .WithTitle("Treemap — click a parent to expand; click Phones to drill into iPhone/Galaxy/Pixel")
    .WithAutoSize(catalogue)              // v1.7.2 Phase W — sizes the canvas to fit every label
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
