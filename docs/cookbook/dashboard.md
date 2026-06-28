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

## StatTileSeries — parameter reference

| Property | Type | Default | Description |
|---|---|---|---|
| `Value` | `double` | (constructor arg) | Headline number displayed in the tile. |
| `Label` | `string?` | `null` | Subtitle text shown beneath the headline. |
| `AccentColor` | `Color?` | `null` | Foreground colour of the headline number. `null` = theme cycle colour. |
| `Format` | `string` | `"0.##"` | .NET numeric format string applied to `Value` (invariant culture). |

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
- [Annotations](annotations.md) — threshold reference lines and breach shading
