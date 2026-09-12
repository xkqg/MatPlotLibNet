---
title: "Accessible charts in C#: alt text and a data table"
description: "Make C# charts readable for everyone: a color-blind-safe palette, alt text, and a data table beside the picture for readers who cannot see it."
---

# Accessibility

## Color-blind safe palette

`Theme.ColorBlindSafe` uses the Okabe-Ito palette. Its colors stay distinguishable for people with all forms of
color vision deficiency:

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

`Theme.HighContrast` meets WCAG AAA contrast ratios. It suits presentations and printed reports:

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

**Read the second and third bullets together, because they interact.** The root role limits what the group
labels do. With `role="img"`, a screen reader treats the whole SVG as one node. The WAI-ARIA
children-presentational rule removes every descendant, so the `aria-label` on a series group is never
announced. Those labels serve tooling, a hyperlinked figure (where the role is `group` and the labels *are*
reachable) and a future in which the figure is navigable. They do not serve today's screen-reader user. That
user hears only the `<title>` and the `<desc>`: one sentence about a picture that may hold four hundred
numbers.

That is why the next section exists. WCAG 1.1.1 calls a one-sentence summary a short text alternative. For a
complex image such as a chart, the guideline also asks for a *long* text alternative: the data.

## The data table

`figure.ToDataTables()` returns the figure's data as one `ChartDataTable` per group of series. Each table has a
caption, typed columns and rows of cells. Each table can write itself in three formats:

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

Both series are plotted against the same x values, so they form **one** table with three columns: `Month`,
`Revenue` and `Cost`. The reader does not have to align two tables by eye. Series that do not share an x each
get their own table. The x column takes the axis label when there is one, and prints dates when the axis is a
date axis.

The caption is `figure.AccessibleName()` (the alt text, else the title, else the tile labels) joined with the
subplot title and the series label. The table and the SVG `<title>` therefore always give the same figure the
same name.

### What the table contains

The table holds the **data**, not what the renderer drew. A line with `MaxDisplayPoints` set draws a
downsampled path, and a streaming series draws only its viewport. Both tabulate every point they hold. The
table gives the reader everything the drawing shows, and more.

Each series type defines its own columns. An XY series gives x and y. An OHLC series gives date, open, high,
low and close. A heatmap gives a long-form table with row, column and value columns; it stays readable at any
width, where a wide grid does not. A histogram gives its bins. A treemap gives its tree flattened depth-first,
with the depth kept as a column.

**Two of the 83 series types have no tabular form and produce no table:** `QuiverKeySeries` (a reference arrow
that explains the scale of a vector field; it is an annotation, not a measurement) and `Text3DSeries` (text
placed at a point in a 3-D scene). They return `null` and are left out. A figure whose series are all of those
two types returns an empty list from `ToDataTables()`. `ShowDataTable` then renders no disclosure at all rather
than an empty one, so the reader is never offered a table that turns out to be empty.

### A dashboard

A control-room dashboard is the hardest case to get right, and it is where a naive table is least useful.
Three rules apply:

**A tile tabulates all of its parts.** `StatTileSeries` tables as `label · value · target · caption · trend`.
A tile exists to show a value next to its target and its trend, so the table carries those too, not the number
alone. A part the tile does not have is an empty cell, not a zero.

**A row of tiles is one table.** Every tile sits in its own subplot, but that is layout, not data. Neighbouring
subplots that each produce the same single row are merged into one table with a row per tile. The reader gets
the row the operator sees, not five one-row grids to join by hand.

**A position on a date axis prints as a time.** A state timeline's `start` and `end` are x coordinates, and
`Plt.OpsDashboard()` pins a date axis on the shared window. The table therefore prints `2026-09-12 07:58:20`,
not `46277.332`. A cell keeps the precision it was given: a midnight prints as a date, a whole minute prints to
the minute, and a thirty-second sample prints to the second. The last case matters more than it seems: rounding
to the minute would make two consecutive samples identical.

