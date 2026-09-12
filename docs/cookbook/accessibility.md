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
- `role="img"` on the root `<svg>` element — or `role="group"` when the figure contains a hyperlink
- `<title>` from `.WithAltText()`, else `.WithTitle()`, else the tile row's own labels, and `<desc>` from
  `.WithDescription()`, referenced by `aria-labelledby` / `aria-describedby`
- `aria-label` on the structural groups (axes, legend, series, selection rectangle)
- Keyboard-navigable interactive features (pan, zoom, reset, brush-select, legend toggle)

**Read the second and third bullets together, because they interact.** `role="img"` makes the whole SVG one
node to a screen reader: WAI-ARIA's children-presentational rule strips every descendant, so the `aria-label`
on a series group is markup that is never announced. It is there for tooling, for a hyperlinked figure (where
the role is `group` and the labels *are* reachable) and for a future in which the figure is navigable — not
for today's screen-reader user. What that user hears is the `<title>` and the `<desc>`: one sentence about a
picture that may hold four hundred numbers.

That is the whole case for the next section. A one-sentence summary is what WCAG 1.1.1 calls a short text
alternative, and for a chart — a complex image — the guideline asks for a *long* one as well: the data.

## The data table

`figure.ToDataTables()` returns the figure's data as one `ChartDataTable` per group of series, each with a
caption, typed columns and rows of cells, and each able to write itself three ways:

```csharp
var figure = Plt.Create()
    .WithTitle("Monthly Revenue vs Cost (2025)")
    .WithAltText("Line chart: revenue and cost trends over 12 months of 2025")
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.SetXLabel("Month");
        ax.Plot(x, revenue, s => s.Label = "Revenue");
        ax.Plot(x, cost,    s => s.Label = "Cost");
    })
    .Build();

foreach (var table in figure.ToDataTables())
{
    string html     = table.ToHtml();      // <table> with <caption> and scoped <th>, capped at 1000 rows
    string markdown = table.ToMarkdown();  // for a README, a chat transcript, or a model
    string csv      = table.ToCsv();       // RFC 4180, CRLF, never capped — this one is a download
}
```

Both series are plotted against the same x values, so they are **one** table with three columns — `Month`,
`Revenue`, `Cost` — not two tables the reader has to align by eye. Series that do not share an x each get
their own table. The x column takes the axis label when there is one, and reads as dates when the axis is a
date axis.

The caption is `figure.AccessibleName()` — the alt text, else the title, else the tile labels — joined with
the subplot title and the series label, so the table and the SVG `<title>` can never name the same picture
differently.

### What the table contains

The table is the **data**, not what the renderer drew. A line with `MaxDisplayPoints` set draws a
downsampled path and a streaming series draws its viewport; both tabulate every point they hold. The table
is the resolution the reader would have had if they could see it — and then some.

Every series type answers for itself. An XY series is x and y; an OHLC series is date, open, high, low,
close; a heatmap is long-form row/column/value, which stays readable at any width where a wide grid does not;
a histogram is its bins; a treemap is its tree flattened depth-first with the depth kept as a column.

**Two of the 83 series types have no tabular form and produce no table:** `QuiverKeySeries` (a reference arrow
— an annotation explaining the scale of a vector field, not a measurement) and `Text3DSeries` (text placed at a
point in a 3-D scene). They return `null` and simply do not appear. A figure whose every series is one of those
returns an empty list from `ToDataTables()`, and `ShowDataTable` renders no disclosure at all rather than an
empty one — a control that promises data and opens on nothing is worse than no control.

### Putting it on the page

| Host | How |
|------|-----|
| Blazor | `<MplChart Figure="fig" ShowDataTable="true" />` — a `<details>` disclosure after the chart, in every display mode |
| ASP.NET Core | `app.MapChartTableEndpoint("/chart/table", ctx => figure)` beside `MapChartSvgEndpoint`; returns an HTML fragment as `text/html; charset=utf-8` |
| GraphQL | the `chartDataTable(chartId: …)` field beside `chartSvg` |
| MCP | the `chart_data_table` tool — markdown, so a model can read values off a chart it can only see |
| Anything else | `table.ToHtml()` into your own page, `table.ToCsv()` behind a download link |

A disclosure (`<details>`/`<summary>`) placed immediately after the chart is the placement the WCAG technique
for complex images names: it is in the reading order at the point where the reader needs it, and it costs a
sighted reader nothing but one closed line. That is what `ShowDataTable` renders. It defaults to `false`
because it is your page's layout, not the library's — but there is no accessibility argument for leaving it
off.

```razor
@* The table is not a fallback for a broken image. It is the same information, in a form that survives
   a screen reader, a text browser, a copy-paste into a spreadsheet and a printout. *@
<MplChart Figure="@_figure" ShowDataTable="true" />
```

### Where it stops

HTML and markdown cap at 1000 rows and end with a "… N more rows" line, because a table nobody can page
through is not an alternative either — and because a 50,000-row `<table>` is a page that never finishes
laying out. `ToCsv()` never caps: a download has no reading order to protect, and truncating a file is how
data quietly goes missing. When the cap bites, offer the CSV.

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
