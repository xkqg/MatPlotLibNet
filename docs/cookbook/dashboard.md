# Dashboard tiles & timelines

Single-panel KPI tiles and horizontal state timelines for operational dashboards,
fleet status views, and monitoring UIs. Both series types are chartless — they carry
no axes, just a formatted value or a coloured timeline — and are designed to be
composed via mosaic or subplot layouts.

## Stat tile — single KPI number

A `StatTileSeries` renders one big formatted headline number with an optional label
beneath it. The tile fills whatever plot region it occupies; use one subplot per tile.

```csharp
// "12 participants" tile — plain integer, no accent color
Plt.Create()
    .StatTile(12, t => t.Label = "Participants")
    .Save("tile_participants.svg");
```

The `Format` property follows standard .NET numeric format strings (invariant culture;
default `"0.##"` strips trailing zeros). Set `AccentColor` to draw the headline in a
warning or brand colour:

```csharp
// Alert count in red when non-zero
int alertCount = 3;
Plt.Create()
    .StatTile(alertCount, t =>
    {
        t.Label       = "Alerts";
        t.AccentColor = alertCount > 0 ? Colors.Red : null;
        t.Format      = "0";   // integer, no decimals
    })
    .Save("tile_alerts.svg");
```

### Multi-tile mosaic dashboard

Use `Plt.Mosaic(...)` to compose several KPI tiles in a grid. Each letter in the
pattern string becomes a panel:

```csharp
Plt.Mosaic("ABC\nDEF")
    .Panel('A', ax => ax.StatTile(12,       t => { t.Label = "Participants"; t.Format = "0"; }))
    .Panel('B', ax => ax.StatTile(0,        t => { t.Label = "Alerts";       t.Format = "0"; }))
    .Panel('C', ax => ax.StatTile(99.4,     t => { t.Label = "Uptime %";     t.Format = "0.0"; }))
    .Panel('D', ax => ax.StatTile(1_042,    t => { t.Label = "Messages/s";   t.Format = "0"; }))
    .Panel('E', ax => ax.StatTile(3.7,      t => { t.Label = "Latency ms";   t.Format = "0.0"; }))
    .Panel('F', ax => ax.StatTile(2,        t =>
    {
        t.Label       = "Errors";
        t.AccentColor = Colors.Red;
        t.Format      = "0";
    }))
    .WithTitle("Fleet Overview")
    .WithTheme(Theme.Dark)
    .WithSize(900, 300)
    .Save("fleet_tiles.svg");
```

### Combining a tile with a chart

Place a KPI tile beside a regular chart using `AddSubPlot` with a GridSpec:

```csharp
double[] x = Enumerable.Range(0, 60).Select(i => (double)i).ToArray();
double[] y = x.Select(v => 50 + 20 * Math.Sin(v * 0.3)).ToArray();

Plt.Create()
    .WithGridSpec(1, 2, widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax
        .Plot(x, y, s => s.Label = "Signal")
        .WithTitle("Live signal")
        .WithLegend())
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax
        .StatTile(y[^1], t =>
        {
            t.Label  = "Last value";
            t.Format = "0.0";
        }))
    .TightLayout()
    .Save("chart_with_tile.svg");
```

## State timeline — discrete coloured segments

A `StateTimelineSeries` renders a single-row horizontal bar divided into coloured
rectangles, one per `StateSegment`. Each segment spans `[Start, End]` in data units
and shows a centred text label. The Y range is fixed at `[0, 1]` so the bar fills
the full plot height.

The `StateSegment` record struct takes four positional arguments:

```csharp
// StateSegment(Start, End, Label, Color)
var segs = new[]
{
    new StateSegment(0,   30,  "Starting",  Colors.Gray),
    new StateSegment(30,  120, "Running",   Colors.Tab10Green),
    new StateSegment(120, 140, "Degraded",  Colors.Tab10Orange),
    new StateSegment(140, 180, "Running",   Colors.Tab10Green),
    new StateSegment(180, 200, "Stopped",   Colors.Red),
};

Plt.Create()
    .StateTimeline(segs, s => s.Label = "Worker A")
    .WithTitle("Worker state over time")
    .Save("state_timeline.svg");
```