```csharp
// The dashboard the cookbook builds above, read rather than seen:
foreach (var table in dashboard.ToDataTables())
{
    Console.WriteLine(table.ToMarkdown());
}

// **Synapse — federation**
// | label   | value | target | caption            | trend                  |
// | ---     | ---   | ---    | ---                | ---                    |
// | Buses   | 15    |        | all 15 normal      |                        |
// | RFx p99 | 28.1  | 25     | target 25 ms · +3.1| 24.1, 24.8, 25.3, 26…  |
//
// **Synapse — federation — Exchange feed**
// | state    | start               | end                 |
// | ---      | ---                 | ---                 |
// | normal   | 2026-09-12 07:55    | 2026-09-12 07:57    |
// | degraded | 2026-09-12 07:57    | 2026-09-12 07:58:20 |
```

### Putting it on the page

| Host | How |
|------|-----|
| Blazor | `<MplChart Figure="fig" ShowDataTable="true" />` renders a `<details>` disclosure after the chart, in every display mode |
| ASP.NET Core | `app.MapChartTableEndpoint("/chart/table", ctx => figure)` beside `MapChartSvgEndpoint`; returns an HTML fragment as `text/html; charset=utf-8` |
| GraphQL | the `chartDataTable(chartId: …)` field beside `chartSvg` |
| MCP | the `chart_data_table` tool; it returns markdown, so a model can read the values from a chart it can otherwise only see |
| Anything else | `table.ToHtml()` into your own page, `table.ToCsv()` behind a download link |

Put the table in a disclosure (`<details>`/`<summary>`) immediately after the chart. That is the placement the
WCAG technique for complex images names. The table then sits in the reading order at the point where the reader
needs it, and a sighted reader sees only one closed line. That is what `ShowDataTable` renders. It defaults to
`false` because the layout of the page is yours, not the library's. There is no accessibility reason to leave
it off.

```razor
@* The table is not a fallback for a broken image. It is the same information, in a form that survives
   a screen reader, a text browser, a copy-paste into a spreadsheet and a printout. *@
<MplChart Figure="@_figure" ShowDataTable="true" />
```

### Where it stops

HTML and markdown cap at 1000 rows and end with a "… N more rows" line. A table nobody can page through is not
a usable alternative either, and a 50,000-row `<table>` is a page that never finishes laying out. `ToCsv()`
never caps: a download has no reading order to protect, and a truncated file loses data without anyone
noticing. When the cap applies, offer the CSV.

## Never encode meaning in colour alone

Roughly eight percent of men have a red-green colour deficiency. A chart that says only "red is bad, green is
fine" has not communicated with them at all. On a monitoring wall, where a glance has to be enough, that is not
a cosmetic failure.

The library has two mechanisms for exactly this. Both are worth using outside a dashboard too.

**A hatch pattern carries meaning that colour alone cannot.** `HatchPattern` on `ShapeStyle`, and therefore on
`BarSeries`, `AreaSeries`, `HistogramSeries`, `StackedAreaSeries`, `PieSeries`, `StatTileSeries` and
`StateSegment`, paints a pattern over a fill. The pattern stays visible in greyscale print, for a colour-blind
reader and on a bad projector, where colour alone does not.

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

The SVG backend and the raster (Skia PNG/PDF) backends paint the same pattern. Where a backend cannot paint a
hatch (the MAUI canvas has no hatch primitive), it reports the omission on `ChartDiagnostics` instead of
dropping the hatch silently. You can then see that an export lost a mark, instead of discovering it later.

**The alarm palette is colour-blind safe by construction.** `Theme.Alarm` (`AlarmPalette`) names Okabe-Ito
amber for *attention* and vermillion for *critical*. The two stay distinguishable under every common form of
colour-vision deficiency. It also names the neutral shade for a resting state. The strongest accessibility
measure of all is to spend no colour on states that do not need it: when the normal state is uncoloured, the
abnormal one does not have to compete for attention.

## Motion

If a display uses motion to carry meaning, use it for exactly one thing, and use it rarely. The alarm
convention reserves a slow pulse (roughly 1 Hz) for a single meaning: *this is new and nobody has acknowledged
it yet*. Once acknowledged, the alarm stops pulsing and stays coloured while the problem lasts.

Constant flickering usually does not mean the system has that many problems; it means nobody has acknowledged
the existing alarms in a while. Within a week, viewers stop noticing an animation that runs all the time, and
that is a worse outcome than never having animated at all. Where a page animates,
honour `prefers-reduced-motion`.
