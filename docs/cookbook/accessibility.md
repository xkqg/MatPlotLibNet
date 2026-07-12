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

## Never encode meaning in colour alone

Roughly eight percent of men have a red-green colour deficiency. A chart that says "red is bad, green is
fine" and nothing else has simply not communicated with them — and on a monitoring wall, where the whole
point is that a glance is enough, that is not a cosmetic failure.

Two mechanisms in the library exist for exactly this, and both are worth reaching for outside a dashboard too.

**A hatch carries meaning that colour cannot.** `HatchPattern` on `ShapeStyle` — and therefore on
`BarSeries`, `AreaSeries`, `HistogramSeries`, `StackedAreaSeries`, `PieSeries`, `StatTileSeries` and
`StateSegment` — paints a pattern over a fill. It survives greyscale printing, a colour-blind reader, and a
bad projector, none of which a hue does.

```csharp
// "No contact" is hatched, not coloured: the source is silent, not broken — a different fault, and a
// dashboard that paints them the same lies exactly when it matters.
ax.StatTile(0, t =>
{
    t.Label = "Exchange";
    t.Caption = "no contact";
    t.Hatch = HatchPattern.ForwardDiagonal;
});
```

The SVG and the raster (Skia PNG/PDF) backends paint the same pattern. Where a backend cannot — the MAUI
canvas has no hatch primitive — it reports the omission on `ChartDiagnostics` rather than dropping it in
silence, so an export that quietly loses a mark is something you can see rather than something you discover
later.

**The alarm palette is colour-blind safe by construction.** `Theme.Alarm` (`AlarmPalette`) names Okabe-Ito
amber for *attention* and vermillion for *critical*, which stay distinguishable under every common form of
colour-vision deficiency. It also names the neutral shade a resting state wears — because the strongest
accessibility measure of all is not spending colour on states that do not need it: when the normal state is
uncoloured, the abnormal one does not have to compete for attention with anything.

## Motion

If a display uses motion to mean something, use it for exactly one thing and use it rarely. The alarm
convention reserves a slow (roughly 1 Hz) pulse for a single meaning: *this is new and nobody has
acknowledged it yet*. Once acknowledged, it goes steady and stays coloured while the problem lasts.

A screen that flickers constantly is not a screen with a lot of problems — it is a screen nobody has looked
at in a while. And any animation that runs forever gets tuned out within a week, which is a worse outcome
than never having animated at all. Where a page animates, honour `prefers-reduced-motion`.