### Date X-axis timeline

Pass OA-date X values and call `SetXDateAxis()` for automatic date tick formatting:

```csharp
DateTime t0 = new DateTime(2026, 6, 1);
var segs = new[]
{
    new StateSegment(t0.ToOADate(),                    t0.AddHours(6).ToOADate(),  "Starting", Colors.Gray),
    new StateSegment(t0.AddHours(6).ToOADate(),        t0.AddHours(20).ToOADate(), "Running",  Colors.Tab10Green),
    new StateSegment(t0.AddHours(20).ToOADate(),       t0.AddHours(22).ToOADate(), "Stopped",  Colors.Red),
    new StateSegment(t0.AddHours(22).ToOADate(),       t0.AddDays(1).ToOADate(),   "Running",  Colors.Tab10Green),
};

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .StateTimeline(segs)
        .SetXDateAxis()
        .WithTitle("Node uptime — June 1"))
    .Save("timeline_dates.svg");
```

### Stacked multi-row timelines (mosaic)

Stack one timeline per participant by assigning each its own subplot row:

```csharp
var workerA = new StateSegment[]
{
    new(0, 40,  "Running", Colors.Tab10Green),
    new(40, 55, "Stopped", Colors.Red),
    new(55, 100,"Running", Colors.Tab10Green),
};
var workerB = new StateSegment[]
{
    new(0,  20,  "Running",  Colors.Tab10Green),
    new(20, 35,  "Degraded", Colors.Tab10Orange),
    new(35, 100, "Running",  Colors.Tab10Green),
};
var workerC = new StateSegment[]
{
    new(0, 100, "Running", Colors.Tab10Green),
};

Plt.Mosaic("A\nB\nC")
    .Panel('A', ax => ax.StateTimeline(workerA, s => s.Label = "Worker A").WithTitle("Worker A"))
    .Panel('B', ax => ax.StateTimeline(workerB, s => s.Label = "Worker B").WithTitle("Worker B"))
    .Panel('C', ax => ax.StateTimeline(workerC, s => s.Label = "Worker C").WithTitle("Worker C"))
    .WithTitle("Fleet state timeline")
    .WithSize(900, 350)
    .TightLayout()
    .Save("fleet_timelines.svg");
```

### Tiles + timelines combined

Mix stat tiles and state timelines in a single dashboard:

```csharp
var alarmHistory = new StateSegment[]
{
    new(0,  10,  "OK",    Colors.Tab10Green),
    new(10, 14,  "ALARM", Colors.Red),
    new(14, 30,  "OK",    Colors.Tab10Green),
};

Plt.Mosaic("AAB\nAAC")
    .Panel('A', ax => ax
        .StateTimeline(alarmHistory)
        .WithTitle("Alarm history (minutes)"))
    .Panel('B', ax => ax.StatTile(2, t => { t.Label = "Alerts today"; t.AccentColor = Colors.Red; t.Format = "0"; }))
    .Panel('C', ax => ax.StatTile(99.3, t => { t.Label = "Uptime %"; t.Format = "0.0"; }))
    .WithTheme(Theme.Dark)
    .WithSize(900, 400)
    .TightLayout()
    .Save("ops_dashboard.svg");
```

## Full ops dashboard — tiles + timeline + thresholded chart

Combine all four v1.12.0 dashboard conveniences in one mosaic: a KPI tile row
(`StatTileSeries`), a `StateTimelineSeries` for service health, and a line chart
using the `Threshold(...)` convenience (dashed reference line + breach shading)
together with `WithLegendValues()` so the legend shows the live reading:

