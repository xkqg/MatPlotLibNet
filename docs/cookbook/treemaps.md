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

## Treemap with drilldown

Click a rectangle to zoom in; press Escape to zoom out. Three levels deep:

```csharp
Plt.Create()
    .WithTitle("Treemap — click to drill down (Escape to zoom out)")
    .WithSize(900, 620)
    .WithTreemapDrilldown()
    .AddSubPlot(1, 1, 1, ax => ax.Treemap(catalogue, s => s.ShowLabels = true))
    .Save("treemap_drilldown.svg");
```

![Treemap drilldown](../images/treemap_drilldown.png)
