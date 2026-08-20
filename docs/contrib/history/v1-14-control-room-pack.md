> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.14 — Control-Room Pack

Chart-type additions and property-level extensions that turn `FigureTemplates.OpsDashboard` (shipped as a first
iteration in 1.13.1) into a control-room display that follows established HMI practice rather than generic dashboard
habit. Design-of-record: the exception-first layout, the colour/motion budget, the staleness rule and the
latency ⟂ throughput split, all grounded in ISA-101 / ISA-18.2 / EEMUA-191 and in Few's and Tufte's tile work.

**Target:** `main`, v1.14.0 (new public API → MINOR).
**Coverage gate:** ≥90 % line AND ≥90 % branch per public class, `-Strict`.
**Everything is fluent.** No new static template with a parameter pile: the dashboard is composed, not configured.

---

## What already exists (reuse — do not rebuild)

Grep confirmed before planning:

| Asset | Where | Use for |
|---|---|---|
| `FillBetween(x, y, y2)` → `AreaSeries` | `AxesBuilder.cs:874` | the learned normal band **and** the min/max envelope — no new band series needed |
| `SetXLim` + `SetXDateFormat` + `AutoDateLocator` | `AxesBuilder.cs:78/371`; `Rendering/TickLocators/` | the pinned rolling clock axis; the locator already picks its granularity from the visible span |
| `StatTileSeries`, `StateTimelineSeries` | `Models/Series/Categorical/` | the tile and the timeline row — extend, do not duplicate |
| `SpanRegion` (AxHSpan/AxVSpan) | `Models/SpanRegion.cs` | a *static* normal band, where the band is a constant rather than learned |

### ⚠ Correction — `HatchPattern` is a hollow API (found while grounding this plan)

The first draft of this plan claimed "the hatch enum **and its renderer** already exist". That is **false**, and the
correction is load-bearing enough to change the phasing.

`Styling/HatchPattern.cs` declares the enum, and `BarSeries.cs:52/54` + `AreaSeries.cs:25/27` expose `Hatch` +
`HatchColor` — but **nothing consumes them**. The only sites that read `.Hatch` anywhere in the repo are
`Tst/MatPlotLibNet/Models/Series/HatchSeriesTests.cs`, which assert the property's *default value* and nothing else. No
renderer emits a pattern; the SVG renderer contains no `<pattern>` element; the Skia backend has no hatch path; the DTO
does not carry it. **A caller who sets `bar.Hatch = HatchPattern.ForwardDiagonal` today gets a plain solid fill and no
error.** It is decorative public API.

So the "no contact" hatch is not a property add — it is a rendering feature that must be built. **The seam it belongs on
is already there:** every shape-drawing operation on `IRenderContext` (`DrawRectangle`, `DrawPolygon`, `DrawPath`, …)
takes a `ShapeStyle` — `Styling/ValueObjects/ShapeStyle.cs`, a `readonly record struct (Color? Fill, Color? Stroke,
double StrokeThickness)` whose own XML doc says it "bundles the optional fill and stroke properties that travel together
through every shape-drawing operation". A fill *pattern* is a fill property. It goes there, and every backend picks it
up through the one path they all already use — **no new interface method, no breaking change to `IRenderContext`**.

1. **`ShapeStyle` gains `Hatch` + `HatchColor` as `init` properties** — additive, non-breaking. A fourth *positional*
   parameter would break every construction site in the repo; `init` properties do not. (Same call as
   `StateSegment.Hatch` below — one rule, applied twice.)
2. **SVG**: a `SvgHatchRegistry` that mirrors the existing `SvgHatchRegistry`-shaped precedent —
   `Rendering/Svg/SvgGradientRegistry.cs` already owns `<defs>` id-allocation + emission for gradients as an SRP class
   writing into the shared buffer. Copy that shape exactly for `<pattern>` defs: deterministic ids, de-duplicated per
   (pattern, colour) pair. Do not invent a second defs mechanism.
