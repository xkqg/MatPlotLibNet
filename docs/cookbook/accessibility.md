# Accessibility

## Color-blind safe palette

The `Theme.ColorBlindSafe` uses the Okabe-Ito palette — distinguishable by people with all forms of color vision deficiency:

```csharp
Plt.Create()
    .WithTitle("Monthly Revenue vs Cost (2025)")
    .WithAltText("Line chart: revenue and cost trends over 12 months of 2025")
    .WithDescription("Revenue grew from $1.2M to $3.5M. Cost grew from $0.9M to $2.4M.")
    .WithTheme(Theme.ColorBlindSafe)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, revenue, s => { s.Label = "Revenue"; s.LineWidth = 2.5; });
        ax.Plot(x, cost,    s => { s.Label = "Cost"; s.LineWidth = 2.5; s.LineStyle = LineStyle.Dashed; });
        ax.WithLegend(LegendPosition.UpperLeft);
    })
    .TightLayout()
    .Save("accessibility_colorblind.svg");
```

![Color-blind safe](../images/accessibility_colorblind.png)

## High-contrast theme

`Theme.HighContrast` meets WCAG AAA contrast ratios — ideal for presentations and printed reports:

```csharp
Plt.Create()
    .WithTitle("High-Contrast: Revenue Trend")
    .WithAltText("High-contrast line chart showing monthly revenue for 2025")
    .WithTheme(Theme.HighContrast)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, revenue, s => { s.Label = "Revenue"; s.LineWidth = 3.0; });
        ax.WithLegend(LegendPosition.UpperLeft);
    })
    .TightLayout()
    .Save("high_contrast.svg");
```

![High contrast](../images/accessibility_highcontrast.png)

## SVG semantics

All SVG exports automatically include:
- `role="img"` on the root `<svg>` element
- `<title>` from `.WithTitle()` and `<desc>` from `.WithDescription()`
- ARIA labels on all structural groups (axes, legend, series)
- Keyboard-navigable interactive features (pan, zoom, reset, brush-select, legend toggle)