```csharp
var serviceHistory = new StateSegment[]
{
    new(0,  6,  "Up",       Colors.Tab10Green),
    new(6,  9,  "Degraded", Colors.Tab10Orange),
    new(9,  14, "Up",       Colors.Tab10Green),
    new(14, 15, "Down",     Colors.Red),
    new(15, 24, "Up",       Colors.Tab10Green),
};

double[] hours = Enumerable.Range(0, 48).Select(i => i * 0.5).ToArray();
double[] load  = hours.Select(h => 55 + 15 * Math.Sin(h * 0.4) + (h > 20 ? 20 : 0)).ToArray();

Plt.Create()
    .WithTitle("Ops Dashboard")
    .WithTheme(Theme.Dark)
    .WithSize(1000, 750)
    .WithGridSpec(3, 3, heightRatios: [1.0, 1.0, 1.6])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax
        .StatTile(12, t => { t.Label = "Participants"; t.Format = "0"; }))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax
        .StatTile(1, t => { t.Label = "Alerts"; t.AccentColor = Colors.Red; t.Format = "0"; }))
    .AddSubPlot(GridPosition.Single(0, 2), ax => ax
        .StatTile(99.4, t => { t.Label = "Uptime %"; t.Format = "0.0"; }))
    .AddSubPlot(new GridPosition(1, 2, 0, 3), ax => ax
        .StateTimeline(serviceHistory, s => s.Label = "API service")
        .WithTitle("Service state — last 24h"))
    .AddSubPlot(new GridPosition(2, 3, 0, 3), ax => ax
        .Plot(hours, load, s => s.Label = "CPU load %")
        .Threshold(80.0, Orientation.Horizontal, ThresholdBreach.Above,
            color: Colors.Red, label: "Alarm")
        .SetXLabel("Hour")
        .SetYLabel("Load %")
        .WithLegend()
        .WithLegendValues())
    .TightLayout()
    .Save("ops_dashboard_full.svg");
```

![Ops dashboard](../images/ops_dashboard_full.png)