3. **Skia (PNG/PDF)**: an `SKShader` tile in `SkiaRenderContext`, so the raster backend reaches parity. Without it a
   hatch is visible on screen and invisible in an exported PNG — the worst divergence there is for an operator who
   screenshots the wall.
4. **Serialization**: `Hatch` (as **nullable** `HatchPattern?` — see grounding fact 4) + `HatchColor` round-trip.
5. **`BarSeries` and `AreaSeries` start working as a side effect.** That is a bug fix to shipped public API and belongs
   in the CHANGELOG under **Fixed**, not folded silently into a feature.

`StatTileSeries` and `StateSegment` then take the same property pair — the *third* and *fourth* consumer. At that point
the hatch machinery is genuinely shared, not speculative.

### Grounding — four facts verified in source before any code is written

1. **`HatchPattern` is hollow.** The only reads of `.Hatch` in the whole repo are default-value assertions in
   `Tst/MatPlotLibNet/Models/Series/HatchSeriesTests.cs`. No renderer, no DTO. Setting it does nothing. (See above.)
2. **`GaugeSeries` loses its bands on round-trip.** `GaugeSeries.cs:43` emits `Type/GaugeValue/GaugeMin/GaugeMax/
   NeedleColor` — and no bands. A gauge serialized and rehydrated comes back with its threshold bands gone, silently.
   Same root cause as (1): model state that reaches no sink. It shares the fix and therefore ships in this pack —
   "out-of-scope" is not a parking lot for an item with the same root cause as in-scope work.
3. **`ISeriesVisitor` uses default no-op interface methods** (`ISeriesVisitor.cs:276/282`, `[ExcludeFromCodeCoverage]
   void Visit(StatTileSeries, RenderArea) { }`). Good news: a new series does **not** break the other backends. Bad
   news: a backend that forgets to override the new `Visit` **renders nothing, silently, with no error**. Every new
   series therefore needs a test that asserts its marks are actually present in the emitted SVG — a "does not throw"
   test would pass on a blank canvas.
4. **`SeriesDto` omits nulls** (`ChartSerializer.cs:23`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`).
   So **nullable** additive DTO fields leave the 76-discriminator golden corpus byte-identical — no regen needed. But a
   **non-nullable** field (an enum like `HatchPattern Hatch` defaulting to `None`) always serializes and **would** change
   the bytes of every series carrying it. Every field this pack adds to the DTO must therefore be nullable
   (`HatchPattern?`), or the golden corpus regenerates for no reason.

The other thing the library genuinely cannot draw today is a **bullet graph**.

---

## 1. `BulletGraphSeries` — the only new series

Stephen Few's bullet graph, the designed replacement for the radial gauge (which the high-performance-HMI literature
rejects outright: a dial spends screen area to say what a bar says in a fifth of it). Three layered parts in one thin
strip:

* the **feature bar** — the actual measure;
* a perpendicular **target tick** — the comparative;
* two or three **qualitative bands** behind it — poor / satisfactory / good, rendered as **one hue at varying
  intensity**, never as red/amber/green. That is deliberate: the bands must survive colour-vision deficiency, and the
  alarm palette must not be spent on a background.

```csharp
// Models/Series/Categorical/BulletGraphSeries.cs
public sealed class BulletGraphSeries : ChartSeries
{
    public double Value { get; }                       // ctor — the identity of the series
    public double? Target { get; set; }
    public IReadOnlyList<double> Bands { get; set; }   // ascending band edges, in data units
    public Color? BarColor { get; set; }               // null → theme
    public Color? BandColor { get; set; }              // null → theme neutral; intensity is derived
    public BulletOrientation Orientation { get; set; } // Horizontal (default) | Vertical
}
```

Fluent surface (canon: identity in the ctor, the rest fluent):

```csharp
ax.Bullet(actual: 2412, b => b
    .WithTarget(2500)
    .WithBands(1800, 2200, 2800)      // poor | satisfactory | good
    .WithOrientation(BulletOrientation.Vertical));
```

Renderer: `BulletGraphSeriesRenderer`, copy the `BarSeriesRenderer` skeleton. Serialization: `Type = "bulletgraph"`,
value via `GaugeValue`, target + bands via existing DTO array fields — no new DTO members if `Starts`/`Ends` fit;
add the smallest additive field set if they do not, and regenerate the golden corpus **deliberately**
(`MPL_REGEN_GOLDEN=1`), never as a side effect.

---

## 2. `StatTileSeries` — the tile anatomy (property-level, not a new type)

A bare big number is a failed dashboard pattern: without a comparison the reader cannot tell whether the number is good
or bad, and supplies the missing context from memory. The tile gains the missing three quarters of the
**value + target + gap + trend** quadrilogy, and the hatch:

```csharp
public sealed class StatTileSeries : ChartSeries
{
    // existing: Value, AccentColor, Format
    public double? Target { get; set; }                 // the comparative
    public IReadOnlyList<double>? Trend { get; set; }   // inline Tufte sparkline: no axis, no frame, no ticks
    public Color? TrendColor { get; set; }
    public HatchPattern Hatch { get; set; }             // "no contact" — shape where colour must not be spent
    public Color? HatchColor { get; set; }
    public string? Caption { get; set; }                // the gap line: "target ≤ 25 ms · +3.1 over"
}
```

```csharp
ax.StatTile(24.8, t => t
    .WithFormat("0.0' ms'")
    .WithTarget(25)
    .WithTrend(p99History)
    .WithCaption("target ≤ 25 ms · within")
    .WithHatch(HatchPattern.ForwardDiagonal));   // only when the source has gone silent
```

The gap text is a **caption the caller supplies**, not a number the library computes — the library must not decide what
"good" means (see §6).

---

## 3. `StateSegment` — a hatch per segment (additive, non-breaking)

`StateSegment` is a positional `readonly record struct`. Adding a fifth positional parameter would be a binary break for
no gain; an `init` property is additive and reads better at the call site:

```csharp
public readonly record struct StateSegment(double Start, double End, string Label, Color Color)
{
    public HatchPattern Hatch { get; init; }
}
```

This is what lets a timeline row show a *gap in knowledge* (hatched) as visually distinct from a *fault* (coloured) —
the single most important representation in a federation, where "I can no longer see you" is the most common failure and
is not the same failure as "you are broken".

---

## 4. `OpsDashboard` — from static template to fluent composition

The 1.13.1 signature (`OpsDashboard(tiles, timelines, trendLines, title, configureTrend)`) is already a five-parameter
pile and the design needs three more concepts (window, band, the two-chart split). Growing it is the wrong move: a
configuration/composition API is exactly where the fluent builder canon applies.

```csharp
Plt.OpsDashboard()
   .WithTitle("Synapse — federation")
   .WithWindow(TimeSpan.FromMinutes(5))            // pinned rolling clock axis, shared by EVERY time panel
   .AddTile(t => t.Label("Buses").Value(15).Caption("all 15 normal"))
   .AddTile(t => t.Label("RFx p99").Value(24.8).Target(25).Trend(p99).Format("0.0' ms'"))
   .AddTimeline(l => l.Label("Service Bus").Segments(busSegments))
   .AddTrend(t => t.Label("publish").Series(x, publish).Envelope(lo, hi))   // FillBetween underneath
   .AddTrend(t => t.Label("consume").Series(x, consume))
   .WithNormalBand(2200, 2700)
   .Build();
```

`WithWindow` is the load-bearing one: **one owner for the time window**, shared by the trend panel and every timeline
row, so they slide in lock-step. It pins `SetXLim(now - W, now)` on every time axis — *exact* bounds with *round*
ticks. The 1.13.1 defect was the mirror image: the auto axis rounds the **bounds** (`ExpandedToNiceBoundsIfAuto`), so
the axis stands still and then jumps a whole step.

The old static entry point stays for one release, marked `[Obsolete]` with a migration note (CONTRIBUTING: breaking
cleanups are allowed, but a one-release shim costs nothing and the API is two weeks old).

---

## 5. Ops themes — the ground is a preference, the alarm palette is a contract

Four grounds an operator can choose (`Theme.OpsNight`, `OpsPanel`, `OpsWarm`, `OpsContrast`), built with the existing
`ThemeBuilder`. Every one of them obeys the same three rules, and the rules are what make the choice safe to offer:

1. **The resting state carries no colour** — not even green. Normal equipment is *not* coloured; colour is a scarce,
   reserved signal. A wall of green spends the contrast you need for the one thing that is wrong.
2. **The alarm hues never change meaning across themes** — Okabe-Ito amber (`#E69F00`) = attention, vermillion
   (`#D55E00`) = critical, and colour is never the sole carrier (always paired with text or shape). Only their
   *luminance* shifts per ground, so they stay equally loud on a dark wall and a bright panel.
3. **Motion is scarce**: a 1 Hz pulse means *unacknowledged*, and nothing else moves on its own.

---

## 6. Boundary — what this pack must NOT contain

Alarm conditioning (on-delay, deadband, off-delay), the worst-child roll-up, and the staleness clock are **domain logic
of the observability layer**, not of a charting library. MatPlotLibNet supplies the *form*; the alarm state machine
belongs with the bus. A charting library that holds opinions about when something is broken is a coupling nobody gets
out of later. The same goes for the "learned" normal band: the library draws the band it is handed (`FillBetween`); it
does not compute a p5–p95 over history.

---

## 7. Simulation — the sample proves it, and the sample is where the alarm logic lives

`Samples/MatPlotLibNet.Samples.Blazor` gets the full control-room demo, which doubles as the executable specification
for §6: everything the library must *not* do is visible here, in the sample, where it belongs.

* a federation simulator (15 buses × 10–20 processes) with a **latched** fault injector — a fault stays until it is
  cleared, because one that heals itself after 30 s cannot be inspected;
* **alarm conditioning** in the sample: on-delay (3 s warn / 2 s critical), deadband (trips at 25, clears at 22),
  off-delay (8 s) — the standard cures for a chattering wall;
* **window** (1 / 5 / 15 min / 1 h) with honest bucketing: rates averaged **with a min/max envelope** so a five-second
  burst survives a one-minute bucket, percentiles carried as their **maximum** (a p99 cannot be averaged — averaging is
  how a dashboard hides the very spike you came for), counters summed. The bucket size is printed on the chart;
* **refresh** (live / 1 / 2 / 5 / 10 s / pause) that throttles **only the charts**. The tiles never slow down: history
  may lag, a warning may not;
* readouts smoothed and rewritten at 1 Hz — dancing digits are unreadable, and the flicker itself teaches people to
  look away — while the **state** underneath is judged on the raw reading and turns the instant it must.

Reference mockup (HTML, behaviourally complete): the shape this pack must reproduce with the library.

---

## Phasing

| # | Step | Depends on |
|---|---|---|
| 0 | **Hatch rendering pipeline** — SVG `<pattern>` defs + Skia `SKShader` parity + DTO round-trip; `BarSeries` / `AreaSeries` hatch starts working (a `Fixed` entry, not a feature) | — |
| 1 | `StatTileSeries` extension (Target / Trend / Caption / Hatch) + fluent + renderer + tests | 0 |
| 2 | `StateSegment.Hatch` + renderer + tests | 0 |
| 3 | `BulletGraphSeries` + renderer + fluent + registry + central Theories + tests | — |
| 4 | `Plt.OpsDashboard()` fluent builder + `WithWindow` (pinned rolling clock axis) + `[Obsolete]` shim on the static | 1–3 |
| 5 | Ops themes (4 grounds) | — |
| 6 | Blazor simulation sample + cookbook + ARCHITECTURE + CHANGELOG | 1–5 |

Every new series must be registered in **both** central theories (`AllSeriesTests.AllSeriesInstances` +
`ChartSerializerRoundTripTests`) — omitting that leaves the conformance and round-trip paths uncovered and regresses the
baseline, which is exactly how the 1.12.0 tiles slipped through.