See [Threshold convenience](annotations.md#threshold-convenience) and
[Legend value display](line-charts.md#legend-value-display) for the full parameter
reference of `Threshold(...)` and `WithLegendValues()`.

## Operations dashboard — `Plt.OpsDashboard()`

A control-room screen is not a chart with more charts on it. It is a screen someone must be able to sit
in front of for eight hours and still notice the one thing that changes. That is a design problem with a
long-settled literature behind it, and this template follows it rather than inventing its own.

**Colour is reserved.** At rest the screen carries none — not even green. The moment a healthy state is
coloured, the abnormal one has nothing left to stand out against, and a wall of cheerful tiles is a wall
nobody looks at. Colour appears only when something needs attention, and it comes out of the theme's
`AlarmPalette`: Okabe-Ito amber for *look at this* and vermillion for *act now*, chosen because roughly
eight percent of men cannot separate red from green and these two stay apart for all of them. Colour is
never the sole carrier of meaning — every coloured mark says the same thing in words too.

**A silent source is hatched, not coloured.** On a monitored fleet, "I can no longer see you" is the most
common failure and it is *not* the same failure as "you are broken". A dashboard that paints them the same
lies exactly when it matters. `StatTileSeries.Hatch` and `StateSegment.Hatch` exist for this.

**The caller owns the clock.** `WithWindow(end, span)` takes the end instant from you. The library never
reads `DateTime.Now` — a figure that depends on the wall clock cannot be tested, cannot be replayed, and
cannot render a dashboard for any moment but this one.

```csharp
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

var now = DateTime.UtcNow;                       // the caller's clock, not the library's
var theme = Theme.OpsNight;                      // or OpsPanel / OpsWarm / OpsContrast

Plt.OpsDashboard()
    .WithTitle("Synapse — federation")
    .WithWindow(now, TimeSpan.FromMinutes(5))    // ONE window, shared by every time panel
    .WithNormalBand(2200, 2700)

    // A tile carries value + target + gap + trend. A bare big number is a failed pattern: without a
    // comparative the reader cannot tell whether it is good or bad, and must supply the missing context
    // from memory — which they mostly cannot.
    .AddTile(15, t =>
    {
        t.Label = "Buses";
        t.Caption = "all 15 normal";             // no accent → the tile wears the theme's neutral shade
    })
    .AddTile(28.1, t =>
    {
        t.Label = "RFx p99";
        t.Format = "0.0' ms'";
        t.Target = 25;
        t.Caption = "target 25 ms · +3.1 over";
        t.Trend = p99History;                    // an inline Tufte sparkline: no axis, no frame, no ticks
        t.AccentColor = theme.Alarm.Warning;     // colour ONLY because it is out of band
    })
    .AddTile(0, t =>
    {
        t.Label = "Exchange";
        t.Caption = "no contact";
        t.Hatch = HatchPattern.ForwardDiagonal;  // unknown ≠ broken — a pattern, never a colour
    })
    .AddTile(3.8, t =>
    {
        t.Label = "Nexus";
        t.Format = "0.0' µs/msg'";
        t.Target = 6;
        // A caption may answer TWO questions — "is this good or bad" and "measured over what". Newlines
        // stack, so the second answer gets its own row instead of running the first one off the tile.
        t.Caption = "threshold 6" + Environment.NewLine + "2148 msg · 1 s";
    })

    .AddTimeline(busSegments, l => l.Label = "Service Bus")
    .AddTrend(clock, publish, s => s.Label = "publish")
    .AddTrend(clock, consume, s => s.Label = "consume")
    .ConfigureTrend(ax => ax.SetYLabel("messages / s"))
    .Build()
    .WithTheme(theme)
    .Save("control-room.svg");
```

### The rolling axis

`WithWindow` pins **exact** bounds and lets the locator round only the **ticks**. That distinction is the
whole difference between a chart that glides and one that lurches: an auto axis expands its bounds outward
to the nearest nice number, so it stands perfectly still while the data grows into it and then jumps a
whole step at once. Pinned bounds slide continuously; round ticks glide out of frame with the data they
belong to.

### Bullet graphs instead of dials

`BulletGraphSeries` is Stephen Few's designed replacement for the radial gauge, which the
high-performance-HMI literature rejects: a dial spends a quarter of a panel to say what a bar says in a
fifth of it, and it cannot be stacked. The bullet keeps the measure, the target and the qualitative ranges
in one thin strip — and its bands are one hue at varying intensity, never red/amber/green, so they neither
exclude colour-blind readers nor spend the alarm palette on a backdrop.

```csharp
ax.Bullet(2412, b =>
{
    b.Target = 2500;
    b.Bands = [new(1800, band1), new(2200, band2), new(2800, band3)];
});
```

### A tile that leads somewhere — progressive disclosure

A wall shows the number; the detail lives UNDER it and opens on a click. The tile says it leads somewhere three
ways at once, never by colour alone: a chevron in its corner (▸ closed, ▾ open), the pointer cursor, and an
`aria-label`. `Url` is matplotlib's `Artist.set_url` idiom — the whole tile becomes an SVG `<a href>`, so it
needs no script (it works in a static file, inline in a Blazor page, in a saved SVG), it is focusable and
Enter-activatable for free, and the open/closed state lives in the URL — which a wall that redraws its tiles
twice a second cannot lose, and an operator can paste to a colleague.

```csharp
bool open = panel == "processes";
dashboard.AddTile(running, t =>
{
    t.Label = "Processes";
    t.Url = open ? "/" : "/?panel=processes";   // the anchor toggles the detail
    t.Expanded = open;                          // ▾ while the detail is shown below
});
```

In Blazor (`InteractiveServer`), the router intercepts a same-origin SVG anchor exactly as it does an HTML one
(`findAnchorTarget` matches `SVGAElement`), so the click is a client-side navigation: the circuit survives,
and a `[SupplyParameterFromQuery(Name = "panel")] string? Panel` on the page re-renders with the detail open.

### Small multiples — twenty processes, compared

Beyond five or six lines on one axes nobody can tell them apart, and the legend becomes the picture. Small
multiples put the name INSIDE each panel and give every panel the same axes, so twenty processes read as twenty
comparable shapes (Task Manager's per-core grid, Grafana's repeated panel):

```csharp
var grid = Plt.SmallMultiples()
    .WithMaxCols(4)                                  // wraps balanced: 5 panels read as 3+2, never 4+1
    .WithPanelSize(300, 110)
    .WithSharedYLimits(0, 100)                       // the SAME y on every panel — that is what compares
    .WithWindow(now, TimeSpan.FromMinutes(3))        // the same pinned x, time-aware ticks
    .ConfigurePanel(ax => ax.AxHLine(100, r => { r.Label = "100 % of one core"; r.Color = theme.Alarm.Warning; }));
foreach (var process in processes)
{
    grid.AddPanel($"{process.Name} · {process.Cpu:0} %", process.Times, process.Cpu);
}
Figure figure = grid.Build().WithTheme(Theme.OpsNight).Build();
```

The label is an axes-fraction annotation (`AnnotationCoordinates.AxesFraction`), so it sits top-left whatever
the limits are. **A threshold on a time axis is `AxHLine(value, r => r.Label = …)`**, never
`Threshold(…, label)`: the latter anchors its label at x = 0 in DATA coordinates, which on a date axis is the
year 1899 — off the canvas.

### A treemap of the fleet — area is size, colour is load

A treemap rect carries two variables: its AREA from `TreeNode.Value` and its COLOUR from `TreeNode.ColorValue`
through the series' normalizer and map. Put the slow-moving quantity on the area (memory — so the layout stays
put; the squarified layout sorts by value and would reshuffle every beat if the area were CPU) and the live one
on the colour:

```csharp
var fleet = new TreeNode
{
    Label = "Ait",
    Children = processes.Select(p => new TreeNode
    {
        Label = $"{p.Name} · {p.Cpu:0} %",
        Value = p.WorkingSetMb,            // area
        ColorValue = p.Cpu,                // colour: % of ONE core
        Hatch = p.Silent ? HatchPattern.ForwardDiagonal : HatchPattern.None,   // "no information", never a colour
    }).ToList(),
};
ax.Treemap(fleet, s =>
{
    s.ColorMap = theme.Alarm.Ramp;         // resting → warning (50 %) → critical (100 %)
    s.VMin = 0; s.VMax = 100;
    s.LabelFit = TreemapLabelFit.Fit;      // a label wider than its rect is not painted across the neighbours
});
```

Measured at 1400×300: nine processes label cleanly; at twenty, `LabelFit.Always` painted nine labels across
their neighbours — a wall above ~12 processes needs height or `Fit`/`Truncate`. An EMPTY tree draws nothing.

### A tree grid — the numbers a treemap cannot show

Once the leaves are many and small, area stops saying anything: on a live fleet two lanes of twenty-three
carried every message in an hour, which as a treemap is two rectangles and twenty-one slivers. Rows compare
exactly, and they are the shape ARIA names (`treegrid`):

```csharp
TreeGridRow[] rows =
[
    new("Ait", ["24 %", "25 323", "10 404 KiB"]) { Expanded = true },
    new("Ait.Binance", ["10 %", "14 920", "6 972 KiB"])
        { Depth = 1, Url = "/?process=Ait.Binance", Expanded = false },   // a link, so expanding needs no script
    new("BinanceKline", ["", "14 920", "6 972 KiB"]) { Depth = 2 },
];

Plt.Create().WithSize(1400, 300).WithTheme(Theme.OpsNight)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.TreeGrid(rows, s => { s.ColumnHeaders = ["CPU", "Messages", "Traffic"]; s.ColumnWidth = 140; });
        ax.HideAllAxes();
    })
    .Build();
```

Values are right-aligned per column so digits line up; a row with a `Url` becomes an `<a href>` carrying
`aria-expanded`, and every row carries `aria-level`. Rows past the region's edge are dropped rather than drawn
over the panel below.

### A cell that carries its number

`TreeNode.Headline` puts the measure under the name and larger — the stat tile's anatomy inside a treemap cell:

```csharp
new TreeNode { Label = "Ait.Cortex", Headline = "13 %", Value = 1, ColorValue = 13 }
```

The headline is dropped before the name is: a number without its subject says nothing.

### A log axis for latencies

`SetYScale(AxisScale.Log)` ranges over the POSITIVE values only (a window that priced 0 µs is masked, not drawn),
pads in log space and installs decade ticks (`10¹`, `10²`) by itself. For data that legitimately includes zero,
`SetYScaleSymLog(linthresh)` keeps a linear band around it instead.

### The control-room sample

`Samples/MatPlotLibNet.Samples.ControlRoom` serves a simulated federation of 15 buses, and descends into it:
fleet → bus → process → lanes, with the level you leave kept as a rail so a sibling is one click away.
Measurement runs at a fixed 250 ms and never waits for a render; the refresh knob throttles the *charts*
only — the tiles never slow down, because history may lag and a warning may not. The window (1 / 5 / 15 min,
1 hour) re-buckets the same measurements: rates keep a min/max envelope so a five-second burst survives a
one-minute bucket, and percentiles carry their **maximum**, because a p99 cannot be averaged and averaging
is precisely how a dashboard hides the spike you came to look for.

Click the **Processes** tile: it is a link (`StatTileSeries.Url`, chevron ▸/▾, `aria-expanded`, a focus ring) that
opens a drill-down under the tile row — composition NOW (an equal-cell grid per bus, colour = CPU as % of one
core through `AlarmPalette.Ramp`, a silent bus hatched, `LabelFit.Fit`) above trend SINCE WHEN (small multiples
of the hottest eight, `Plt.SmallMultiples()`, one shared 0–150 % axis, a line at one core). The open/closed
state is the URL (`?panel=processes`), so it survives every redraw and can be pasted; the two panels are
published only while a tab has them open (`IChartSubscriptions`).

The alarm conditioning — on-delay, deadband, off-delay, the worst-child roll-up and the staleness clock —
lives in the **sample**, not in this library. A charting library that decides when something counts as
broken has started holding opinions about a domain it cannot see.

## StatTileSeries — parameter reference

| Property | Type | Default | Description |
|---|---|---|---|
| `Value` | `double` | (constructor arg) | Headline number displayed in the tile. |
| `Label` | `string?` | `null` | Subtitle text shown beneath the headline. |
| `AccentColor` | `Color?` | `null` | Foreground colour of the headline number. `null` = theme cycle colour. |
| `Format` | `string` | `"0.##"` | .NET numeric format string applied to `Value` (invariant culture). |
| `Target` | `double?` | `null` | The comparative the value is measured against. |
| `Caption` | `string?` | `null` | The gap line under the label; newlines stack, long lines wrap to the tile. |
| `Trend` | `IReadOnlyList<double>?` | `null` | Inline sparkline in the tile's lower fifth. |
| `TrendColor` | `Color?` | `null` | The sparkline's ink; `null` = the headline colour. |
| `Hatch` / `HatchColor` | `HatchPattern` / `Color?` | `None` / `null` | "No information" — a pattern, never a colour. |
| `Url` | `string?` | `null` | Where the tile leads: the whole tile becomes an SVG `<a href>` with a chevron, pointer and `aria-label`. |
| `Expanded` | `bool` | `false` | Chevron direction: ▸ closed, ▾ open. Ignored without `Url`. |

## StateTimelineSeries — parameter reference

| Property | Type | Description |
|---|---|---|
| `Segments` | `IReadOnlyList<StateSegment>` | The ordered segments that make up the timeline. |

### StateSegment fields

| Field | Type | Description |
|---|---|---|
| `Start` | `double` | X-axis start of the segment (data units). |
| `End` | `double` | X-axis end of the segment (data units). |
| `Label` | `string` | Centred text rendered inside the segment rectangle. |
| `Color` | `Color` | Fill colour of the segment rectangle. |

## See also

- [Subplots & GridSpec](subplots.md) — mosaic and GridSpec layout, row/column ratios
- [Financial Charts](financial.md) — OHLC/candlestick dashboards with indicator subplots
- [Annotations](annotations.md) — threshold reference lines and breach shading, full `Threshold(...)` parameter reference
- [Line Charts](line-charts.md) — legend value display (`WithLegendValues()`), full parameter reference
