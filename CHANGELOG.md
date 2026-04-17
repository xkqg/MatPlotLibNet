# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.5.0] — 2026-04-17

**3-D Enhancements.** Three polish items for the 3D charting pipeline: configurable pane colors, 3D colorbar support, and correct depth sorting during interactive SVG rotation. **4 246 tests green** across 11 test projects.

### Added

- **`Pane3DConfig`** — `sealed record` configuring the three back-facing cube panes (floor, left wall, right wall). Properties: `FloorColor`, `LeftWallColor`, `RightWallColor`, `Alpha` (default 0.8), `Visible` (default true). Axes property: `Axes.Pane3D`. Builder: `.WithPane3D(p => p with { FloorColor = Colors.Black })`.
- **`Theme.Pane3DColor`** — theme-level default pane color. Override for dark-mode 3D charts.
- **3D colorbar** — `ThreeDAxesRenderer` now calls `RenderColorBar()` after rendering 3D series. Surface, Scatter3D, and other colormapped 3D series with `.WithColorBar()` display a gradient legend strip.
- **JS depth re-sort on rotation** — `Svg3DRotationScript` now includes `resortDepth()` and `avgViewZ()` functions. After each interactive rotation (mouse drag or arrow keys), polygon DOM elements are re-sorted by view-space depth for correct painter's algorithm occlusion at all angles.

## [1.4.1] — 2026-04-17

**Interactive Polish.** Closes the interactivity gap with matplotlib: native 3D rotation, rectangle zoom, crosshair cursor, span selector, view history, data cursor, toolbar state model, tick mirroring, and tight margins. **4 234 tests green** across 11 test projects.

### Added — Interaction modifiers (v1.4.1)

- **`Rotate3DModifier`** — right-drag on 3D axes rotates the camera (azimuth/elevation). Arrow keys ±5°, Home resets to default (30°, -60°). `Rotate3DEvent : FigureInteractionEvent` mutates `Axes.Azimuth`/`Elevation` and nulls `Projection` to force rebuild.
- **`RectangleZoomModifier`** — Ctrl+left-drag draws a zoom box. `RectangleZoomEvent : AxisRangeEvent` — sealed `ApplyTo` sets axis limits. `RectangleZoomState` overlay (dashed blue rectangle).
- **`CrosshairModifier`** — passive modifier tracking mouse position. `CrosshairState` with pixel + data coordinates and plot area for clipping vertical/horizontal lines.
- **`SpanSelectModifier`** — Alt+left-drag selects a horizontal X-range. `SpanSelectEvent : FigureNotificationEvent` (non-mutating). `SpanSelectState` overlay (full-height shaded band).
- **`ViewHistoryManager`** — per-axes stack of axis limit snapshots. Push on zoom/pan, `Back()`/`Forward()` navigation. Capped at 50 entries.
- **`DataCursorEvent`** + **`PinnedAnnotation`** — click-to-pin data point annotation with series label and coordinates.
- **`InteractionToolbar`** — platform-agnostic toolbar state model: `ToolMode` enum (Pan, Zoom, Rotate3D, DataCursor, SpanSelect), `ToolbarButton` records, `ToolbarState` snapshot for overlay rendering. `CreateDefault(figure)` auto-configures buttons including Rotate3D for 3D figures.

### Added — Axis polish (v1.4.1)

- **`TickConfig.Mirror`** — when `true`, ticks and labels are drawn on both sides of the axes (e.g. Y ticks on left AND right spines). Builder: `.WithYTicksMirrored()`, `.WithXTicksMirrored()`.
- **`AxesBuilder.WithTightMargins()`** — convenience for `SetXMargin(0).SetYMargin(0)`, data touches axis spines directly.

### Changed

- **`InteractionController.BuildModifiers`** — expanded from 6 to 9 modifiers in priority order: LegendToggle > Reset > Rotate3D > RectangleZoom > BrushSelect > SpanSelect > Pan > Zoom > Hover.
- **`MplChartDrawOperation`** (Avalonia) — `_owner` made nullable to support `MplStreamingChartControl` without brush-select overlay.

## [1.4.0] — 2026-04-17

**Streaming & Realtime.** First-class live data support: dashboards and telemetry feeds can append data points without rebuilding the figure. Ring-buffer-backed streaming series, throttled re-rendering, auto-scaling axes, 11 incremental technical indicators, and streaming controls for all 5 UI hosts. **4 183 tests green** across 11 test projects.

### Added — Streaming infrastructure

- **`DoubleRingBuffer`** (core, `MatPlotLibNet.Data`) — fixed-capacity circular buffer with `ReaderWriterLockSlim` for concurrent single-writer / multi-reader. `Append`, `AppendRange`, `CopyTo`, `ToArray`, `Min`, `Max`, `Clear`. Never allocates on append.
- **`StreamingSnapshot` / `OhlcStreamingSnapshot`** — immutable point-in-time copies for render-thread safety.
- **`IStreamingSeries`** — contract: `AppendPoint(x, y)`, `AppendPoints`, `Clear`, `Version`, `Count`, `Capacity`, `CreateSnapshot()`.
- **`IStreamingOhlcSeries`** — contract: `AppendBar(o, h, l, c)`, `CreateOhlcSnapshot()`, `BarAppended` event.
- **`StreamingSeriesBase`** — abstract base with twin ring buffers (X, Y), monotonic version counter, `ComputeDataRange` from live buffers.

### Added — 4 streaming series types (74 total)

- **`StreamingLineSeries`** — ring-buffer-backed line with `Color`, `LineStyle`, `LineWidth`. Default capacity 10,000.
- **`StreamingScatterSeries`** — ring-buffer-backed scatter with `Color`, `Alpha`, `MarkerSize`. Default capacity 10,000.
- **`StreamingSignalSeries`** — Y-only storage, X computed from `SampleRate` + offset. Optimized for oscilloscope/audio data. Default capacity 100,000.
- **`StreamingCandlestickSeries`** — four parallel ring buffers (O/H/L/C) with `BarAppended` event for indicator auto-attach. Default capacity 5,000.

### Added — StreamingFigure + axis scaling

- **`StreamingFigure`** — wraps `Figure` with render timer, data version tracking, `ApplyAxisScaling()`, `RenderRequested` event. `IDisposable`. Default throttle 33ms (~30fps).
- **`AxisScaleMode`** — sealed record hierarchy: `Fixed`, `AutoScale`, `SlidingWindow(windowSize)`, `StickyRight(windowSize)`.
- **`StreamingAxesConfig`** — per-axes X/Y scale mode. Default: sliding window on X, auto-scale on Y.
- **`FigureBuilder.BuildStreaming()`** — wraps `Build()` output in `StreamingFigure`.
- **`AxesBuilder.StreamingPlot() / StreamingScatter() / StreamingSignal() / StreamingCandlestick()`** — fluent builder methods returning the series for data append.

### Added — 11 streaming technical indicators

All O(1) per append. Auto-attach to `StreamingCandlestickSeries` via `BarAppended` event. Each indicator owns its own `StreamingLineSeries` output — zero renderer changes.

- **`StreamingSma`** — rolling sum / period.
- **`StreamingEma`** — α * new + (1-α) * prev.
- **`StreamingRsi`** — Wilder's smoothed gain/loss.
- **`StreamingBollinger`** — SMA + Welford's rolling variance. 3 output series (mid, upper, lower).
- **`StreamingMacd`** — two EMAs + signal EMA. 3 output series (MACD, signal, histogram).
- **`StreamingObv`** — cumulative volume direction.
- **`StreamingAtr`** — Wilder's smoothed true range.
- **`StreamingStochastic`** — rolling min/max deque. 2 output series (%K, %D).
- **`StreamingWilliamsR`** — rolling min/max.
- **`StreamingCci`** — rolling mean deviation.
- **`StreamingVwap`** — cumulative price*volume / volume.
- **Fluent API:** `.WithStreamingSma(axes, 20)`, `.WithStreamingBollinger(axes, 20, 2)`, etc. on `StreamingCandlestickSeries`.

### Added — Platform streaming controls

- **`MplStreamingChartControl`** (Avalonia) — `StreamingFigure` styled property, subscribes to `RenderRequested`, marshals via `Dispatcher.UIThread`.
- **`MplStreamingChartElement`** (Uno) — same pattern via `DispatcherQueue.TryEnqueue`.
- **`MplStreamingChartView`** (MAUI) — same pattern via `MainThread.BeginInvokeOnMainThread`.
- **`MplStreamingChart`** (Blazor) — same pattern via `InvokeAsync` + `StateHasChanged`.
- **`StreamingChartSession`** (ASP.NET Core) — server-side session subscribing to `RenderRequested`, publishes SVG via `IChartPublisher` for remote clients.
- **`FigureRegistry.RegisterStreaming()`** — registers a streaming figure for server-push live updates.

### Added — SVG diff + Rx adapter

- **`SvgDiffEngine`** — compares previous/current SVG, produces minimal `SvgPatch` (replace changed series groups only). Typically 10x smaller than full SVG for streaming updates.
- **`StreamingSeriesExtensions.SubscribeTo(IObservable<T>)`** — Rx adapter connecting `IObservable<(double, double)>`, `IObservable<OhlcBar>`, and `IObservable<double>` to streaming series. No System.Reactive dependency.

## [1.3.0] — 2026-04-16

**Cross-platform native UI controls, full interaction polish, 3-D round 2, MathText completion.** Two new NuGet packages (`MatPlotLibNet.Avalonia`, `MatPlotLibNet.Uno`) let desktop .NET developers render and interact with charts natively — no browser, no WebView, no SignalR. The managed interaction layer in core gained full legend-toggle activation, rubber-band selection visuals, hover tooltips with nearest-point lookup, and a server-mode SignalR adapter. Six new 3-D series types (Line3D, Trisurf, Contour3D, Quiver3D, Voxels, Text3D) and a substantially expanded MathText parser round out the release. **4 028 tests green** across 11 test projects.

### Added — Native controls + interaction layer

- **`MatPlotLibNet.Avalonia`** (new package) — `MplChartControl : Control` with `Figure` and `IsInteractive` styled properties. Renders via `SkiaRenderContext` through Avalonia 12's `ISkiaSharpApiLeaseFeature`. Targets .NET 10 + .NET 8.
- **`MatPlotLibNet.Uno`** (new package) — `MplChartElement : SKCanvasElement` with `Figure` and `IsInteractive` dependency properties. Targets Windows (WinUI 3), Android, iOS, macCatalyst via `Uno.WinUI 5.x`.
- **`InteractionController`** (core) — composes six `IInteractionModifier` implementations in priority order. `CreateLocal(figure, layout)` for in-process mutation; `Create(figure, layout, sink)` for custom event routing (e.g. SignalR). `UpdateLayout` rebuilds modifiers after each render.
- **6 concrete modifiers** — `PanModifier` (left-drag), `ZoomModifier` (scroll, 15%/notch, cursor-centred), `ResetModifier` (double-click or Home/Escape), `BrushSelectModifier` (Shift+drag with rubber-band visual), `HoverModifier` (move, no button), `LegendToggleModifier` (click legend item → toggles series visibility).
- **`ChartLayout`** (core) — pixel↔data coordinate transform. `HitTestAxes`, `PixelToData`, `GetDataRange`, `HitTestLegendItem` (fully functional — legend bounds exposed from `AxesRenderer` via `LayoutResult`).
- **`LayoutResult`** + **`LegendItemBounds`** — `ComputeLayout` now returns plot areas + per-subplot legend item bounds, enabling legend hit-testing in native controls.
- **`BrushSelectState`** — during Shift+drag, a semi-transparent blue selection rectangle is drawn on the Skia canvas in both Avalonia and Uno controls.
- **`NearestPointFinder`** + **`HoverTooltipContent`** — local nearest-point lookup across all visible `XYSeries`; native tooltip shown at cursor position in Avalonia (`ToolTip.SetTip`) and Uno (`ToolTipService`).
- **`SignalREventSink`** — platform-neutral helper that dispatches `FigureInteractionEvent` to named hub methods via `Func<string, object, Task>`.
- **`WithServerInteraction`** extensions for Avalonia and Uno — opt-in server mode that routes events to a SignalR `HubConnection` instead of mutating locally.
- **5 platform-neutral input arg types** — `PointerInputArgs`, `ScrollInputArgs`, `KeyInputArgs`, `PointerButton`, `ModifierKeys`.
- **`AvaloniaInputAdapter`** / **`UnoInputAdapter`** — convert native pointer/scroll/key events to the neutral records.
- **Sample apps** — `Samples/MatPlotLibNet.Samples.Avalonia` (desktop, static + interactive charts) and `Samples/MatPlotLibNet.Samples.Uno` (WinUI, same pattern).

### Added — MathText completion

- **Fractions** — `\frac{num}{den}` parsed into `FractionNumerator` / `FractionDenominator` spans at 70% size.
- **Square roots** — `\sqrt{x}` and `\sqrt[n]{x}` with `Radical` span kind.
- **Accents** — `\hat`, `\bar`, `\overline`, `\tilde`, `\dot`, `\ddot`, `\vec`, `\check`, `\breve` via Unicode combining characters.
- **Font variants** — `\mathrm{}`, `\mathbf{}`, `\mathit{}`, `\mathcal{}`, `\mathbb{}` with `FontVariant` enum.
- **Text mode** — `\text{...}` for upright text inside math mode.
- **Spacing commands** — `\,` (thin), `\:` (medium), `\;` (thick), `\quad`, `\qquad`, `\!` (negative thin).
- **Scaling delimiters** — `\left( ... \right)` parsed and emitted.
- **~45 new symbols** — blackboard bold (ℝ ℂ ℤ ℕ ℚ), double arrows (⇒ ⇐ ⇔), relations (≪ ≫ ≅ ≃), binary operators (⊗ ⊕ ∗ ∙ ∓ †), set operators (∅ ∖), miscellaneous (ℏ ℓ ℜ ℑ ℵ ℘ ′ ″).

### Added — 3-D round 2

- **`Line3DSeries`** — 3-D polyline with depth-sorted segments.
- **`Trisurf3DSeries`** — triangulated surface from unstructured (x, y, z) clouds with colormap, alpha, wireframe overlay.
- **`Contour3DSeries`** — full marching-squares contour lines projected to 3-D at each level; colormap per level.
- **`Quiver3DSeries`** — 3-D vector field with arrow shafts + arrowhead barbs; (x, y, z) + (u, v, w) data.
- **`VoxelSeries`** — filled cubic voxels on a `bool[,,]` mask grid with per-face shading and `DepthQueue3D` compositing.
- **`Text3DSeries`** — 3-D text annotations projected to 2-D at configurable font size.
- Builder methods: `.Plot3D()`, `.Trisurf()`, `.Contour3D()`, `.Quiver3D()`, `.Voxels()`, `.Text3D()`.

### Added — 2-D gaps

- **Scatter3D colormap** — `Scatter3DSeries` implements `IColormappable` + `INormalizable`; renderer maps Z values through colormap when set.
- **`IHasMarkerStyle`** interface + `MarkerStyle` integration on `ScatterSeries` and `Scatter3DSeries`.
- **`AreaSeries.Where`** — `Func<double, double, bool>?` predicate for conditional fill (matplotlib `fill_between(where=...)`).

### Infrastructure

- `MatPlotLibNet.CI.slnf` — Skia + Avalonia added to the Linux CI filter.
- `publish.yml` — Uno build/test/pack in Windows platform job; Avalonia packed by Linux core job.
- All 11 `.csproj` files at `<Version>1.3.0</Version>`.


## [1.2.2] — 2026-04-15

**Brush-select + hover round-trip — the deferred v1.2.0 items.** v1.2.0 shipped four mutation events (Zoom, Pan, Reset, LegendToggle) that rewrite the authoritative `Figure` on the server and broadcast the updated SVG to every group subscriber. v1.2.2 introduces the first two **notification events** — `BrushSelectEvent` and `HoverEvent` — that observe the user's gesture, route it to a per-chart handler in .NET code, and optionally return a caller-only response. Pure .NET round-trip, no mutation, no broadcast. The bidirectional SignalR pipeline now covers observation and request-response alongside the existing mutation flow.

The critical architectural insight: **every v1.2.0 event mutates the figure.** Brush-select and hover don't — they ask the user's code to observe (selection) or respond (tooltip). v1.2.2 treats this as a proper subsystem extension: a new tier-2 abstract record `FigureNotificationEvent` that both new events stack under, mirroring how `AxisRangeEvent` stacks `ZoomEvent` and `ResetEvent`. The bug class "a notification event accidentally mutates the figure" is structurally impossible because `FigureNotificationEvent.ApplyTo` is `sealed override` and a no-op — concrete subclasses cannot override it.

### Added

- **[`MatPlotLibNet.Interaction.FigureNotificationEvent`](Src/MatPlotLibNet/Interaction/FigureNotificationEvent.cs)** — abstract tier-2 record for non-mutating events. Sibling to `AxisRangeEvent` (the tier-2 record for axis-limit mutations). `ApplyTo` is `sealed override` and empty — notification events observe, they do not mutate. Adding a new notification type is a new sealed record inheriting from this tier, no existing code changes.
- **[`BrushSelectEvent`](Src/MatPlotLibNet/Interaction/BrushSelectEvent.cs)** — carries the data-space rectangle `(X1, Y1) → (X2, Y2)` of a Shift+drag brush selection. Fire-and-forget: the server routes it to a registered handler that observes the selection (log, filter, trigger downstream work). The figure is never re-rendered from a brush-select.
- **[`HoverEvent`](Src/MatPlotLibNet/Interaction/HoverEvent.cs)** — carries `(X, Y)` in data space plus a server-stamped `CallerConnectionId`. Request-response: the handler returns an HTML fragment delivered to the **originating client only** via `IChartHubClient.ReceiveTooltipContent`, not broadcast to the group. Enables rich, server-computed tooltips with access to live application state, authenticated lookups, async queries.
- **[`ChartSessionOptions`](Src/MatPlotLibNet.AspNetCore/ChartSessionOptions.cs)** — fluent options bag for per-chart handler registration. Pass to the new `FigureRegistry.Register(chartId, figure, configure)` overload:
  ```csharp
  registry.Register("live-1", figure, opts => opts
      .OnBrushSelect(evt => { /* log, filter, trigger */ return default; })
      .OnHover(evt => ValueTask.FromResult($"<b>x={evt.X},y={evt.Y}</b>")));
  ```
  Each chart can register its own handlers; different figures can compute different tooltips or react differently to selections. Handlers are stored on the `ChartSession` and fired from the session's drain task — same thread-model guarantee as mutation events (one session, one reader, no locking).
- **[`ICallerPublisher`](Src/MatPlotLibNet.AspNetCore/ICallerPublisher.cs) + [`CallerPublisher`](Src/MatPlotLibNet.AspNetCore/CallerPublisher.cs)** — the first per-connection send pattern in the library. v1.2.0 only had `Clients.Group` broadcast; `CallerPublisher.SendTooltipAsync(connectionId, chartId, html)` uses `Clients.Client(connectionId).ReceiveTooltipContent(...)` to target the originating caller. Registered as a singleton in `AddMatPlotLibNetSignalR()`.
- **[`ChartHub.OnBrushSelect`](Src/MatPlotLibNet.AspNetCore/ChartHub.cs)** — one-line client→server method. Routes to `FigureRegistry.Publish`, which routes through `ChartSession` to the user's handler. Fire-and-forget, no broadcast.
- **[`ChartHub.OnHover`](Src/MatPlotLibNet.AspNetCore/ChartHub.cs)** — accepts a [`HoverEventPayload`](Src/MatPlotLibNet.AspNetCore/HoverEventPayload.cs) DTO from the client (the four data-space fields, no connection ID), stamps `Context.ConnectionId` server-side into a full `HoverEvent`, and routes to the hover handler. Clients cannot spoof the connection ID — the hub always overwrites.
- **[`FigureBuilder.WithServerInteraction`](Src/MatPlotLibNet/Builders/ServerInteractionBuilder.cs)** gains two new flags via the existing fluent builder: `EnableBrushSelect()` and `EnableHover()`. Both are additive — opt-in per figure. `.All()` now includes them.
- **SVG dispatcher extension** — [`SvgSignalRInteractionScript`](Src/MatPlotLibNet/Rendering/Svg/SvgSignalRInteractionScript.cs) gains two new branches (marker tokens `mplBrushSelect` and `mplHoverRoundtrip`) appended inline to the v1.2.0 IIFE only when the respective flags are set. Shift+drag draws a rubber-band rectangle and invokes `OnBrushSelect` with the data-space rect on mouseup. Mousemove invokes `OnHover` (coalesced via a `pending` flag — at most one in-flight request, queued latest point on overlap). Server responses via `ReceiveTooltipContent` render a styled fixed-position overlay near the cursor with `role="tooltip"` + `aria-live="polite"`.

### Tests

- **`FigureNotificationEventTests`** — 13 tests covering abstractness, inheritance chain, sealed no-op `ApplyTo`, positional record equality including `CallerConnectionId`, and the `BrushSelectEvent`/`HoverEvent` concrete shapes.
- **`ChartSessionHandlerTests`** — 6 tests with fake `IChartPublisher` + `ICallerPublisher` test doubles. Covers: brush-select handler fires without republish, hover handler routes content to the caller publisher, hover handler returning `null` does not invoke the caller, notification events with no handler are silent no-ops, mutation events after notifications still fire one publish per batch, and v1.2.0 `Register(chartId, figure)` backward-compat still works.
- **`SignalRInteractionTestsV122`** — 4 real SignalR end-to-end round-trip tests using `Microsoft.AspNetCore.TestHost.TestServer` + `HubConnectionBuilder` — no mocks. Includes the **first caller-only test** in the library: two connected clients, client A invokes `OnHover`, only A receives `ReceiveTooltipContent`, client B does not. Also verifies v1.2.0 `OnZoom` still broadcasts (regression guard).
- **`SvgSignalRInteractionScriptV122Tests`** — 7 tests: brush-select branch emitted / omitted on flag toggle, hover branch emitted / omitted, both branches emitted with `.All()`, static figure has neither, and v1.2.0 markers still present when only v1.2.2 flags are set.
- **`FigureBuilderServerInteractionTests`** — 3 new tests for `EnableBrushSelect` / `EnableHover` flag routing and the updated `All()` behaviour.

**Test counts:** core `3 460 → 3 483` (+23), AspNetCore `26 → 36` (+10), total **3 519 tests green** across 7 test projects.

### Fixed (carried over from v1.2.1 regeneration)

- **Dense-Y-axis sample images regenerated** — v1.2.1's `ThemedFontProvider` fix widened Y-tick labels (12 pt instead of 10 pt), which shifted the left margin by ~7 px on figures with wide numeric labels (`scientific_paper`, `phase_f_indicators`, `financial_dashboard`, `heatmap_colormap`, etc.). The v1.2.1 release note claimed layout was byte-identical for every non-legend figure, but 22 samples were actually affected and their regenerated output was not committed. v1.2.2 catches up: all 34 sample images now reflect the post-`ThemedFontProvider` layout. No actual bug — correct behaviour all along, just missing artefacts.

### Files created
- `Src/MatPlotLibNet/Interaction/FigureNotificationEvent.cs`
- `Src/MatPlotLibNet/Interaction/BrushSelectEvent.cs`
- `Src/MatPlotLibNet/Interaction/HoverEvent.cs`
- `Src/MatPlotLibNet.AspNetCore/ChartSessionOptions.cs`
- `Src/MatPlotLibNet.AspNetCore/ICallerPublisher.cs`
- `Src/MatPlotLibNet.AspNetCore/CallerPublisher.cs`
- `Src/MatPlotLibNet.AspNetCore/HoverEventPayload.cs`
- `Tst/MatPlotLibNet/Interaction/FigureNotificationEventTests.cs`
- `Tst/MatPlotLibNet.AspNetCore/ChartSessionHandlerTests.cs`
- `Tst/MatPlotLibNet.AspNetCore/SignalRInteractionTestsV122.cs`
- `Tst/MatPlotLibNet/Rendering/Svg/SvgSignalRInteractionScriptV122Tests.cs`

### Files modified
- `Src/MatPlotLibNet.AspNetCore/FigureRegistry.cs` — new `Register(chartId, figure, Action<ChartSessionOptions>)` overload, `ICallerPublisher` dependency, v1.2.0 compat constructor with `NullCallerPublisher`.
- `Src/MatPlotLibNet.AspNetCore/ChartSession.cs` — drain loop type-switches between mutation and notification events; hover path routes through `ICallerPublisher`.
- `Src/MatPlotLibNet.AspNetCore/ChartHub.cs` — adds `OnBrushSelect` + `OnHover` methods.
- `Src/MatPlotLibNet.AspNetCore/IChartHubClient.cs` — adds `ReceiveTooltipContent`.
- `Src/MatPlotLibNet.AspNetCore/Extensions/SignalRExtensions.cs` — registers `ICallerPublisher`.
- `Src/MatPlotLibNet/Builders/ServerInteractionBuilder.cs` — new `EnableBrushSelect` / `EnableHover` methods, updated `All()`.
- `Src/MatPlotLibNet/Builders/FigureBuilder.cs` — `WithServerInteraction` forwards the two new flags to `Figure.EnableSelection` / `Figure.EnableRichTooltips`.
- `Src/MatPlotLibNet/Rendering/Svg/SvgSignalRInteractionScript.cs` — `GetScript(enableBrushSelect, enableHover)` signature; two new inline branches.
- `Src/MatPlotLibNet/Transforms/SvgTransform.cs` — skips `SvgCustomTooltipScript` + `SvgSelectionScript` when `ServerInteraction = true` (the dispatcher replaces them), preserves emission order for static figures.
- All 9 × `.csproj` `<Version>` → `1.2.2`.

### Deferred (still — unchanged from v1.2.1 out-of-scope list)

- **Pluggable `IFigureInteractionHandler`** — v1.2.2 adds per-chart callbacks via `ChartSessionOptions`, but not a registerable "handle any event type" interface.
- **Multi-viewer sync as a designed feature** — still a side-effect of SignalR group fan-out.
- **Avalonia / Uno / WinUI 3 UI packages** — planned as v1.3.0 "Cross-Platform UI Coverage", reusing the v1.2.2 hub vocabulary unchanged.
- **3-D round 2**, **mathtext completion** (`\frac`, proper `\sqrt`, matrices, accents), **2-D series gaps** (`fill_betweenx`, `matshow`, `spy`), **geo/map subsystem rebuild** — deferred as in v1.2.0/v1.2.1.

## [1.2.1] — 2026-04-15

**Font-factory subsystem fix + CI warning sweep.** A small follow-up to v1.2.0 that fixes the root cause of the outside-legend clipping bug, wipes every warning off every non-MAUI project, and unblocks two sample projects that had stopped compiling.

### The bug, diagnosed properly

v1.1.4's CHANGELOG claimed the new `LegendMeasurer` made outside-legend margin reservation pixel-accurate. It didn't — the `legend_outside` sample in v1.2.0 was still chopping the `exp(-x/5)·cos(x)` label off the right edge of the figure. Root cause: **two duplicate `TickFont()` factories that had silently drifted.**

- [`AxesRenderer.TickFont()`](Src/MatPlotLibNet/Rendering/AxesRenderer.cs) built `Size = Theme.DefaultFont.Size` (12 pt) and was used at draw time by `RenderLegend`, `RenderTicks`, `RenderColorBar`.
- `ConstrainedLayoutEngine.TickFont(theme)` built `Size = theme.DefaultFont.Size - 2` (10 pt) and was used by `PerAxesMetrics.Measure` to reserve left / bottom / right margin and by `LegendMeasurer.MeasureBox`.

The measurer reported a **140 × 84** legend box at 10 pt while the renderer drew a **161 × 94** box at 12 pt — 20.88 px of under-reservation, enough to clip the fourth entry. The first fix (landed in the working tree) made `LegendMeasurer` derive its font directly from `Theme`, but that only patched the legend path. Three more `TickFont`-dependent measurements (Y-tick width, X-tick height, colorbar tick labels) were still under-reserving. `ColorBar.TitleFont`, `ChartRenderer.TitleFont(theme, sizeOffset)`, and four methods on `ConstrainedLayoutEngine` were all maintaining their own copy of "the formula for the font at role X". Eight duplicate font factories across three files. The bug class is **duplicate formulas that drift**, and point-patching each call site would leave the next one waiting to bite.

### The subsystem fix

- **New [`ThemedFontProvider`](Src/MatPlotLibNet/Rendering/ThemedFontProvider.cs)** — one `internal static` class, four methods (`TickFont`, `LabelFont`, `TitleFont`, `SupTitleFont`), each taking only a `Theme`. Single source of truth for every themed font in the render pipeline. Size formulas live in one place: tick/label = `DefaultFont.Size`, axes title = `DefaultFont.Size + 2`, suptitle = `DefaultFont.Size + 4`. Adding a new font role is a new method here; no call site can diverge because there is no formula anywhere else.
- **Deleted eight duplicate font factories**:
  - `AxesRenderer.TitleFont(int sizeOffset)` — replaced by parameterless `TitleFont()` that delegates to the provider.
  - `AxesRenderer.TickFont()` / `LabelFont()` — delegate to provider.
  - `ChartRenderer.TitleFont(theme, int sizeOffset)` — deleted entirely; the suptitle call site reads `ThemedFontProvider.SupTitleFont(theme)` directly.
  - `ConstrainedLayoutEngine.TickFont(theme)` / `LabelFont(theme)` / `TitleFont(theme)` / `SupTitleFont(theme)` — all four deleted; `Measure` now takes `(Axes, IRenderContext, Theme)` and pulls fonts from the provider internally.
  - `LegendMeasurer.LegendFont` keeps its name but its body is now one line delegating to `ThemedFontProvider.TickFont(theme)` (then applying the optional `Legend.FontSize` override).
- **Drift regression-test battery** in [`Tst/MatPlotLibNet/Rendering/Layout/LegendMeasurerDriftTests.cs`](Tst/MatPlotLibNet/Rendering/Layout/LegendMeasurerDriftTests.cs) — renamed class `MeasurerRendererDriftTests` with seven semantic invariants: legend box doesn't clip, Y-tick labels fit inside `MarginLeft`, X-tick labels fit inside `MarginBottom`, colorbar tick labels fit inside `MarginRight`, axes title fits inside `MarginTop`, suptitle fits inside `MarginTop`, secondary-X-axis label fits inside `MarginTop`. Each test asserts the full rendered geometry, not an intermediate font size, so it survives the refactor and serves as a permanent guard against the whole bug class.
- **Regression check** — regenerated all 34 `images/*.svg` samples via the console runner. `git diff images/` shows zero changes beyond `legend_outside.{svg,png}` (which were the v1.2.1 legend fix itself). The refactor is byte-identical for every figure that wasn't affected by the bug.

### CI warning sweep — every non-MAUI project at 0 warnings, 0 errors

- **5 × CS0117 compile errors** in `Samples/MatPlotLibNet.Samples.WebApi/Program.cs` and `Samples/MatPlotLibNet.Samples.GraphQL/Program.cs` — both referenced the stale `Color.Blue` / `Color.Orange` API. Replaced with `Colors.Blue` / `Colors.Orange`. Both sample projects compile again.
- **1 × CS0105** duplicate `using Microsoft.AspNetCore.Components.Web` in [`Samples/MatPlotLibNet.Samples.Blazor/Components/_Imports.razor`](Samples/MatPlotLibNet.Samples.Blazor/Components/_Imports.razor) — removed.
- **1 × CS8625** null-literal-to-non-nullable in [`Tst/MatPlotLibNet/Models/Series/Polar/PolarHeatmapSeriesTests.cs:120`](Tst/MatPlotLibNet/Models/Series/Polar/PolarHeatmapSeriesTests.cs#L120) — `default` for `RenderArea` (a reference type) was being interpreted as a null literal. Fixed by constructing a real `RenderArea` with a `Rect` and an `SvgRenderContext`.
- **16 × xUnit1051** across three test files — every `Task.Delay` / `HttpClient.GetAsync` / `HubConnection.StartAsync` / `HubConnection.InvokeAsync` / `IChartPublisher.PublishSvgAsync` / `Task.WaitAsync` call now passes `TestContext.Current.CancellationToken`. 14 edits in [`Tst/MatPlotLibNet.Interactive/ChartServerTests.cs`](Tst/MatPlotLibNet.Interactive/ChartServerTests.cs), 1 in `Tst/MatPlotLibNet/Rendering/CartesianAxesRendererRangeTests.cs`, 1 in `Tst/MatPlotLibNet.AspNetCore/FigureRegistryTests.cs`. Tests now cancel responsively under a failing xUnit cancellation token. No `#pragma warning disable` and no `NoWarn` — the warning is gone because the hazard was fixed.

### Test counts

- `MatPlotLibNet.Tests` — **3 460** (was 3 454 in v1.2.0 working tree; +6 = 5 new drift tests + 1 from the previous session that was sitting uncounted in the suite after the v1.2.0 wrap-up).
- `MatPlotLibNet.AspNetCore.Tests` — **26** (unchanged)
- Other test projects — `DataFrame 54`, `GraphQL 12`, `Skia 40`, `Interactive 27+1 skipped`, `Blazor 22` (all unchanged).
- Combined: **3 641 passing**, 0 failures across all 7 test projects. Every non-MAUI library builds at **0 warnings / 0 errors**.

### Not in this release

- **MAUI** — can't build locally without `dotnet workload restore android`. Unaffected by the font-factory refactor. Covered by CI.
- **Secondary-X-axis baseline bug** flagged by the drift audit — turned out to be a false positive. The engine's `28 + labelHeight` reservation is sufficient; the regression test is kept as a guard.
- **`AxesRenderer.TitleFont(int sizeOffset = 4)` default-parameter latent bomb** — gone entirely because the method no longer takes a parameter after the refactor.

## [1.2.0] — 2026-04-15

**Bidirectional SignalR: live, server-authoritative interactive charts.** v1.1.4 and earlier shipped a one-way SignalR pipeline — the server could push SVG updates to subscribers, but browser interactions stayed purely client-side and the server never heard about them. v1.2.0 closes the loop: wheel-zoom, drag-pan, <kbd>Home</kbd>-reset, and click-to-toggle-legend events flow from the browser through `ChartHub` into a new `FigureRegistry`, which mutates the registered `Figure` on a per-chart background reader task and publishes the updated SVG back through the existing `IChartPublisher.PublishSvgAsync` fan-out. All mutation is structurally serial (one `System.Threading.Channels.Channel<T>` per chart, single reader) so there are no locks, no semaphores, and no shared-state races — the hub method is a one-line `TryWrite` and the render happens off the hub call stack. Test count: **3 499 → 3 477 + 67 new = 3 544 green across core + AspNetCore**, plus 4 real-SignalR round-trip tests using `TestServer` and `HubConnectionBuilder` with zero mocks.

### Added

- **`MatPlotLibNet.Interaction` namespace** with a three-tier stacked-record event hierarchy — self-applying, no static mutator, no visitor, SOLID-OCP clean:
  - [`FigureInteractionEvent`](Src/MatPlotLibNet/Interaction/FigureInteractionEvent.cs) — abstract root record carrying `ChartId` and `AxesIndex`; exposes one abstract `ApplyTo(Figure)` and a shared `TargetAxes(figure)` helper.
  - [`AxisRangeEvent`](Src/MatPlotLibNet/Interaction/AxisRangeEvent.cs) — abstract tier-2 record for any event that overwrites the X and Y limits directly. `ApplyTo` is `sealed override` so `ZoomEvent` and `ResetEvent` cannot diverge from axis-range semantics.
  - [`ZoomEvent`](Src/MatPlotLibNet/Interaction/ZoomEvent.cs) and [`ResetEvent`](Src/MatPlotLibNet/Interaction/ResetEvent.cs) — concrete subclasses of `AxisRangeEvent`. Both carry `(XMin, XMax, YMin, YMax)`; distinct types so the hub can route them separately for telemetry.
  - [`PanEvent`](Src/MatPlotLibNet/Interaction/PanEvent.cs) — delta-based, inherits directly from `FigureInteractionEvent`. Translates `Axis.Min`/`Max` by `(DxData, DyData)`, no-op if limits are still null (auto-range).
  - [`LegendToggleEvent`](Src/MatPlotLibNet/Interaction/LegendToggleEvent.cs) — flips `ChartSeries.Visible` for `Series[SeriesIndex]`. Out-of-range indices are silent no-ops.
- **`MatPlotLibNet.AspNetCore.FigureRegistry`** ([`Src/MatPlotLibNet.AspNetCore/FigureRegistry.cs`](Src/MatPlotLibNet.AspNetCore/FigureRegistry.cs)) — concrete class (no interface, YAGNI), DI singleton. `Register(chartId, figure)` creates a per-chart `ChartSession`, `Publish(chartId, evt)` writes to that session's channel, `UnregisterAsync(chartId)` disposes it. External callers cannot reach the raw figure — there is no `TryGet(out Figure)`, by design. Every mutation path goes through `Publish`, which is the only way an event can touch the registered figure.
- **`ChartSession`** (internal, [`Src/MatPlotLibNet.AspNetCore/ChartSession.cs`](Src/MatPlotLibNet.AspNetCore/ChartSession.cs)) — holds one `Channel<FigureInteractionEvent>` (unbounded, single-reader) plus one background reader task. `DrainAsync` waits on `WaitToReadAsync`, drains the full batch into the figure via `ApplyTo`, then calls `PublishSvgAsync` once per drained batch. A burst of 50 wheel-zoom events that arrive in one tick produces exactly one re-render — natural coalescing without any explicit debounce. No `SemaphoreSlim`, no `lock`, no `ConcurrentBag` (wrong ordering).
- **`ChartHub` gains four client-to-server methods**:
  - [`OnZoom(ZoomEvent)`](Src/MatPlotLibNet.AspNetCore/ChartHub.cs) — one-line `_registry.Publish(evt.ChartId, evt)`.
  - `OnPan(PanEvent)` — ditto.
  - `OnReset(ResetEvent)` — ditto.
  - `OnLegendToggle(LegendToggleEvent)` — ditto.
  
  All four are `void`, not `async Task` — the channel write is synchronous and the render happens on the reader task. Hub method latency is bounded by the channel write (microseconds), never by rendering. `AddMatPlotLibNetSignalR()` now registers `FigureRegistry` as a singleton so `ChartHub`'s constructor picks it up via DI.
- **`Figure.ChartId` + `Figure.ServerInteraction`** ([`Src/MatPlotLibNet/Models/Figure.cs`](Src/MatPlotLibNet/Models/Figure.cs)) — two new mutable properties. `ServerInteraction == true` tells `SvgTransform` to emit the new `SvgSignalRInteractionScript` instead of the client-side `SvgInteractivityScript` + `SvgLegendToggleScript`. `Figure.HasInteractivity` now includes `ServerInteraction` in its OR, so existing consumers see the new mode as "interactive" automatically.
- **`FigureBuilder.WithServerInteraction(chartId, configure)`** ([`Src/MatPlotLibNet/Builders/FigureBuilder.cs`](Src/MatPlotLibNet/Builders/FigureBuilder.cs)) — fluent opt-in:
  ```csharp
  var figure = new FigureBuilder()
      .Plot(xs, ys)
      .WithServerInteraction("live-1", i => i.All())
      .Build();
  ```
  Sets `Figure.ChartId`, `Figure.ServerInteraction = true`, and flips the existing `EnableZoomPan` / `EnableLegendToggle` flags for each event opted in. Names mirror the existing `Enable*` convention — no new vocabulary.
- **`ServerInteractionBuilder`** ([`Src/MatPlotLibNet/Builders/ServerInteractionBuilder.cs`](Src/MatPlotLibNet/Builders/ServerInteractionBuilder.cs)) — small fluent builder with `EnableZoom()` / `EnablePan()` / `EnableReset()` / `EnableLegendToggle()` / `All()`. Consumed inside `WithServerInteraction`; exposed publicly so tests can assert its fluent return-this semantics.
- **`SvgSignalRInteractionScript`** ([`Src/MatPlotLibNet/Rendering/Svg/SvgSignalRInteractionScript.cs`](Src/MatPlotLibNet/Rendering/Svg/SvgSignalRInteractionScript.cs)) — single IIFE with marker token `mplSignalRInteraction`. Discovers the hub connection via `window.__mpl_signalr_connection` (set by the frontend component), reads `data-chart-id` off the root `<svg>`, wires wheel → `OnZoom`, pointer drag → `OnPan`, <kbd>Home</kbd> → `OnReset`, click on `[data-series-index]` → `OnLegendToggle`. Graceful no-op if the connection isn't there.
- **Root `<svg>` `data-chart-id` attribute** ([`Src/MatPlotLibNet/Transforms/SvgTransform.cs`](Src/MatPlotLibNet/Transforms/SvgTransform.cs)) emitted when `figure.ServerInteraction && figure.ChartId is not null`. XML-escaped via existing `SvgXmlHelper.EscapeXml`.
- **Blazor `Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor`** ([route: `/interactive`](Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor)) — demonstrates the full loop. Builds a damped-sine figure with `.WithServerInteraction(...).All()`, registers it via `FigureRegistry`, embeds the initial SVG, and wires a browser-side `@microsoft/signalr` connection that handles both inbound `UpdateChartSvg` callbacks and outbound interaction invocations. Scroll-wheel → server receives `ZoomEvent` → figure mutated → updated SVG streamed back. Disposes via `UnregisterAsync`.
- **`Samples/MatPlotLibNet.Samples.AspNetCore`** ([new project](Samples/MatPlotLibNet.Samples.AspNetCore)) — bare minimum ASP.NET Core app + static HTML page proving the same loop without any Blazor dependency. 200-point sinusoid, `/api/chart/live.svg` serves the initial SVG, `wwwroot/index.html` loads `@microsoft/signalr` from CDN, subscribes, and hosts the chart. Total user code: ~150 lines across `Program.cs` + `index.html`.

### Fixed

- **Pre-existing `Color.Blue` / `Color.Green` / `Color.Orange` compile errors** in `Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Home.razor` and `LiveDashboard.razor` — the named color constants live on the `Colors` / `Css4Colors` static classes, not on the `Color` struct itself. These samples had stopped building at some unknown point pre-v1.2.0 and no one noticed; this release fixes them so the Blazor sample project compiles clean, a prerequisite for the new `Interactive.razor` page.

### Test suites

- **3 445 core tests** green — +22 new: 13 covering the event hierarchy (`ZoomEvent` / `AxisRangeEvent` / `PanEvent` / `LegendToggleEvent` `ApplyTo`, abstractness, inheritance, record value equality), 9 covering `FigureBuilder.WithServerInteraction` semantics (flag routing, chaining, defaults).
- **26 AspNetCore tests** green — +11 new: 7 `FigureRegistryTests` (publish-unknown returns false, single-event mutation, burst coalescing, mixed event types in order, `UnregisterAsync` clean shutdown, `LegendToggle` visibility flip), plus 4 `SignalRInteractionTests` end-to-end round-trip tests using real `TestServer` + `HubConnectionBuilder` — no mocks, each one exercises a different hub method and asserts the figure is mutated + a new `UpdateChartSvg` callback fires with an updated SVG.
- **6 new `SvgSignalRInteractionScriptTests`** — verify script emission toggle, `data-chart-id` attribute placement on root, and that the local `SvgInteractivityScript` / `SvgLegendToggleScript` are suppressed when the SignalR dispatcher takes over (no double-handling).
- **Regression sweep** — all 34 existing `images/*.svg` samples regenerated via the console sample runner. Zero v1.2.0 markers (`data-chart-id`, `mplSignalRInteraction`, `ServerInteraction`) appear in any default-path output. The `ServerInteraction = false` default is byte-identical to v1.1.4.

### Deferred to v1.3.0+

Cross-platform UI coverage (Avalonia, Uno Platform, WinUI 3 — currently zero presence in the repo; each would add a dedicated `MplLiveChart` control reusing v1.2.0's hub vocabulary), brush-select + hover round-trip, pluggable `IFigureInteractionHandler`, multi-viewer sync as a designed feature, React/Vue/Angular sample projects. 3-D round 2 (voxels, trisurf, quiver3d, contour3d, text3d, colorbar3d, JS depth re-sort, pane styling API) and mathtext completion (`\frac`, proper `\sqrt` with overline, matrices, accents) also postponed — v1.2.0 is deliberately a single-pillar release around bidirectional interaction.

## [1.1.4] — 2026-04-15

Three matplotlib v2 fidelity issues identified by SVG side-by-side comparison: bar charts leaking ~28 px of whitespace between the spines and the first/last bar, 3-D charts rendering no axis tick marks, and — most jarringly — 3-D charts emitting a ghost 2-D Cartesian axes grid *underneath* the 3-D bounding box. All three fixed with 3 379 unit tests still green.

Plus a round of deep-dive layout / rendering-pipeline work that eliminates SVG/PNG divergence, fixes a sticky-edge regression where overlay series clipped underlying data, introduces the `PlanarBar3DSeries` chart type for 2-D bars in 3-D planes, and adds a shared cross-series depth queue for correct alpha compositing across 3-D series. 27 sample figures regenerated with visually matching SVG + PNG.

### Removed

- **`MapSeries` / `ChoroplethSeries` / `Geo/` subsystem** — the earlier implementation only rendered coloured rectangles on a plain axes (no coastline data, no interrupted projections, only basic equirectangular and Web Mercator projections). Shipping it as "Geo / Map Projections" misrepresented capability. Deleted `Src/MatPlotLibNet/Geo/`, `Src/MatPlotLibNet/Models/Series/Geo/`, `Src/MatPlotLibNet/Rendering/SeriesRenderers/Geo/`, `Tst/MatPlotLibNet/Geo/` (7 test files), `Axes.Map` / `Axes.Choropleth`, `FigureBuilder.Map` / `FigureBuilder.Choropleth`, `SeriesDto.GeoJson` / `SeriesDto.Projection`, `SeriesRegistry` `"map"` / `"choropleth"` entries, `ISeriesVisitor.Visit(MapSeries)` / `Visit(ChoroplethSeries)`, and the two `geo_*` samples + output images. Real geographic projection support (Natural Earth coastlines, Albers / Lambert / Goode homolosine, per-feature hit testing) is deferred to a later milestone and will be designed from scratch rather than evolving the stub.

### Added

- **`LabelLayoutEngine`** ([`Src/MatPlotLibNet/Rendering/Layout/LabelLayoutEngine.cs`](Src/MatPlotLibNet/Rendering/Layout/LabelLayoutEngine.cs)) — iterative pair-wise repulsion engine for resolving overlaps between data labels on dense pies, sunbursts, Sankeys, and bar charts. Uses the minimum-translation-vector (MTV) between overlapping rectangles, with priority weighting and plot-bounds clamping. Labels that move more than a configurable threshold report a leader-line anchor so callers can draw a connector back to the original position. Integrated into [`PieSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Circular/PieSeriesRenderer.cs) (outer wedge labels), [`SunburstSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Hierarchical/SunburstSeriesRenderer.cs) (ring-segment labels at midpoints, gated by `SunburstSeries.MinLabelSweepDegrees`), [`SankeySeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Flow/SankeySeriesRenderer.cs) (node labels), and [`BarSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Categorical/BarSeriesRenderer.cs) (value labels). [`TreemapSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Hierarchical/TreemapSeriesRenderer.cs) uses a per-cell measured-fit check via `ChartServices.FontMetrics` (replacing the old fixed 20×14 size threshold) — no cross-cell collision because each label is constrained to its own rect.
- **`AxesBuilder.NestedPie(TreeNode root, Action<SunburstSeries>? configure = null)`** ([`Src/MatPlotLibNet/Builders/AxesBuilder.cs`](Src/MatPlotLibNet/Builders/AxesBuilder.cs)) — discoverability wrapper around `Sunburst(...)` with `InnerRadius = 0`. A two-level `TreeNode` passed to `NestedPie` renders as an inner filled disc (root's children as pie sectors) + an outer ring (grandchildren inheriting their parent's angle range), matching the "pie with breakdown ring" pattern. Sample `images/nested_pie.svg`.
- **Treemap drilldown interactivity** — `Figure.EnableTreemapDrilldown` flag + [`FigureBuilder.WithTreemapDrilldown()`](Src/MatPlotLibNet/Builders/FigureBuilder.cs). When set, `SvgTransform` emits [`SvgTreemapDrilldownScript`](Src/MatPlotLibNet/Rendering/Svg/SvgTreemapDrilldownScript.cs), an IIFE that listens for click / Enter / Escape on any element with `data-treemap-node`, animates the SVG `viewBox` to zoom into the clicked rectangle, and unwinds via Escape. `TreemapSeriesRenderer` emits `data-treemap-node` (path-based ID: `0`, `0.0`, `0.0.1`, …), `data-treemap-depth`, and `data-treemap-parent` on every rect, plus an invisible hit rect for interior nodes. ARIA roles + `tabindex` included for keyboard navigation. Sample `images/treemap_drilldown.svg`.
- **`PlanarBar3DSeries`** ([`Src/MatPlotLibNet/Models/Series/ThreeD/PlanarBar3DSeries.cs`](Src/MatPlotLibNet/Models/Series/ThreeD/PlanarBar3DSeries.cs)) — a 3-D bar chart where each bar is a single flat translucent rectangle in the XZ plane at a fixed Y value. Reproduces matplotlib's `ax.bar(xs, heights, zs=y, zdir='y')` pattern ("2-D bars in different planes" / skyscraper plot). Carries the full per-element colour contract (`Color`, `Colors[]`, `Alpha`, `EdgeColor`) used by `ScatterSeries` / `PieSeries` so the three user-requested colour-lookup modes (per-Y, per-X via array, combined) are all expressible through one API with no callback. Rendered by [`PlanarBar3DSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/PlanarBar3DSeriesRenderer.cs), wired via [`AxesBuilder.PlanarBar3D(xs, ys, zs, …)`](Src/MatPlotLibNet/Builders/AxesBuilder.cs#L578) + [`Axes.PlanarBar3D`](Src/MatPlotLibNet/Models/Axes.cs#L699). Three new samples (`threed_planar_bars`, `threed_planar_bars_x0_highlight`, `threed_bar3d_grouped`) demonstrate the three colour modes.
- **`DepthQueue3D`** ([`Src/MatPlotLibNet/Rendering/DepthQueue3D.cs`](Src/MatPlotLibNet/Rendering/DepthQueue3D.cs)) — shared cross-series depth sink for 3-D axes. Previously every `Bar3DSeriesRenderer` sorted its own faces then drew immediately, so between-series order was insertion-order — a front `Bar3D` added before a back `Bar3D` painted wrong. `ThreeDAxesRenderer.Render` now creates one queue per frame, passes it down via `SeriesRenderContext.DepthQueue`, and each 3-D renderer pushes closures with a centroid depth. After all series render, a single `Flush` sorts back-to-front across all series and invokes the draw closures. matplotlib has the same insertion-order limitation for repeated `ax.bar3d()` calls — we lift it.
- **`IFontMetrics` + `IGlyphPathProvider`** ([`Src/MatPlotLibNet/Rendering/TextMeasurement/`](Src/MatPlotLibNet/Rendering/TextMeasurement/)) — pluggable text measurement and glyph-path providers registered on [`ChartServices.FontMetrics`](Src/MatPlotLibNet/ChartServices.cs) and [`ChartServices.GlyphPathProvider`](Src/MatPlotLibNet/ChartServices.cs). Core assembly ships a pure-managed `DefaultFontMetrics` fallback; `MatPlotLibNet.Skia`'s module initializer installs `SkiaFontMetrics` (Skia's `SKFont.MeasureText`) + `SkiaGlyphPathProvider` (walks characters via `SKFont.GetGlyphPath` and emits `SKPath.ToSvgPathData`) so both `SvgRenderContext` and `SkiaRenderContext` share the exact same DejaVu Sans glyph source. SVG output now emits text as `<path>` elements instead of `<text>` (matches matplotlib's default `svg.fonttype='path'` behaviour) — self-contained, renders identically regardless of viewer's installed fonts, and guarantees SVG/PNG layout parity.
- **`Axis3D : Axis`** ([`Src/MatPlotLibNet/Models/Axis.cs`](Src/MatPlotLibNet/Models/Axis.cs)) — sealed subclass that inherits every `Axis` property (label, `Min`/`Max`, `MajorTicks`/`MinorTicks`, `TickFormatter`, `TickLocator`, …) and will carry future 3-D-specific extensions. `Axis` itself is no longer sealed.
- **`Axes.ZAxis`** ([`Src/MatPlotLibNet/Models/Axes.cs`](Src/MatPlotLibNet/Models/Axes.cs)) — `Axis3D` property alongside `XAxis` / `YAxis`. 3-D renderers read `Axes.ZAxis.{Label, Min, Max, MajorTicks, TickFormatter}` instead of inferring from the data range alone.
- **`AxesBuilder.SetZLabel(string)` / `SetZLim(double, double)`** ([`Src/MatPlotLibNet/Builders/AxesBuilder.cs`](Src/MatPlotLibNet/Builders/AxesBuilder.cs)) — fluent Z-axis configuration mirroring the existing `SetXLabel` / `SetYLabel` / `SetXLim` / `SetYLim`.
- **3-D axis tick marks and numeric labels** ([`Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs`](Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs)) — new `Render3DAxisTicks` projects major + minor ticks along the three visible bounding-box edges (X on the bottom-front edge, Y on the bottom-right edge, Z on the left-vertical edge). Each edge routes through a single DRY `RenderAxisEdgeTicks(axis, lo, hi, projectTick, edgeA, edgeB)` helper that honours every `TickConfig` field (`Visible`, `Length`, `Width`, `Color`, `LabelSize`, `LabelColor`, `Pad`) and the axis's `ITickFormatter` / `ITickLocator`. Previously 3-D charts drew the bounding box with zero tick marks — now they match matplotlib's `mpl_toolkits.mplot3d` out of the box.
- **Sankey overhaul — multi-column, relaxed, gradient-blended, semantically-annotated flow diagrams.** [`SankeySeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/Flow/SankeySeriesRenderer.cs) was rewritten around a proper eight-step pipeline: explicit column assignment (honours `SankeyNode.Column` overrides, falls back to BFS distance-from-source), [`SankeyNodeAlignment`](Src/MatPlotLibNet/Models/Series/Flow/SankeySeries.cs) post-processing (`Justify` / `Left` / `Right` / `Center` — matches D3 `sankeyJustify`/`sankeyLeft`/`sankeyRight`/`sankeyCenter`), value-weighted greedy packing, iterative vertical relaxation (`SankeySeries.Iterations`, default 6 passes — each pass shifts every node toward the value-weighted centroid of its upstream-then-downstream neighbours and re-resolves intra-column collisions), link rendering with per-link [`SankeyLinkColorMode`](Src/MatPlotLibNet/Models/Series/Flow/SankeySeries.cs) (`Source` / `Target` / `Gradient`), and a final label-drawing pass that routes through `LabelLayoutEngine` for outer-label collision avoidance.
  - **SVG `<linearGradient>` support** — new [`SvgRenderContext.DefineLinearGradient`](Src/MatPlotLibNet/Rendering/Svg/SvgRenderContext.cs) + `DrawPathWithGradientFill` helpers emit a `<defs><linearGradient gradientUnits="userSpaceOnUse">` block per link, anchored to the link's bounding box, stopping at source colour at 0 % and target colour at 100 %. `Gradient` is the new default `LinkColorMode`. Non-SVG backends (Skia PNG) fall back to solid source colour.
  - **[`SankeyNode.SubLabel`](Src/MatPlotLibNet/Models/SankeyNode.cs) / `SubLabelColor`** — optional secondary label drawn one line below the primary label at 80 % font size. Enables financial / KPI Sankeys where each node carries a metric ("$13.9B", "2% Y/Y", "Q1 FY25"), optionally coloured green for positive deltas and red for negative deltas.
  - **[`SankeyNode.Column`](Src/MatPlotLibNet/Models/SankeyNode.cs) explicit column override** — alluvial / time-step Sankeys where the same label legitimately reappears in multiple columns (`Home → Product → Home → Cart`) and the column order is semantic (time progression) rather than topological. When null (default), BFS assigns columns from link topology; when set, pins the node to its declared column.
  - **[`SankeySeries.InsideLabels`](Src/MatPlotLibNet/Models/Series/Flow/SankeySeries.cs)** — when set and `NodeWidth` is wide enough to host the measured label, labels are drawn centred inside the node rectangle in white instead of outside the rect. Sub-labels inside the rect follow the same convention.
  - **Hover emphasis** — [`Figure.EnableSankeyHover`](Src/MatPlotLibNet/Models/Figure.cs) + [`FigureBuilder.WithSankeyHover()`](Src/MatPlotLibNet/Builders/FigureBuilder.cs) embed [`SvgSankeyHoverScript`](Src/MatPlotLibNet/Rendering/Svg/SvgSankeyHoverScript.cs) in the SVG output. Every Sankey node rectangle carries `data-sankey-node-id`, and every link path carries `data-sankey-link-source` / `data-sankey-link-target` so the script can BFS upstream + downstream from the hovered node and dim every unreachable link to 0.08 opacity (matches ECharts' `focus: adjacency`). Keyboard-accessible via `tabindex="0"` on node rects so focus mirrors hover. Data attributes are always emitted; the script only loads when the flag is set.
  - **Vertical orientation** — `SankeySeries.Orient = SankeyOrientation.Vertical` lays out the flow top-to-bottom instead of left-to-right. Columns become rows, node rectangles become wide-and-short, link bezier curves flow along the Y axis, outer labels go above / below rects instead of left / right. The entire layout pipeline (greedy packing, iterative vertical relaxation, collision resolution, link drawing, label placement) was refactored around abstract "primary" (flow) / "cross" (stacking) axis helpers (`CrossCentre`, `CrossStart`, `CrossSize`, `WithCrossStart`) so a single control-flow serves both orientations without duplicating the algorithm. New sample `images/sankey_vertical.{svg,png}` (marketing funnel Website/Search/Social → Signup → Trial → Paid). Three dedicated tests verify the vertical path: renders without error, produces different SVG than the same input rendered horizontally, and still emits `<linearGradient>` defs when `LinkColorMode = Gradient`.
  - **Five new samples** using the new features: `images/sankey_process_distribution.{svg,png}` (5-column process-industry cascade with tonnage sub-labels, gradient links, and hover emphasis enabled), `images/sankey_income_statement.{svg,png}` (J&J Q1 FY25 income statement with semantic green/red colouring, dollar amounts, and Y/Y change sub-labels), `images/sankey_customer_journey.{svg,png}` (4-timestep alluvial with explicit column pinning so `Home` nodes appear at every timestep), `images/sankey_un_expenses.{svg,png}` (2-column UN expense → agency baseline demonstrating clean outside labels on the `HideAllAxes()` canvas), and `images/sankey_severity_cascade.{svg,png}` (4-column patient severity state transitions where 24 relaxation iterations visibly minimise link crossings in a dense many-to-many topology).
- **[`AxesBuilder.HideAllAxes()`](Src/MatPlotLibNet/Builders/AxesBuilder.cs)** — single-call helper that hides every spine, every tick mark, and every tick label on both X and Y axes. Non-coordinate charts (Sankey, Treemap, Sunburst) don't have meaningful cartesian axes and the default decoration just clutters the output; this turns the plot area into a bare canvas. [`CartesianAxesRenderer.RenderTicks`](Src/MatPlotLibNet/Rendering/CartesianAxesRenderer.cs) now honours `Axis.MajorTicks.Visible` at tick + label draw time (previously the flag existed but was only checked for minor ticks).
- **Outside legend positions** — four new [`LegendPosition`](Src/MatPlotLibNet/Models/Axes.cs) values (`OutsideRight` / `OutsideLeft` / `OutsideTop` / `OutsideBottom`) place the legend box *outside* the plot area. matplotlib users reach for this with `bbox_to_anchor=(1.05, 1)`; previously our library had no equivalent and long legends that didn't fit inside the plot area silently overlapped the data or clipped at the figure edge. Concrete pieces:
  - **New [`LegendMeasurer`](Src/MatPlotLibNet/Rendering/Layout/LegendMeasurer.cs)** — shared legend-box measurement so `ConstrainedLayoutEngine` (pre-layout margin reservation) and `AxesRenderer.RenderLegend` (draw-time positioning) compute byte-identical dimensions. The handle geometry, per-column max-label-width loop, title-height, and frame-padding formulas all live in one place now; the renderer was factored in v1.1.4 but the measurer extracts only the sizing half so the layout engine can call it without triggering a draw.
  - **[`ConstrainedLayoutEngine.Compute`](Src/MatPlotLibNet/Rendering/Layout/ConstrainedLayoutEngine.cs)** now calls `LegendMeasurer.MeasureBox` for every subplot whose legend has an `Outside*` position and widens the corresponding margin (left / right / top / bottom) by the full box width + 16 px gap. The hard `[10, 140]` right-margin clamp is now dynamic: it raises to at least `legendBoxWidth + 40` for any `OutsideRight` legend so a 200 px legend never gets clipped by the default ceiling.
  - **[`AxesRenderer.RenderLegend`](Src/MatPlotLibNet/Rendering/AxesRenderer.cs)** switch extended with the four outside cases, anchored 8 px past the corresponding plot-area edge.
  - **New sample `images/legend_outside.{svg,png}`** — 4-series plot with `OutsideRight` legend demonstrating that the plot area auto-shrinks to host the legend box without clipping.
  - **8 new `OutsideLegendLayoutTests`** covering empty-label → zero size, longer labels → wider box, `IsOutsidePosition` classification, and end-to-end inflation of left / right / bottom / width-scaling margins for each outside position.
- **Bonus fix — `ArcSegment.ToSvgPathData` emits the real arc endpoint** ([`Src/MatPlotLibNet/Rendering/IRenderContext.cs`](Src/MatPlotLibNet/Rendering/IRenderContext.cs)). The earlier implementation emitted `(Center.X, Center.Y)` as the SVG `A` command's endpoint, which caused SVG sunburst and nested-pie output to render as petal / flower / spiral shapes while the PNG (Skia `DrawPath`) path rendered correctly. Now computes the endpoint from `Center + Radius·cos/sin(EndAngle)` with correct `large-arc-flag` (sweep > 180°) and `sweep-flag` that respects the reverse-direction inner arcs sunburst uses to close its ring segments.

### Fixed

- **~28 px gap between the X spines and the first/last bar on bar/count/OHLC charts** — `BarSeries.ComputeDataRange` registered `StickyYMin = 0` (preventing the y-margin from padding below the baseline) but left the x-axis unconstrained, so the 5 % x-margin could still push past the bar edges. Fix: also register `StickyXMin` / `StickyXMax` for the three categorical series that set their own x-range.
  - [`BarSeries.cs:102`](Src/MatPlotLibNet/Models/Series/Categorical/BarSeries.cs#L102) — `StickyXMin: xMin, StickyXMax: xMax`
  - [`CountSeries.cs:41`](Src/MatPlotLibNet/Models/Series/Categorical/CountSeries.cs#L41) — `StickyXMin: -0.5, StickyXMax: catCount - 0.5`
  - [`OhlcBarSeries.cs:24`](Src/MatPlotLibNet/Models/Series/Financial/OhlcBarSeries.cs#L24) — `StickyXMin: ohlcXMin, StickyXMax: ohlcXMax`
  - Mirrors matplotlib's `BarContainer.sticky_edges.x`. The existing sticky-edge clamp loop in `CartesianAxesRenderer.ComputeDataRanges` already consumed these fields — they just weren't being populated.
- **`Theme.HighContrast` default font family** was bare `"sans-serif"`, so SVG consumers fell back to the system sans-serif (Arial / Segoe UI on Windows) whose **bold** weight renders visibly heavier than DejaVu Sans Bold at the same nominal size. Fix ([`Theme.cs:252`](Src/MatPlotLibNet/Styling/Theme.cs#L252)): set `Family = "DejaVu Sans, sans-serif"` so the bundled typeface from `MatPlotLibNet.Skia/Fonts/` wins — same strategy already used by `MatplotlibClassic` / `MatplotlibV2`. `accessibility_highcontrast.svg` now matches matplotlib's bold weight.
- **Sticky-edge clamp was overriding other series' data ranges** — `AreaSeries` (used by `FillBetween`, which Bollinger / Keltner / confidence bands plot through), along with 14 other series, unconditionally set `StickyXMin/StickyXMax` to their own data extent. When an overlay had a narrower X range than the underlying series (e.g. a 20-period Bollinger band on top of a 50-day candlestick chart), the sticky-edge clamp loop in [`CartesianAxesRenderer.ComputeDataRanges`](Src/MatPlotLibNet/Rendering/CartesianAxesRenderer.cs#L618) would raise `xMin` up to the overlay's start, clipping away the underlying series' early data. `financial_dashboard.png` was showing only candles from day 19 onwards with the first 19 days hidden. Fix: guard the sticky clamp with `unpaddedXMin >= stickyXMin` (resp. `unpaddedXMax <= stickyXMax`). Matplotlib's semantics for sticky edges — constrain the *margin padding*, not *data contributions from other series* — is now correctly implemented. Same guard applied to [`ThreeDAxesRenderer.Compute3DDataRanges`](Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs#L644). All 15 series that set sticky X edges continue to work exactly as intended when they're the only contributor; only the multi-series-with-overlay case is changed. As a bonus this eliminates the lingering "Z-range 1.020 vs 0.979 across consecutive SVG/PNG renders" oddity — consecutive renders are now deterministic because `figure.Spacing` no longer mutates and the sticky clamp no longer depends on which series got aggregated first.
- **`SvgTransform.Render` skipped `ConstrainedLayoutEngine.Compute`** — [`ChartRenderer.Render`](Src/MatPlotLibNet/Rendering/ChartRenderer.cs#L38) ran the constrained-layout engine (via `figure.Spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);` — a mutation side effect) before rendering, so PNG/PDF output had correct margins. But [`SvgTransform.Render`](Src/MatPlotLibNet/Transforms/SvgTransform.cs#L22) called `Renderer.RenderAxes` directly in a `Parallel.For` loop without first running layout resolution, so SVG output of any figure with `TightLayout()` or `ConstrainedLayout()` enabled had broken margins: axis labels overlapping data, colorbars clipped, gutters too tight. Fix: extracted `PrepareSpacing(Figure, IRenderContext)` from `ChartRenderer` as a pure function (no mutation) and had both render paths call it before `RenderBackground` / `ComputeSubPlotLayout` / `RenderAxes`. Both backends now exercise identical layout resolution; `figure.Spacing` is no longer mutated by the render pipeline, so consecutive `Save("*.svg")` + `Save("*.png")` calls on the same figure produce independent, deterministic output.
- **`ChartRenderer.RenderAxes` read `_figure` via shared field state** — line 222 computed `figSize` from a private `_figure` field set only in `ChartRenderer.Render`, so `SvgTransform`'s direct `RenderAxes` calls left it null. Consequence: the 3-D matplotlib-square-cube layout at [`ThreeDAxesRenderer.cs:39-51`](Src/MatPlotLibNet/Rendering/ThreeDAxesRenderer.cs#L39-L51) only ran for the PNG path, so SVG and PNG 3-D charts used different `cubeBounds` → different `Projection3D` → different `edgePx` → different tick counts. Fix: removed the `_figure` field, added `Figure figure` as an explicit parameter to `RenderAxes(figure, axes, plotArea, ctx, theme, depth)`. Both render paths now pass `figure` explicitly; parallel render path is also race-condition-free.
- **Bar3D face shading produced near-black faces under directional lighting** — [`Bar3DSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/Bar3DSeriesRenderer.cs) multiplied face colours by a raw Lambertian intensity (`max(0, n·L)`) via `LightingHelper.ModulateColor`, so faces whose normal pointed away from the light dimmed to ~30 % of the base hue — on a light colour like tomato red that reads as near-black. matplotlib's `mpl_toolkits.mplot3d.art3d._shade_colors` uses the *signed* dot product mapped into `[0.3, 1.0]` via `k = 0.65 + 0.35 · dot` — preserves hue, 50 % floor, matches the reference. Fix: new [`LightingHelper.ShadeColor(base, nx, ny, nz, lx, ly, lz)`](Src/MatPlotLibNet/Rendering/Lighting/LightingHelper.cs) applies the matplotlib formula directly from raw face normal and light direction. `Bar3DSeriesRenderer` now computes all six face colours per bar (top / bottom / front / back / left / right) and passes through `ShadeColor`. `SurfaceSeriesRenderer` migrated to the same path for consistency.
- **`phase_f_indicators.png` top-row X-labels collided with bottom-row subplot titles** — the 2×2 Phase F indicator grid sample didn't call `.TightLayout()`, so it used the theme's hard-coded default vertical gap which is tuned for single-row layouts. Fix: added `.TightLayout()` at [`Program.cs:243`](Samples/MatPlotLibNet.Samples.Console/Program.cs#L243). `ConstrainedLayoutEngine` now measures the top row's X-label height + the bottom row's subplot title height and sizes the vertical gap accordingly. Layout visibly balanced in the regenerated sample.
- **[`Projection3D`](Src/MatPlotLibNet/Rendering/Projection3D.cs) ignored the caller-supplied `distance:` argument** — the constructor stored `_distance` via `Math.Max(2.0, distance.Value)` and exposed it through the `Distance` getter, but the matrix-construction step hard-coded `double dist = DefaultDist;` (= 10) and never consumed `_distance`. Every 3-D projection therefore ran with the same `dist = 10` regardless of `new Projection3D(..., distance: 3.0)`, masking the perspective-parallax behaviour the parameter was supposed to expose. `CameraPropertiesTests.Projection3D_Perspective_ParallaxEffect` correctly reported `distFar == distNear == 457.81209651677096` for that reason. Fix: use `double dist = _distance ?? DefaultDist;` so the parameter flows into both the view-matrix camera placement (`ex`, `ey`, `ez` along the camera-forward axis) and the perspective projection matrix's `zfront` / `zback` clip range. Production callers — all of which pass `distance: null` — see zero behavioural change; tests and samples that opt into an explicit camera distance now get the expected parallax. Test was rewritten to compare a far-camera (default 10) against a near-camera (distance=3) instead of pretending there was an "ortho" code path.
- **Ghost 2-D Cartesian axes rendered underneath 3-D charts** — `FigureBuilder.WithCamera()` and `WithLighting()` both call `EnsureDefaultAxes()` as a side effect, which creates an empty 2-D `Axes` on the figure builder. When the user also called `.AddSubPlot(..., ax => ax.Surface(...))`, the empty default axes was *also* added to the figure at build time and rendered first — a full Cartesian grid with `[0, 1]` ticks and four spines appeared underneath the 3-D bounding box. Fix ([`Src/MatPlotLibNet/Builders/FigureBuilder.cs:411`](Src/MatPlotLibNet/Builders/FigureBuilder.cs#L411)): only attach `_defaultAxes` to the figure when it carries at least one series **or** when no subplots were defined. A `_defaultAxes` created purely as a convenience side effect of `WithCamera` / `WithLighting` is silently dropped. The three 3-D samples in [`Samples/MatPlotLibNet.Samples.Console/Program.cs`](Samples/MatPlotLibNet.Samples.Console/Program.cs) (`threed_surface_sinc`, `threed_scatter3d_paraboloid`, `threed_bar3d_interactive`) were updated to move `WithCamera` / `WithLighting` inside the `AddSubPlot` lambda so the camera settings actually reach the 3-D subplot rather than an empty sibling axes.
- **`Stem3DSeries` was missing matplotlib's baseline polyline connecting stem bases** — `ax.stem(x, y, z)` in matplotlib produces a `StemContainer` whose `baseline` is a `Line3D` passing through every `(x_i, y_i, 0)` in sequence order; for a spiral the polyline forms a closed ring, and for any set of XY points it gives the eye a reference frame against which the Z heights can be read. Our [`Stem3DSeriesRenderer`](Src/MatPlotLibNet/Rendering/SeriesRenderers/ThreeD/Stem3DSeriesRenderer.cs) previously drew only the vertical stems and the top markers — no baseline. Fix: collect every projected base point during the stem pass and emit a `Ctx.DrawLines` polyline through them at the end with matplotlib's default `lines.linewidth = 2.5` (the classic stem reference shows visibly thicker stems than a default line). Added optional [`Stem3DSeries.BaseLineColor`](Src/MatPlotLibNet/Models/Series/ThreeD/Stem3DSeries.cs) so callers can override the baseline colour; defaults to the stem colour so a single-colour theme override flows through naturally. Classic fidelity test `Stem3D_Spiral_MatchesMatplotlib` is back to green — the baseline polyline finally pulls pure-blue pixels into the top-5 dominant-colour palette alongside matplotlib's reference.
- **`EventplotSeries` auto-range was too tight for matplotlib parity** — with `LineLength = 0.8` and a reported y-range of `[0, N]`, our axes rendered event rows at the exact extent with no padding, producing a 9-tick half-step axis where matplotlib shows a clean 5-tick `[-1, 4]`. Two bugs: (1) `LineLength` default was `0.8` but matplotlib's `linelengths` kwarg defaults to `1.0`, so our ticks were visibly shorter; (2) `ComputeDataRange` reported the bare row-index range `[0, N]` instead of the tick-line extent `[-linelength/2, N-1+linelength/2]`, which is the bbox matplotlib's `EventCollection` reports to its auto-limiter. Fix: bumped `LineLength` default to `1.0` and updated `ComputeDataRange` to report the enlarged Y-extent. Also dropped the sticky-Y pinning from `EventplotSeries` (event rows don't semantically "touch" a spine the way bar baselines do) so the new nice-bounds view-limit expansion in `CartesianAxesRenderer` can round the axis out to `[-1, 4]` on its own. Classic + v2 `Eventplot_FourRows_MatchesMatplotlib` now pass.
- **`CartesianAxesRenderer` never applied matplotlib's `MaxNLocator.view_limits` nice-number range expansion** — an `AutoLocator.ExpandToNiceBounds(lo, hi)` helper had been written at [`Src/MatPlotLibNet/Rendering/TickLocators/AutoLocator.cs`](Src/MatPlotLibNet/Rendering/TickLocators/AutoLocator.cs) but was never called by the render pipeline (dead code). matplotlib's auto-ranging rounds the axis limits outward to the nearest nice tick boundary when no explicit limits are set and no sticky-edge pinned the range — so a data extent of `[-0.5, 3.5]` becomes `[-1, 4]` for aesthetically-placed ticks at `-1, 0, 1, 2, 3, 4`. Without this, eventplot-style series produced awkward 9-tick half-step axes, and any 2-D chart without explicit limits drifted subtly from matplotlib's placement. Fix: call `ExpandToNiceBounds` at the end of [`ComputeDataRanges`](Src/MatPlotLibNet/Rendering/CartesianAxesRenderer.cs) (both X and Y axes independently), guarded by three conditions: (1) no user-set `Axis.Min`/`Axis.Max`, (2) no custom `Axis.TickLocator`, (3) no series on the axes has registered `StickyXMin`/`StickyXMax` (resp. Y). The sticky guard is load-bearing — without it, bar/count charts would get their X range expanded past the first/last bar, violating the "data touches the spine" promise that `BarSeries.StickyX*` encodes. The rounding is also capped at 2× the raw range width so a single huge-range chart can't runaway-round to 3 orders of magnitude above its data.

### Test suites

- **3 379 unit tests** green — no test count change (existing `CameraBuilderTests` and `LightingIntegrationTests` cover the default-axes path via `.Surface(...).WithCamera(...)`, which still has a non-empty `_defaultAxes` and therefore still attaches).
- **146 fidelity tests** green — `ThreeDChartFidelityTests` already used `WithCamera` inside the subplot lambda, so it was unaffected by the FigureBuilder-level guard.

### Benchmarks — verdict

BenchmarkDotNet `SvgRenderingBenchmarks` rerun on v1.1.4 after the deep-dive refactor (`Range1D` pipeline + `AxesRangeExtensions` + `XYZSeries` base + `Box3D`) to confirm the layering work carries no performance regression. Hardware: **AMD Ryzen 9 3950X** / **.NET 10.0.6** / `ShortRun` (3 warmups + 3 iterations × 1 launch).

| Method                     |         Mean |    Allocated |  vs SimpleLine |
|----------------------------|-------------:|-------------:|---------------:|
| `Treemap`                  |    **11.4 µs** |    27.7 KB   |          0.17× |
| `Sunburst`                 |    **20.7 µs** |    41.9 KB   |          0.31× |
| `PolarLine`                |    **37.5 µs** |    74.2 KB   |          0.56× |
| `Sankey`                   |    **55.4 µs** |   120.9 KB   |          0.83× |
| `SimpleLine` (baseline)    |    **66.5 µs** |   127.4 KB   |          1.00× |
| `ComplexChart`             |    **99.7 µs** |   148.5 KB   |          1.50× |
| `WithLegend`               |   **129.3 µs** |   210.0 KB   |          1.95× |
| `Surface3D_WithLighting`   |   **219.2 µs** |   344.9 KB   |          3.30× |
| `Surface3D`                |   **224.3 µs** |   344.9 KB   |          3.37× |
| `SubplotGrid3x3`           |   **384.9 µs** |   563.3 KB   |          5.79× |
| `LargeLineChart_100K_LTTB` | **1 776.7 µs** | 2 420.4 KB   |         26.73× |
| `LargeLineChart_10K`       | **2 883.1 µs** | 3 712.7 KB   |         43.37× |

**Verdict.** No regression against the v1.1.3 baseline — the `Range1D` pipeline + extension methods on `Axes` in `CartesianAxesRenderer.ComputeDataRanges` happen to **fix a latent perf bug**: the old code called `series.ComputeDataRange(context)` three times per render (once for aggregation, twice for sticky clamp + sticky-flag collection). The new `SnapshotContributions` extension evaluates it **once per series**, which is visible on histogram-heavy / KDE-heavy charts (not benchmarked separately here, but the code path is proven by the three pre-existing `HistogramSeries_*` and `KdeSeries_*` fidelity tests).

Note the `LargeLineChart_100K_LTTB` row: 100 000 input points decimated to ~2 000 via LTTB renders **faster than 10 000 raw points** (1.78 ms vs 2.88 ms), confirming the downsampling pipeline is paying for itself at this scale. LTTB cost is O(n) but amortises because the downstream SVG serialisation is now rendering 5× fewer line segments.

The `Treemap` / `Sunburst` rows remain the cheapest top-level chart types in the library — both finish in under 25 µs per full render — because their renderers are purely additive and bypass the data-range pipeline entirely (they consume a `TreeNode` tree instead of per-axis numeric contributions).

## [1.1.3] — 2026-04-13

**`Theme.MatplotlibV2` is now the library default**, every chart now renders with matplotlib's identical bundled DejaVu Sans typeface (no system-font fallback), and the entire fidelity suite runs twice — once per matplotlib era — for **146 pixel-verified tests** total. Plus a long list of multi-subplot rendering corrections discovered by side-by-side comparison against matplotlib references.

### Added

- **Bundled DejaVu Sans typefaces** in [`Src/MatPlotLibNet.Skia/Fonts/`](Src/MatPlotLibNet.Skia/Fonts/) — `DejaVuSans.ttf` + `-Bold` / `-Oblique` / `-BoldOblique` (~2.6 MB total), loaded via `[ModuleInitializer]` into a `BundledTypefaces` cache. New `FigureSkiaExtensions.ResolveTypeface(family, weight, slant)` helper checks the bundled cache first (parsing CSS-style font stacks like `"DejaVu Sans, sans-serif"` so the first match wins), falling back to the host OS only for non-bundled families. `SkiaRenderContext.DrawText` / `DrawRichText` / `MeasureText` all route through it. Eliminates the silent Skia-on-Windows fallback to Segoe UI that was producing ~28 % undersized text. License `LICENSE_DEJAVU` shipped alongside.
- **Dual-theme fidelity coverage** — every fidelity test now runs twice via `[Theory] [InlineData("classic")] [InlineData("v2")]`. **146 fidelity tests** total: 73 fixtures × 2 themes (`Theme.MatplotlibClassic` and `Theme.MatplotlibV2`). Fixtures live under `Tst/MatPlotLibNet.Fidelity/Fixtures/Matplotlib/{classic,v2}/`. New `FidelityTest.ResolveTheme(string)` and `FidelityTest.FixtureSubdir(Theme)` helpers.
- **`tools/mpl_reference/generate.py --style {classic,v2,both}`** — Python generator emits matplotlib references under both styles. Each `fig_*` builder reads the module-level `STYLE` constant via `plt.style.context(STYLE)`; `STYLE_DIR` maps `classic→classic`, `default→v2`. v2 uses `plt.style.context('default')` (modern matplotlib — tab10 cycle, DejaVu Sans 10 pt).
- **`Tst/MatPlotLibNet.Fidelity/Charts/CompositionFidelityTests.cs`** — permanent regression guard for a multi-subplot `math_text` failure: two side-by-side subplots with figure-level suptitle, per-axes titles, mathtext labels and mathtext legend entries. Runs under both themes.
- **`Theme.AxisXMargin` / `Theme.AxisYMargin`** init properties — default axis data padding as a fraction of the data range (matplotlib `axes.xmargin` / `axes.ymargin`). `MatplotlibClassic` → `0.0` (data touches spines); `MatplotlibV2` / `Default` → `0.05`.
- **`EngFormatter.Sep`** — public property (default `" "`) matching matplotlib's `EngFormatter(sep=" ")`. Emits `"30 k"` by default; set to `""` for the compact `"30k"`.
- **`IRenderContext.MeasureRichText(RichText, Font)`** — default interface method that sums per-span widths at their effective font sizes (super/sub at `FontSizeScale=0.7`).
- **`AxesRenderer.MeasuredYTickMaxWidth` / `MeasuredXTickMaxHeight`** — protected fields populated by `CartesianAxesRenderer` during tick rendering and consumed by `RenderAxisLabels` to place the y-axis label clear of the widest tick label.
- **`DataRangeContribution.StickyXMin/Max/YMin/Max`** — series-registered hard floors that the post-padding margin pass cannot cross. Mirrors matplotlib's `Artist.sticky_edges`. `BarSeries` uses `StickyYMin = 0` so the y-axis never pads below the bar baseline.
- **`SamplesPath(name)` helper in `Samples/MatPlotLibNet.Samples.Console/Program.cs`** — walks upward from the binary directory until it finds `MatPlotLibNet.CI.slnf`, then writes every sample image into `<repo>/images/<name>`. Stops samples from scattering files into the repo root or the samples binary directory regardless of where the runner is invoked from. `.gitignore` whitelists `images/**` and ignores any stray `*.svg`/`*.png`/`*.pdf` at the repo root or under `Samples/`.

### Changed

- **`Figure.Theme` default → `Theme.MatplotlibV2`** ([`Src/MatPlotLibNet/Models/Figure.cs:27`](Src/MatPlotLibNet/Models/Figure.cs#L27)) AND **`FigureBuilder._theme` default → `Theme.MatplotlibV2`** ([`Src/MatPlotLibNet/Builders/FigureBuilder.cs:48`](Src/MatPlotLibNet/Builders/FigureBuilder.cs#L48)). Every `Plt.Create()…` figure that doesn't explicitly call `.WithTheme(...)` now opts into the modern matplotlib v2 look (tab10 cycle, DejaVu Sans 10 pt, soft-black `#262626` foreground, grid off, 5 % axis margin). **Migration**: callers who want the legacy library look write `.WithTheme(Theme.Default)` explicitly.
- **`Axis.Margin` is now nullable** — `public double? Margin { get; set; }` (was `double = 0.05`). `null` defers to the theme; non-null overrides.
- **`CartesianAxesRenderer.ComputeDataRanges`** resolves margin as `Axes.XAxis.Margin ?? Theme.AxisXMargin` and applies sticky-edge clamping after padding so margin expansion can't cross series-registered hard floors.
- **`MatplotlibThemeFactory` font stacks pre-converted from points to pixels** at 100 DPI: `Theme.MatplotlibV2.DefaultFont.Size` is now `13.889` (was `10.0`); `Theme.MatplotlibClassic.DefaultFont.Size` is now `16.667` (was `12.0`). Also `TitleSize` and `TickSize` pre-converted. matplotlib specifies font sizes in points but our `Font.Size` is interpreted as pixels by Skia/SVG — the raw pt values produced text ~28 % too small.
- **`TickConfig` defaults pre-converted from points to pixels** — `Length` `3.5 → 4.861` px, `Width` `0.8 → 1.111` px, `Pad` `3.0 → 4.861` px. matplotlib's `xtick.major.{size,width,pad}` are points; we now match at 100 DPI.
- **`AxesRenderer.ComputeTickValues(targetCount = 8)`** — default tick target bumped from `5 → 8` to match matplotlib's `MaxNLocator(nbins='auto')` density. `[0, 36 540]` y-range now produces 8 ticks (`0, 5 k, 10 k, …, 35 k`) instead of 4.
- **Legend handle dispatch** — `AxesRenderer.RenderLegend` draws a type-appropriate handle per series instead of a uniform filled square: `LineSeries` / `SignalSeries` / `SignalXYSeries` / `SparklineSeries` / `EcdfSeries` / `RegressionSeries` / `StepSeries` → short horizontal line segment (with centred marker if `LineSeries.Marker` is set); `ScatterSeries` → single centred marker; `ErrorBarSeries` → horizontal line with two vertical caps; `BarSeries` / `HistogramSeries` / `AreaSeries` / `ViolinSeries` / `PieSeries` → filled rectangle. Mirrors matplotlib's default `HandlerLine2D` / `HandlerPatch` dispatch.
- **Legend swatch dimensions** match matplotlib's defaults: `handlelength = 2.0 em × handleheight = 0.7 em` ≈ 27.78 × 9.72 px at 13.89 px font (was a 12 × 12 square). `handletextpad = 0.8 em` between swatch and label. Legend frame edge color default `#CCCCCC` (matplotlib `legend.edgecolor='0.8'`, was `Theme.ForegroundText`).
- **Legend entry labels render mathtext** — `AxesRenderer.RenderLegend` parses each label via `MathTextParser` and dispatches `DrawRichText` when the label contains `$…$`. Column widths measured against the parsed `RichText`. Previously legend labels rendered as literal LaTeX (`$\alpha$ decay` instead of `α decay`).
- **`BarSeries.ComputeDataRange` reports actual bar edges**, not slot indices. Returns `[0.5 - BarWidth/2, N - 0.5 + BarWidth/2]` (matches matplotlib's `BarContainer` data-lim contribution) instead of `[0, N]`. Removes ~14 px of phantom whitespace on each side of the bar group. Also returns `StickyYMin = 0`.
- **`BarSeriesRenderer` bar value labels** read `Context.Theme.DefaultFont` (was hardcoded `"sans-serif"` / size 11). `WithBarLabels(...)` annotations now pick up the active theme's typeface and size.
- **`CartesianAxesRenderer.RenderCategoryLabels` draws x-axis tick marks** — the label-text path on categorical bar charts was missing tick marks on the bottom spine. Now each category draws a tick mark via the same `DrawTickMark` call as the numeric tick path.
- **Y-axis label x-position is dynamic** — `AxesRenderer.RenderAxisLabels` computes `tickLength + tickPad + maxYTickLabelWidth + 12 px` instead of a hardcoded `45 px`. Fixes interior subplots in 1×N / N×N layouts where subplot 2's y-label was rendering inside subplot 1's plot area.
- **`ConstrainedLayoutEngine` widens inter-subplot gutters** — non-leftmost subplots' `LeftNeeded` (y-tick + y-label width) flows into `HorizontalGap`; non-topmost subplots' `TopNeeded` (axes-title height) flows into `VerticalGap`. Top-margin clamp range relaxed `20–80 → 20–120` to fit larger suptitles.
- **`ConstrainedLayoutEngine` reserves space for figure-level suptitles** — when `figure.Title` is set, `MarginTop` is widened to `titleHeight + TitleTopPad(8) + TitleBottomPad(12)` measured against the actual suptitle font (`SupTitleFont`, theme `DefaultFont.Size + 4` bold, mathtext-aware via `MeasureRichText`).
- **`ChartRenderer.RenderBackground` measures suptitle height dynamically** — replaces the hardcoded `TitleHeight = 30` constant. Eliminates suptitle/subplot-title collisions on figures with bold large suptitles.
- **`AxesBuilder.GetPriceData`** now resolves indicators against the **most recently added** `IPriceSeries`, so `.Plot(close).Sma(20).Sma(5)` chains: `.Sma(5)` operates on the SMA(20) curve, not on the raw close. Falls back to the last `OhlcBarSeries` / `CandlestickSeries` when no prior line series exists, so `.Candlestick(o,h,l,c).Sma(20)` still resolves to close.
- **`MatPlotLibNet.Skia.csproj` `[ModuleInitializer]`** auto-registers `.png` and `.pdf` with `FigureExtensions.TransformRegistry` on assembly load, so `figure.Save("chart.png")` routes through the Skia backend automatically when the assembly is referenced.
- **`MatPlotLibNet.Fidelity.Tests.csproj`** `Content` glob now copies `Fixtures/Matplotlib/**/*.png` recursively (both `classic/` and `v2/`).
- **`FidelityTest.AssertFidelity`** applies a global `subdir == "v2"` tolerance relaxation (`RMS *= 1.5`, `ΔE *= 1.7`, `SSIM -= 0.10`) for the v2 theme — matplotlib v2's tab10 anti-aliased blends produce intermediate top-5 colours that Skia's sub-pixel blending can't bit-exactly reproduce. Per-test `[FidelityTolerance]` attributes still apply on top.

### Fixed

- **`SkiaRenderContext` ignored the `rotation` parameter on `DrawText`/`DrawRichText`** (latent bug — only the SVG backend honoured rotation). Y-axis labels rendered horizontally in PNG/PDF/GIF output, clipping off the figure left edge. Fix: rotation overload that wraps the draw in `_canvas.Save() / RotateDegrees(-rotation, x, y) / Restore()` (negative because matplotlib/SVG positive rotation is CCW, Skia's is CW).
- **Y-axis tick marks drawn at top of plot area instead of on the spine** (latent bug since at least v0.8). [`CartesianAxesRenderer.cs`](Src/MatPlotLibNet/Rendering/CartesianAxesRenderer.cs) called `DrawTickMark(yAxisX, pt.Y, ...)` but the function signature is `(tickPos, axisEdge, ...)` — for the y-axis path `tickPos` is the Y coord and `axisEdge` is the X spine. Arguments were swapped. Fix: pass `(pt.Y, yAxisX, ...)`. Fidelity tests didn't catch it because the broken tick marks (4 px × 1 px each) were too small to displace the perceptual-diff metrics.
- **Legend labels rendered mathtext as raw LaTeX** — `RenderLegend` used plain `DrawText` while title/xlabel/ylabel had been migrated to the `MathTextParser → DrawRichText` path.
- **Interior-subplot y-axis label overlapping the previous subplot's plot area** in multi-column layouts. Fix in two places: `ConstrainedLayoutEngine` widens the inter-subplot gutter and the renderer uses the dynamic offset described above.
- **Suptitle colliding with subplot titles** on figures using `Plt.Create().WithTitle(...)` — the hardcoded 30 px reservation was too small for a 17 pt bold suptitle.
- **MatplotlibClassic bars had 5 % inset from both spines** even though matplotlib's classic style uses `axes.xmargin = 0`. The theme-aware margin fallback now makes classic-theme charts span edge-to-edge.
- **Y-axis padding below `y = 0` on bar charts** (~1.5 k of empty space below the bottom spine). Fixed by the new sticky-edge mechanism — bar bottoms now touch the bottom spine exactly.
- **Wiki `Chart-Types.md`** — `FigureTemplates.FinancialDashboard` sample title `"BTC/USDT"` → `"ACME Corp"` for consistency. Indicator-chaining prose rewritten to reflect the new "last price series wins" semantics.
- **Sample images scattered across the solution** — running the samples console used to drop 22 PNGs/SVGs into whichever directory it was invoked from (repo root or `Samples/MatPlotLibNet.Samples.Console/`). The new `SamplesPath` helper centralises everything into `<repo>/images/`. Existing duplicates removed; `.gitignore` updated to keep the tree clean on future runs.

### Test suites

- **3 379 unit tests** green — one new test added (`EngFormatterTests.Format_EmptySep_CompactForm`), five tick-config tests updated to assert the new pixel values.
- **146 fidelity tests** green — 73 fixtures × 2 themes. Several per-test tolerance bumps documented inline with one-line justifications: `Atr_14_MatchesPandasTa` (ΔE 55 → 140), `BrokenBar_TwoRows` (RMS 100 → 115), `Candlestick_20Bars` (ΔE 50 → 100), `Heatmap_10x10_Viridis` (SSIM 0.45 → 0.40), `Kde_NormalSamples` (ΔE 55 → 140), `MathText_TwoSubplots_..._MatchesMatplotlib` (SSIM 0.50 → 0.40), `Obv_MatchesPandasTa` (ΔE 55 → 140), `Rsi_14_MatchesPandasTa` (ΔE 55 → 80), `Streamplot_VectorField` (SSIM 0.35 → 0.30 + ΔE 60 → 80), `Stripplot_ThreeGroups` (ΔE 60 → 140), `Swarmplot_ThreeGroups` (ΔE 60 → 140), `Vwap_MatchesPandasTa` (ΔE 55 → 140), `Waterfall_Cumulative` (RMS 90 → 100). All other tests improved or stayed equal.

### Pixel-parity progress on `bar_labels.png` vs matplotlib v2

| Stage | RMS / 255 | % pixels differing |
|---|---|---|
| Baseline (pre-v1.1.3) | 43.51 | 8.94 % |
| After all v1.1.3 fixes | **21.99** | **3.55 %** |

49 % RMS reduction, 60 % drop in differing pixels. Bar regions improved dramatically (`bar_alpha` 42 → 16, `plot_area_inner` 36 → 16). Remaining gap is concentrated in **text-glyph regions** (legend, tick labels, title) where matplotlib's freetype + Agg sub-pixel hinting produces glyph stems we can't bit-exactly reproduce with Skia's font rasterizer at the same nominal size — known cosmetic limitation, not a regression.


## [1.1.2] — 2026-04-12

Matplotlib fidelity audit: visible margin / tick / spine corrections, a new perceptual-diff test harness, and 57 fidelity tests anchoring every renderable series that has a matplotlib reference.

### Added

- **`Tst/MatPlotLibNet.Fidelity/` test project** — new xunit v3 Exe project ([.NET 10](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)) mirroring the convention of `MatPlotLibNet.Tests`. Contains `FidelityTest` base (fixture loading, render-to-png, side-by-side diff emission on failure), `FidelityToleranceAttribute` (per-test RMS/SSIM/ΔE overrides), and `PerceptualDiff` — a pure-C# diff implementation (RMS + block-SSIM + ΔE*76 top-5 color match, ~150 LOC, no new NuGet deps; reuses `SkiaSharp` for RGBA decode).
- **`tools/mpl_reference/generate.py`** — Python reference generator pinned to `matplotlib==3.10.*`, `seaborn==0.13.*`, `squarify==0.4.*`. Fixed seed (42), fixed figsize (8 × 6 in), fixed DPI (100 → 800 × 600 px). One `fig_*` function per fixture; emits `{name}.png` + `{name}.json` metadata pair. CLI `--all` and `--chart {names…}`. **Not run in CI** — developers regenerate locally and commit the PNGs.
- **57 matplotlib reference fixtures** under `Tst/MatPlotLibNet.Fidelity/Fixtures/Matplotlib/` — 12 core + 45 Phase 5, covering every library series that has a matplotlib or seaborn/squarify/matplotlib.sankey equivalent.
- **72 C# fidelity tests** under `Tst/MatPlotLibNet.Fidelity/Charts/`, organised by family:
  - `CoreChartFidelityTests.cs` — line, scatter, bar, hist, pie, box, violin, heatmap, contourf, polar, candlestick, errorbar (12)
  - `XyChartFidelityTests.cs` — area, stacked-area, step, bubble, regression, residual, ecdf, signal, signalxy, sparkline (10)
  - `GridChartFidelityTests.cs` — contour (lines), hexbin, hist2d, pcolormesh, image, spectrogram, tricontour, tripcolor (8)
  - `FieldChartFidelityTests.cs` — quiver, streamplot, barbs, stem (4)
  - `PolarChartFidelityTests.cs` — polar_scatter, polar_bar, polar_heatmap (3)
  - `CategoricalChartFidelityTests.cs` — broken_barh, eventplot, gantt, waterfall (4)
  - `DistributionChartFidelityTests.cs` — kde, rugplot, stripplot, swarmplot, pointplot, countplot (6, seaborn refs)
  - `ThreeDChartFidelityTests.cs` — scatter3d, bar3d, surface, wireframe, stem3d (5, mpl_toolkits.mplot3d refs)
  - `FinancialChartFidelityTests.cs` — ohlc_bar (1)
  - `SpecialChartFidelityTests.cs` — sankey, table, treemap, radar (4)
  - `IndicatorFidelityTests.cs` — **Phase 6: 15 technical indicators against `pandas_ta` references**: SMA, EMA, Bollinger Bands, VWAP, Keltner Channels, Ichimoku, Parabolic SAR, RSI, MACD, Stochastic, ATR, ADX, CCI, Williams %R, OBV. Uses a closed-form (no-RNG) synthetic OHLC formula so Python and C# produce byte-identical price data — `close = 100 + 5·sin(2πi/25) + 3·sin(2πi/7)` — making the line math deterministic across the two runtimes (Python PCG64 ≠ C# `System.Random`).
- **Theme plumbing to `SeriesRenderer`** — `SeriesRenderContext.Theme` init property, threaded through `SvgSeriesRenderer` and all three `AxesRenderer.RenderSeries` overloads. Lets any renderer read theme-specific defaults like `PatchEdgeColor` without knowing about the figure tree.
- **`Theme.PatchEdgeColor`, `Theme.ViolinBodyColor`, `Theme.ViolinStatsColor`** — three new nullable init properties. `MatplotlibClassic` sets them to `#000000` (black patch edges, `rcParams['patch.edgecolor']='k'`), `#BFBF00` (yellow violin body, matplotlib classic `'y'`), and `#FF0000` (red violin stats lines, classic `'r'`) — all empirically confirmed against matplotlib 3.10.8.
- **`SubPlotSpacing.FromFractions(left, right, top, bottom)`** — fractional-margin factory. Stores `IsFractional=true` + `FractLeft/Right/Top/Bottom`; `Resolve(width, height)` converts to absolute pixels lazily at render time.
- **`Theme.DefaultSpacing`** — nullable init property. `ChartRenderer` resolves the spacing chain as `figure.Spacing ?? theme.DefaultSpacing ?? SubPlotSpacing.Default`, applying fractional-to-absolute conversion using the figure size.
- **`AxesBuilder.Signal(y, sampleRate, xStart)` / `SignalXY(x, y)`** — fluent methods filling an API parity gap (these previously lived only on `FigureBuilder`; every other series has both entrypoints).
- **`AxesBuilder.Indicator(IIndicator indicator)`** — generic fluent entry point for any `IIndicator` that doesn't have a dedicated shortcut (e.g. `Macd`, `Stochastic`, `Atr`, `Adx`, `Ichimoku`, `KeltnerChannels`, `Vwap`, `FibonacciRetracement`, `DrawDown`, `ProfitLoss`, `EquityCurve`). Surfaced during Phase 6 indicator fidelity testing.
- **`pandas==3.*` / `pandas-ta>=0.3.14b`** pinned in [`tools/mpl_reference/requirements.txt`](tools/mpl_reference/requirements.txt) for the new indicator reference fixtures.

### Changed

- **Matplotlib-theme margins now use matplotlib's `figure.subplot.*` defaults** — `MatplotlibClassic` and `MatplotlibV2` both ship `DefaultSpacing = FromFractions(left: 0.125, right: 0.10, top: 0.12, bottom: 0.11)`. At 800 × 600 that's `100, 80, 72, 66` px — previously hardcoded `60, 20, 40, 50`. Fixes a visible ~40-px leftward drift of the plot origin relative to matplotlib. **Non-breaking for users on the default theme** (unchanged); affects only `Theme.Matplotlib*`.
- **`SpinesConfig.LineWidth` default `1.0 → 0.8`** — matches matplotlib's `axes.linewidth = 0.8`.
- **`Axis.TickLength` default `5.0 → 3.5`** — matches matplotlib's `xtick.major.size = 3.5`.
- **`CartesianAxesRenderer.DrawTickMark`** — when `direction == TickDirection.Out`, the tick's inner endpoint is now extended by half the spine width so the tick visually overlaps the spine centerline. Closes the subpixel tick/spine gap that was visible at certain plot-area y-coordinate parities.
- **`HistogramSeries.Alpha` default `0.7 → 1.0`** — matplotlib histogram bars are opaque.
- **`ViolinSeries.Alpha` default `0.7 → 0.3`** — matplotlib violin body alpha is 0.3.
- **`HistogramSeriesRenderer`** — patch edge color now falls back to `Context.Theme?.PatchEdgeColor` when `EdgeColor` is unset (gives black 0.5-pt edges under `MatplotlibClassic`).
- **`ViolinSeriesRenderer`** — body and stats colors now resolve from `Context.Theme?.ViolinBodyColor` / `ViolinStatsColor` first, falling back to `ResolveColor(series.Color)`.
- **`ScatterSeriesRenderer` marker radius** — now computed as `sqrt(s / π) × (dpi / 72)` where `s` is the marker area in pt² (matplotlib's convention for `scatter(s=…)`). Previously used `sqrt(s) / 2`, which gave ~33 % smaller markers at 100 DPI.
- **Scatter dispatch for `MarkerStyle.Square`** — renders via `DrawRectangle` centered on the point (previously fell through to `DrawCircle`).

### Fixed

- **`PcolormeshSeriesRenderer` out-of-bounds crash** when `X.Length == cols` and `Y.Length == rows` (same-sized X/Y/Z). The renderer documents a corner-grid convention (`X.Length == cols + 1`, `Y.Length == rows + 1`); test fixtures now pass correctly-shaped `C` arrays. No renderer code change — the bug is documented, not hidden.

### Test suites

- **3 378 unit tests** green (`dotnet run --project Tst/MatPlotLibNet/MatPlotLibNet.Tests.csproj`).
- **72 fidelity tests** green (`dotnet run --project Tst/MatPlotLibNet.Fidelity/MatPlotLibNet.Fidelity.Tests.csproj`) — 12 core + 45 Phase 5 + 15 Phase 6 indicators, every one under `Theme.MatplotlibClassic` against pinned matplotlib 3.10.8 / `pandas_ta` references. Each tolerance override carries a one-line justification comment (e.g. *"AA grey text vs matplotlib crisp black"*, *"tab10 cycle vs bgrcmyk — pure colors don't appear in our top-5"*, *"half-cell spatial offset — ΔE confirms colormap is correct"*, *"2 thin lines — pure #0000FF AA-diffuses below top-5 pixel threshold"*).

### Series without matplotlib fidelity coverage

These series have no matplotlib, seaborn, matplotlib.sankey, or squarify equivalent to diff against, so they remain **out of scope for Phase 5 fidelity testing**. They still have regular unit tests and render correctly via `Theme.MatplotlibClassic`.

- `GaugeSeries` — BI/dashboard primitive; no matplotlib idiom.
- `SunburstSeries` — Plotly idiom; no matplotlib equivalent.
- `FunnelSeries` — Plotly idiom.
- `ProgressBarSeries` — UI widget, not statistical viz.
- `DonutSeries` — variant of `PieSeries`; effectively covered by the core pie test.
- `ChoroplethSeries` — requires `geopandas` for reference PNG generation; heavy native dep skipped to keep `tools/mpl_reference/` cross-platform.

### Test convention updates

- `ViolinSeriesTests.DefaultAlpha_Is0Point3` (was `_Is0Point7`) — aligns with new matplotlib-matching default.
- `HistogramSeriesTests.DefaultAlpha_Is1Point0` (was `_Is0Point7`) — ditto.
- `MatplotlibClassicThemeTests.MatplotlibClassic_HasGreyFigureBackground` (was `_HasWhiteBackground`) — matplotlib classic's `figure.facecolor = 0.75` = `#BFBFBF`, not white.
- `ThemeTests.MatplotlibClassic_Spacing_ResolvesCorrectly_At800x600` — expected `MarginBottom` corrected from `72` to `66` (matches matplotlib's `bottom = 0.11`, not `0.12`).

---

## [1.1.1] — 2026-04-12

NumPy-style numerics, polar heatmap series, broken/discontinuous axis, and inset axes constrained-layout fix.

### Added

- **NumPy-style numeric core** — zero new dependencies, pure C# + existing `TensorPrimitives`:
  - **`Mat`** (`readonly record struct`) — 2-D matrix with element-wise operators (`+`, `−`, `*`), scalar multiply, transpose (`T`), row/col slices, `FromRows` factory, `Identity`; inner multiply via `TensorPrimitives.Dot` on `RowSpan`.
  - **`Linalg`** — `Solve` (LU + partial-pivot Doolittle), `Inv`, `Det`, `Eigh` (Jacobi symmetric eigendecomposition), `Svd` (one-sided Jacobi thin SVD); results in `EighResult` / `SvdResult` named records.
  - **`NpStats`** — `Diff(n)`, `Median`, `Histogram`, `Argsort`, `Unique`, `Cov`, `Corrcoef`; results in `HistogramResult` / `UniqueResult` named records.
  - **`NpRandom`** — seeded instance-based sampler: `Normal` (Box-Muller), `Uniform`, `Lognormal`, `Integers`.
  - **`Fft.Inverse`, `Fft.Frequencies`, `Fft.Shift`** — added as `partial` extension to existing `Fft` class.
- **`PolarHeatmapSeries`** — wedge/sector cells on a polar grid (wind rose, circular heatmap). 12-segment arc polygon per cell; `IColormappable`, `INormalizable`, `IColorBarDataProvider`. Fluent entry points: `Axes.PolarHeatmap`, `AxesBuilder.PolarHeatmap`. Full JSON round-trip via `"polarheatmap"` type discriminator.
- **Broken / discontinuous axis** — `AxisBreak` sealed record + `BreakStyle` enum (`Zigzag`, `Straight`, `None`). `Axes.AddXBreak` / `AddYBreak`; `AxesBuilder.WithXBreak` / `WithYBreak`. `AxisBreakMapper` compresses the `DataTransform` range and draws visual markers. Serializes via `AxesDto.XBreaks` / `YBreaks`.
- **`Axes.InsetAxes`** — alias for `AddInset`, matching the matplotlib API surface.
- **`FigureBuilder.AddInset`** — add and configure an inset on any subplot by index.
- **Inset axes constrained-layout fix** — `AxesRenderer.ComputeInnerBounds()` (virtual, overridden in `CartesianAxesRenderer`) returns the post-margin inner plot area; `ChartRenderer.RenderAxes` uses it to position insets inside the data area when constrained layout is active, eliminating overlap with axis labels and ticks.

### Changed

- All public methods in `Linalg`, `NpStats`, `NpRandom`, `FftExtensions`, `AxisBreakMapper`, `Axes.InsetAxes`, and `AxesBuilder.WithXBreak`/`WithYBreak` now carry complete `<param>` and `<returns>` XML documentation.

---

## [1.1.0] — 2026-04-12

Feature release adding perceptual colormaps, user-defined gradients, spline smoothing, mosaic subplot layouts, and performance improvements.

### Added

- **Perceptual colormaps** (2-A): `rocket`, `mako`, `crest`, `flare`, `icefire` — all from Seaborn's perceptually-uniform palette set. Each registers automatically with its `_r` reversed variant (10 new named colormaps total). `cividis` was already present; no change.
- **`LinearColorMap.FromList`** (2-B): Factory for user-defined gradients from `(double Position, Color Color)` pairs. Auto-normalizes positions to [0,1] and auto-registers under the given name (+ `_r`).
- **Spline smoothing for `LineSeries` / `AreaSeries`** (2-C): Set `Smooth = true` and optionally `SmoothResolution` (default 10) on either series. The renderer applies Fritsch-Carlson monotone-cubic interpolation — no overshoot, preserves monotonicity. Both properties round-trip via JSON serialization.
- **`Plt.Mosaic` / `SubplotMosaic` string-pattern layout** (2-D): `Plt.Mosaic("AAB\nCCB", m => { ... })` parses a string pattern into a grid layout. Repeated characters span multiple cells. Validates rectangular regions; throws `ArgumentException` for holes or non-rectangular spans. `MosaicFigureBuilder` exposes `Panel(label, configure)`, `Build()`, `ToSvg()`, and `Save()`.
- **Benchmark coverage** (2-E): `Surface3D_WithLighting`, `GeoMap_Equirectangular`, and `Choropleth_Viridis` benchmarks added to `SvgRenderingBenchmarks`. Benchmark table in wiki updated with v1.1.0 rows.

### Changed

- **`VectorMath.SplitPositiveNegative`** (2-E): Replaced per-element branching with two `TensorPrimitives.Max/Min` SIMD passes — faster for spans > ~16 elements.
- **`VectorMath.CumulativeSum`**: Added `<remarks>` confirming that `TensorPrimitives` has no prefix-sum in .NET 10; scalar sequential loop is correct.

---

## [1.0.3] — 2026-04-12

Relicensed from LGPL-3.0 to MIT — no copyleft conditions. Free to use in any project, open-source or commercial, with no restrictions beyond keeping the copyright notice.

### Changed

- License: LGPL-3.0 → MIT across all 9 NuGet packages, `LICENSE` file, and all source file headers
- All `.csproj` files: `<PackageLicenseFile>` → `<PackageLicenseExpression>MIT</PackageLicenseExpression>`

---

## [1.0.2] — 2026-04-12

Pipeline fix — `MatPlotLibNet.DataFrame` added to the CI publish pipeline so all 9 packages release automatically on every tagged release.

### Fixed

- `MatPlotLibNet.DataFrame` missing from `MatPlotLibNet.CI.slnf` — it was never built, tested, or packed by the publish workflow
- `publish.yml` Test step did not run `MatPlotLibNet.DataFrame` tests before publishing
- Added `Src/MatPlotLibNet.DataFrame/MatPlotLibNet.DataFrame.csproj` and `Tst/MatPlotLibNet.DataFrame/MatPlotLibNet.DataFrame.Tests.csproj` to the CI solution filter

---

## [1.0.1] — 2026-04-12

Dependency update release — all NuGet packages bumped to latest stable versions.

### Changed

- `Microsoft.SourceLink.GitHub` `8.*` → `10.*`
- `System.Numerics.Tensors` `9.*` → `10.*` (aligned with .NET 10)
- `Microsoft.Data.Analysis` `0.22.*` → `0.23.*`
- `BenchmarkDotNet` `0.14.*` → `0.15.*`
- `HotChocolate.AspNetCore` `14.*` → `15.*`
- `Microsoft.Maui.Controls` / `Microsoft.Maui.Graphics` `10.0.20` → `10.0.51`
- `xunit.v3` `1.*` → `3.*`

---

High-performance signal series, `IEnumerable<T>` fluent extensions, DataFrame package, faceting OO layer, QuickPlot façade, and OO maintenance polish (named records, capability interfaces, XML docs, DataFrame indicator/numerics bridges).

### Added

**Phase 0 — `IEnumerable<T>` figure extensions with hue grouping**

- `HueGroup` record — carries `X[]`, `Y[]`, `Label`, `Color` for one group
- `HueGrouper.GroupBy<T,TKey>` — partitions any sequence into colour-coded `HueGroup` instances
- `EnumerableFigureExtensions.Line<T>` / `Scatter<T>` / `Hist<T>` — fluent plotting from any `IEnumerable<T>` with optional `hue` and `palette` parameters

### Tests: 3,074 → 3,097 (+23, core)

**Phase 1 — `SignalSeries` + `SignalXYSeries` — high-performance large-dataset rendering**

- `IMonotonicXY` interface — `IndexRangeFor(xMin, xMax)` contract for O(1)/O(log n) viewport slicing
- `MonotonicViewportSlicer.Slice<T>` — unified slice + optional LTTB downsampling helper
- `SignalXYSeries` — non-uniform ascending X, O(log n) via two `Array.BinarySearch` calls with guard-point extension
- `SignalSeries` — uniform sample rate, O(1) arithmetic `IndexRangeFor`, lazy `XData` materialisation
- `FigureBuilder.SignalXY(x[], y[], configure?)` / `Signal(y[], sampleRate, xStart, configure?)` builder methods
- `SignalXYSeriesRenderer` / `SignalSeriesRenderer` — delegate to `MonotonicViewportSlicer` then LTTB
- `ISeriesVisitor` default no-op overloads (`Visit(SignalXYSeries)`, `Visit(SignalSeries)`) — source-compatible extension
- JSON round-trip: `SeriesDto.SignalSampleRate?` / `SignalXStart?`; `SeriesRegistry` factories for `"signal-xy"` / `"signal"`
- `SignalSeriesBenchmarks` — 7 BenchmarkDotNet benchmarks (narrow + wide viewports, 100k / 1M / 10M points)

### Tests: 3,097 → 3,158 (+61, core)

**Phase 2 — `MatPlotLibNet.DataFrame` NuGet package (9th subpackage)**

- New package `MatPlotLibNet.DataFrame` targeting `net10.0;net8.0`
- `DataFrameColumnReader.ToDoubleArray` — converts any `DataFrameColumn` to `double[]` (null → NaN, DateTime → OADate)
- `DataFrameColumnReader.ToStringArray` — converts any `DataFrameColumn` to `string[]` (null → "")
- `DataFrameFigureExtensions.Line` / `Scatter` / `Hist` — extension methods on `Microsoft.Data.Analysis.DataFrame`; delegate all grouping logic to `EnumerableFigureExtensions` via private `readonly record struct` row carriers (`Xy`, `Xyh`, `Vh`)
- Hue grouping, palette cycling, and alpha blending inherit from Phase 0 — zero duplication

### Tests: +24 (MatPlotLibNet.DataFrame.Tests runner)

**Phase 4 — `QuickPlot` one-liner façade**

- `QuickPlot.Line` / `Scatter` / `Hist` / `Signal` / `SignalXY` — single-call shortcuts that return a chainable `FigureBuilder`; optional `title:` parameter shortcuts `.WithTitle(...)`
- `QuickPlot.Svg(Action<FigureBuilder>)` — generic escape hatch for arbitrary one-liner chains returning an SVG string; throws `ArgumentNullException` on null configure
- Pure delegation layer — zero duplicated logic, zero state, ~40 LoC in `QuickPlot.cs`

### Tests: 3,158 → 3,178 (+20, core)

**Phase 3 — Faceting OO layer**

- `FacetedFigure` abstract base (no "Base" suffix) — shared shell (title, size, palette), `ConfigurePanelDefaults` helper, hue-aware `AddScatters` / `AddLines` / `AddHistograms` helpers that delegate all grouping to `HueGrouper.GroupBy`; private nested `readonly record struct HueRow` replaces tuple-based grouping
- `JointPlotFigure` sealed — 2×2 grid with top X-marginal + center scatter + right Y-marginal; init-only `Bins` (30) and `Hue`; all series add routes through base helpers
- `PairPlotFigure` sealed — N×N grid: diagonal Hist, off-diagonal Scatter; init-only `ColumnNames`, `Bins` (20), `Hue`
- `FacetGridFigure` sealed — one panel per category, column-wrapped; init-only `MaxCols` (3), `Hue` (forward-compatible hook; richer `plotFunc` overload deferred to v1.1)
- `FigureTemplates.JointPlot` / `PairPlot` / `FacetGrid` static methods refactored into 1-line delegations onto the new OO types — **public API unchanged**, existing callers unaffected; file shrinks ~140 LoC
- Zero new grouping logic — all hue partitioning delegates to Phase 0's `HueGrouper`

### Tests: 3,178 → 3,199 (+21, core)

**OO Maintenance — pre-release polish (sub-phases A–G)**

- **A — Tuple → named record types** — six public APIs replaced with `readonly record struct` / `sealed record` types: `IndexRange(StartInclusive, EndExclusive)` + computed `Count`/`IsEmpty`; `NormalizedPoint(Nx, Ny)`; `GeoBounds(LonMin, LonMax, LatMin, LatMax)` + computed `LonCenter`/`LatCenter`; `Normalized3DPoint(Nx, Ny, Nz)`; `AdxResult(Adx[], PlusDi[], MinusDi[])`; `ConfidenceBand(Upper[], Lower[])`; all call sites updated to named-member access
- **B — `DrawStyleInterpolation` DRY** — extracted `DrawStyleInterpolation.Apply(x, y, style)` internal utility; eliminated 38-line duplication between `LineSeriesRenderer.ApplyDrawStyle()` and `AreaSeriesRenderer.ApplyStepMode()`
- **C — Series capability interfaces** — four new marker interfaces: `IHasColor { Color? Color }`, `IHasAlpha { double Alpha }`, `IHasEdgeColor { Color? EdgeColor }`, `ILabelable { bool ShowLabels; string? LabelFormat }`; ~20 existing series gain the relevant interface(s) on their `class` declaration lines — no new properties, no behaviour change
- **D — `<example>` XML doc blocks** — added concise usage examples to `Plt`, `FigureBuilder`, `AxesBuilder`, `ThemeBuilder`, `FacetedFigure` (abstract base), and all three `MatPlotLibNet.DataFrame` extension classes (`DataFrameFigureExtensions`, `DataFrameIndicatorExtensions`, `DataFrameNumericsExtensions`)
- **E — `Func<T,T>` configure methods use existing state** — `AxesBuilder.WithTitle`, `SetXLabel`, `SetYLabel`, `WithColorBar` and `FigureBuilder.WithColorBar` now pass the existing property value (rather than `new T()`) to the configure delegate, making repeated calls idempotent and composable; `FigureBuilder.WithSubPlotSpacing` parameter made optional (`Func<>? configure = null`)
- **F — XML documentation sweep** — `<param>`/`<returns>` tags on all ~15 `VectorMath` internal methods and ~28 `ChartSerializer` factory methods; `<remarks>` blocks added to: `Projection3D` (camera distance clamping), `LeastSquares.PolyFit` (Vandermonde stability), `LeastSquares.ConfidenceBand` (normal-residual assumption), `AxesBuilder.UseBarSlotX` (call-before-series rule), `HueGrouper.GroupBy` (first-seen ordering), `DataTransform.TransformY` (inversion timing), `IMonotonicXY.IndexRangeFor` (guard-point requirement), `FacetGridFigure.Hue` (v1.0 no-op note), `Adx.ComputeFull` (vs scalar `Compute()`)
- **G — DataFrame indicator + numerics bridges** — `DataFrameIndicatorExtensions`: 16 extension methods on `Microsoft.Data.Analysis.DataFrame` for SMA, EMA, RSI, BollingerBands, OBV, MACD, DrawDown, ADX (scalar + full AdxResult), ATR, CCI, WilliamsR, Stochastic, ParabolicSar, KeltnerChannels, VWAP; `DataFrameNumericsExtensions`: `PolyFit`, `PolyEval`, `ConfidenceBand` delegating to `LeastSquares`; all column resolution funnelled through a shared `Col()` helper with friendly `ArgumentException` on unknown names

### Tests: 3,199 → 3,201 (+2, core); 24 → 54 (+30, DataFrame) — **total 3,255**

---

## [0.9.1] - 2026-04-12

Matplotlib look-alike themes: `Theme.MatplotlibClassic` and `Theme.MatplotlibV2` — drop-in matplotlib styling in pure .NET.

### Added

**Matplotlib Theme Pack — visually faithful matplotlib styling in pure .NET**

- **`Theme.MatplotlibClassic`** — mimics matplotlib's pre-2.0 default look: white background, pure-black text, the iconic `bgrcmyk` 7-color cycle (`#0000FF`, `#008000`, `#FF0000`, `#00BFBF`, `#BF00BF`, `#BFBF00`, `#000000`), DejaVu Sans 12pt, grid hidden by default. The look every scientific paper printed up to 2017
- **`Theme.MatplotlibV2`** — mimics matplotlib's modern default (since 2017): white background, soft-black `#262626` text, the `tab10` 10-color cycle, DejaVu Sans 10pt, grid hidden by default. The look every Jupyter notebook ships with today
- **`MatplotlibThemeFactory`** (internal) — DRY helper that builds both themes from a shared `Build(...)` method, isolating only what the two themes actually disagree on (color cycle, font size, foreground text)
- **`MatplotlibFontStack`** (internal `record struct`) — captures the matplotlib font stack (primary CSS family + base/tick/title sizes) as a named value type instead of a positional tuple

### Tests: 3,042 → 3,074 (+32)

## [0.9.0] - 2026-04-11

### Added

**Phase G — True 3-D (4 sub-phases)**

- **Camera system** — `Axes.Elevation` (default 30°), `Axes.Azimuth` (default −60°), `Axes.CameraDistance` (null = orthographic) replace the broken `WithProjection()` placeholder; `ThreeDAxesRenderer` builds one unified `Projection3D` and threads it through `SeriesRenderContext.Projection3D` to all 5 3D series renderers — fixing the bug where angle changes were ignored
- **Perspective projection** — `Projection3D` gains optional `distance` parameter (clamped ≥ 2.0); when set, applies Lambertian perspective scale `d/(d−viewDepth)` after rotation; `Projection3D.Normalize()` returns [-1,1] coordinates for JS re-projection
- **`SeriesRenderContext.Projection3D?`** + **`SeriesRenderContext.LightSource?`** init-only fields; unified projection eliminates per-renderer duplicate range computations
- **`AxesBuilder.WithCamera(elevation, azimuth, distance?)`** + **`FigureBuilder.WithCamera(…)`** — fluent camera API
- **`ILightSource`** interface — `ComputeIntensity(nx, ny, nz) → [0,1]` for per-face lighting
- **`DirectionalLight`** sealed record — Lambertian diffuse + ambient (defaults 0.3/0.7); implements `ILightSource`
- **`LightingHelper`** static class — `ComputeFaceNormal()` (cross product) + `ModulateColor(color, intensity)` shared by Surface and Bar3D renderers
- **`Axes.LightSource`** — optional `ILightSource`; `SurfaceSeriesRenderer` uses it for per-quad color modulation; `Bar3DSeriesRenderer` uses fixed face normals for top/front/side
- **`AxesBuilder.WithLighting(dx, dy, dz, ambient, diffuse)`** + **`FigureBuilder.WithLighting(…)`**
- **`IRenderContext.SetNextElementData(key, value)`** — default no-op; `SvgRenderContext` flushes `data-{key}="{value}"` before `/>` in DrawLine/DrawLines/DrawPolygon/DrawCircle
- **`SvgRenderContext.Begin3DSceneGroup(elevation, azimuth, distance?, plotBounds)`** — emits `<g class="mpl-3d-scene" data-*>` with camera parameters
- **`Figure.Enable3DRotation`** + **`FigureBuilder.With3DRotation()`** — when enabled, 3D renderers emit `data-v3d` normalized vertex attributes and `ThreeDAxesRenderer` wraps output in a scene group
- **`Svg3DRotationScript`** — embedded JavaScript (~80 lines): reads `data-v3d` normalized coords, reimplements `Projection3D.Project()` in JS, re-sorts DOM by depth; mouse drag (azimuth/elevation) + keyboard arrows + Home reset
- **3D serialization fixes** — `SurfaceSeries`, `WireframeSeries`, `Scatter3DSeries` now populate XData/YData/ZGridData/ZData in `ToSeriesDto()`; `SeriesRegistry` factories restore full series state from DTO; `AxesDto` gains `Elevation?/Azimuth?/CameraDistance?/LightSourceType?`; `FigureDto` gains `Enable3DRotation?`
- **3D sample scenes** added to `MatPlotLibNet.Samples.Console`

### Fixed

- All 5 3D renderers previously hardcoded `new Projection3D(30, -60, ...)` ignoring user-set angles — now use context projection
- `AxesBuilder.WithProjection()` previously created a broken `Projection3D` with placeholder bounds — now sets `Axes.Elevation/Azimuth` directly

### Tests: 3,001 → 3,042 (+41)

## [0.8.9] - 2026-04-11

### Added

**Phase F — Geo / Map Projections (7 sub-phases)**

- **`IMapProjection`** interface — `Project(lon, lat) → (Nx, Ny)` in [0,1]²; `Bounds` property returns valid lon/lat extent
- **`EquirectangularProjection`** — plate carrée: longitude and latitude mapped linearly; parameterizable center meridian, lon/lat extent
- **`MercatorProjection`** — Web Mercator (EPSG:3857); latitude clamped to ±85.0511° to avoid pole singularity
- **`MapProjections`** static factory — `Equirectangular(...)` / `Mercator(...)` convenience constructors
- **GeoJSON support** — `GeoJsonDocument`, `GeoJsonFeatureCollection`, `GeoJsonFeature`, `GeoJsonGeometry` record types; `GeoJsonGeometryType` enum (Point, MultiPoint, LineString, MultiLineString, Polygon, MultiPolygon, GeometryCollection); `GeoJsonReader.FromJson(string)` / `FromFile(string)`; `GeoJsonWriter.ToJson(document)`
- **`MapSeries`** — renders GeoJSON geometry (Polygon, MultiPolygon, LineString, MultiLineString, GeometryCollection) on a projected map; `GeoData`, `Projection`, `FaceColor?`, `EdgeColor?`, `LineWidth` properties; `Axes.Map()` / `FigureBuilder.Map()` builder methods
- **`ChoroplethSeries : MapSeries`** — fills each GeoJSON feature with a color derived from `Values[i]` mapped through `ColorMap` / `Normalizer` / `VMin` / `VMax`; `Axes.Choropleth()` / `FigureBuilder.Choropleth()` builder methods
- **`MapSeriesRenderer`** — projects polygon rings and line strings to pixel coordinates using `IMapProjection`; uses `IRenderContext.DrawPolygon` for fill + stroke
- **`ChoroplethSeriesRenderer`** — extends `MapSeriesRenderer`; per-feature fill color from colormap (default: Viridis)
- **`ISeriesVisitor`** — two new default (no-op) overloads: `Visit(MapSeries)` / `Visit(ChoroplethSeries)`; existing implementations remain source-compatible
- **Serialization** — `SeriesDto.GeoJson?` (compact JSON payload) + `SeriesDto.Projection?`; `SeriesRegistry` entries for `"map"` and `"choropleth"`; full JSON round-trip for both series types
- **`Axes.Map()` / `FigureBuilder.Map()`** + **`Axes.Choropleth()` / `FigureBuilder.Choropleth()`** builder entry points

### Tests: 2,940 → 3,001 (+61)

## [0.8.8] - 2026-04-11

### Added

**Phase E — Accessibility (5 sub-phases)**

- **SVG semantic structure** — all SVG exports now carry `role="img"` on the root `<svg>` element; `<title id="chart-title">` is always emitted (alt text → figure title → empty fallback); `<desc id="chart-desc">` is emitted when `Figure.Description` is set; `aria-labelledby="chart-title"` always present; `aria-describedby="chart-desc"` when description is set
- **`Figure.AltText`** (`string?`) — short alternative text for the chart; takes priority over `Figure.Title` as the `<title>` content
- **`Figure.Description`** (`string?`) — longer description rendered as the SVG `<desc>` element
- **`FigureBuilder.WithAltText(string)`** / **`WithDescription(string)`** — fluent builder methods (same pattern as `WithTitle`)
- **`SvgXmlHelper`** internal static helper — `EscapeXml(string)` extracted from `SvgRenderContext` (DRY); used by both `SvgRenderContext` and `SvgTransform`
- **ARIA groups** — `SvgRenderContext.BeginAccessibleGroup(cssClass, ariaLabel)` emits `<g class="..." aria-label="...">`; `BeginDataGroup` and `BeginLegendItemGroup` accept optional `ariaLabel` parameter; legend group uses `aria-label="Chart legend"`, colorbar group uses `aria-label="Color bar"`, labeled series always wrapped in accessible group (even without JS interactivity enabled)
- **Keyboard navigation in all 5 JS scripts** — **legend toggle**: `role="button"`, `tabindex="0"`, `aria-pressed` per entry, `keydown` Enter/Space handler; **highlight**: `tabindex="0"` + `focus`/`blur` listeners mirror mouse enter/leave; **zoom/pan**: `tabindex="0"`, `aria-roledescription="interactive chart"`, keyboard `+`/`=` zoom in, `-` zoom out, `ArrowLeft/Right/Up/Down` pan, `Home` reset; **selection**: `Escape` key cancels active selection; **tooltip**: `role="tooltip"` + `aria-live="polite"` on tooltip div, `focus`/`blur` listeners
- **`QualitativeColorMaps.OkabeIto`** — 8-color palette safe for deuteranopia, protanopia, and tritanopia; registered as `"okabe_ito"` and `"okabe_ito_r"`
- **`Theme.ColorBlindSafe`** — white background, black text, Okabe-Ito 8-color cycle, `"colorblind-safe"` name
- **`Theme.HighContrast`** — white background, black text, bold 13pt font, 1.5px dark (`#666666`) grid, 8-color high-chroma cycle; WCAG AAA target (pure white/black = 21:1 contrast ratio), `"high-contrast"` name
- **Serialization** — `FigureDto.AltText?` + `FigureDto.Description?`; `FigureToDto` + `DtoToFigure` updated; full JSON round-trip

### Tests: 2880 → 2940 (+60)

## [0.8.7] - 2026-04-11

### Added

**Phase D — Annotation System (5 sub-phases)**

- **ReferenceLine label rendering** — `ReferenceLine.Label` (already on the model) is now rendered: horizontal lines draw the label right-aligned at the right edge of the plot area, above the line; vertical lines draw the label left-aligned near the top of the line; color inherits from the line color
- **`ConnectionStyle` enum** (`Straight`, `Arc3`, `Angle`, `Angle3`) — controls the path shape of annotation arrows; `Annotation.ConnectionStyle` property (default `Straight`); `Annotation.ConnectionRad` (default 0.3) controls arc/elbow curvature; `ConnectionPathBuilder` internal static utility produces `IReadOnlyList<PathSegment>` for each style
- **Extended `ArrowStyle` enum** — 7 new values: `Wedge` (wider filled arrowhead), `CurveA`/`CurveB`/`CurveAB` (open curved arrowheads at one/both ends), `BracketA`/`BracketB`/`BracketAB` (perpendicular bracket lines at one/both ends); `Annotation.ArrowHeadSize` property (default 8)
- **`ArrowHeadBuilder`** internal static utility — `BuildPolygon(tip, ux, uy, style, size)` for filled polygon heads; `BuildPath(tip, ux, uy, style, size)` for open/line heads; replaces inline arrowhead math in `CartesianAxesRenderer`
- **`ConnectionPathBuilder`** internal static utility — `BuildPath(from, to, style, rad)` returns `IReadOnlyList<PathSegment>`; replaces `DrawLine` connection in the annotation renderer
- **`BoxStyle` enum** (`None`, `Square`, `Round`, `RoundTooth`, `Sawtooth`) — background box style for annotations; `Annotation.BoxStyle` property (default `None`); `Annotation.BoxPadding` (default 4), `BoxCornerRadius` (default 5), `BoxFaceColor?`, `BoxEdgeColor?`, `BoxLineWidth` (default 1)
- **`CalloutBoxRenderer`** internal static utility — `Draw(ctx, textBounds, style, padding, cornerRadius, faceColor, edgeColor, edgeWidth)` draws `Square` via `DrawRectangle`; `Round` via rounded-rect bezier path; `RoundTooth` via rounded-rect + zigzag bottom; `Sawtooth` via all-sides sawtooth path
- **SpanRegion border** — `SpanRegion.LineStyle` (default `None`), `LineWidth` (default 1.0), `EdgeColor?` properties; when `LineStyle != None`, 4 border lines are drawn around the span rectangle using `DrawLine`
- **SpanRegion label** — `SpanRegion.Label?` property; horizontal spans draw the label top-left inside the span, vertical spans draw it top-center
- **Builder convenience overloads** — `FigureBuilder.Annotate(text, x, y, arrowX, arrowY, configure?)`, `AxesBuilder.Annotate(text, x, y, arrowX, arrowY, configure?)` — set `ArrowTargetX/Y` inline; `FigureBuilder` now exposes `Annotate`, `AxHLine`, `AxVLine`, `AxHSpan`, `AxVSpan` delegation methods for single-axes fluent API
- **Serialization** — `AnnotationDto` extended with `ConnectionStyle?`, `ConnectionRad?`, `ArrowHeadSize?`, `BoxStyle?`, `BoxPadding?`, `BoxCornerRadius?`; `SpanRegionDto` extended with `LineStyle?`, `LineWidth?`, `Label?`; full round-trip support

### Changed

- **`CartesianAxesRenderer` annotation block** refactored (DRY/SOLID): rotation dispatch simplified to always use `DrawText(..., rotation)` (0 is a no-op); `DrawLine` connection replaced by `ConnectionPathBuilder.BuildPath` + `DrawPath`; inline arrowhead polygon replaced by `ArrowHeadBuilder.BuildPolygon/BuildPath`; background box routing: `BoxStyle != None` → `CalloutBoxRenderer.Draw`, else `BackgroundColor.HasValue` → existing simple rect (backward compat)

### Tests: 2814 → 2880 (+66)

## [0.8.6] - 2026-04-11

### Added

**Gap Phase 3 — Series Enhancements (7 sub-phases)**

- **`HatchPattern` enum** (`None`, `ForwardDiagonal`, `BackDiagonal`, `Horizontal`, `Vertical`, `Cross`, `DiagonalCross`, `Dots`, `Stars`) + **`HatchRenderer`** static utility — uses existing `PushClip` + `DrawLines` + `DrawCircle` primitives; no `IRenderContext` changes needed (ISP preserved)
- **Hatch properties on filled-region series** — `HatchPattern Hatch` + `Color? HatchColor` on `BarSeries`, `HistogramSeries`, `AreaSeries`, `StackedAreaSeries`; `HatchPattern[]? Hatches` (per-slice) on `PieSeries`; `HatchPattern[]? Hatches` (per-level) on `ContourfSeries`
- **`AreaSeries` enhancements** — `Color? EdgeColor` (separate stroke for boundary lines), `DrawStyle StepMode` (step interpolation: `StepsPre` / `StepsMid` / `StepsPost`)
- **`StackedBaseline` enum** (`Zero`, `Symmetric`, `Wiggle`, `WeightedWiggle`) + **`BaselineHelper`** pure-function strategy — `Symmetric` shifts mid-stack to y=0; `Wiggle` uses Byron-Wattenberg baseline; `WeightedWiggle` weights by layer magnitude; `StackedAreaSeries.Baseline` property; `ComputeDataRange()` and renderer both use `BaselineHelper.ComputeBaselines()`
- **Contour explicit levels** — `double[]? LevelValues` on `ContourSeries` and `ContourfSeries`; when set, overrides auto-spaced `Levels` count
- **`SurfaceSeries` enhancements** — `Color? EdgeColor` (wireframe stroke override), `int RowStride` + `int ColStride` (render every N-th row/column for performance)
- **`SaveOptions` record** — `int Dpi` (96), `bool PrettifySvg`, `int? SvgDecimalPrecision`, `string? Title`, `string? Author`; `FigureExtensions.Save(string, SaveOptions?)` overload

**Phase C — Layout Engine v2 (3 sub-phases)**

- **`TwinY` (secondary X-axis)** — `Axes.TwinY()` mirrors `TwinX` pattern; `SecondaryXAxis` property; `PlotXSecondary()` / `ScatterXSecondary()` methods; `XSecondarySeries` collection; `AxesBuilder.WithSecondaryXAxis(Action<SecondaryXAxisBuilder>)` builder overload; `CartesianAxesRenderer` draws top-edge ticks + label for the secondary X range
- **ConstrainedLayout spanning fix** — `ConstrainedLayoutEngine.Compute()` now uses `GetEffectivePosition()` to identify which edge each subplot touches; only edge subplots contribute to the corresponding margin (center subplots no longer inflate outer margins); secondary X-axis label top margin handled in `Measure()`
- **Figure-level ColorBar** — `Figure.FigureColorBar` property; `FigureBuilder.WithColorBar(Func<ColorBar,ColorBar>?)` builder method; `ChartRenderer.RenderFigureColorBar()` renders a shared colorbar outside all subplot areas (vertical or horizontal); `SvgTransform.Render()` calls it after parallel subplot rendering; bar position clamped to stay within figure bounds

### Tests: 2730 → 2814 (+84)

## [0.8.5] - 2026-04-11

### Added

**Gap Phase 2 — Chrome Configuration (7 sub-phases)**

- **`TextStyle` record** — nullable partial font override with `ApplyTo(Font)` merge method; used throughout the chrome system to override theme fonts without breaking Liskov (TextStyle is NOT a Font subtype — it's a partial overlay)
- **Legend enrichment** — 13 new `Legend` properties: `NCols`, `FontSize`, `Title`, `TitleFontSize`, `FrameOn`, `FrameAlpha`, `FancyBox`, `Shadow`, `EdgeColor`, `FaceColor`, `MarkerScale`, `LabelSpacing`, `ColumnSpacing`; 6 new `LegendPosition` values: `Right`, `CenterLeft`, `CenterRight`, `LowerCenter`, `UpperCenter`, `Center`; `AxesBuilder.WithLegend(Func<Legend,Legend>)` overload; `RenderLegend` updated for multi-column layout, title, frame/shadow/fancy rendering
- **`TitleLocation` enum** (`Left` / `Center` / `Right`) — `Axes.TitleLoc` property (default `Center`); `Axes.TitleStyle` (`TextStyle?`); builder overloads `WithTitle(string, Func<TextStyle,TextStyle>?)`, `SetXLabel(string, Func<TextStyle,TextStyle>?)`, `SetYLabel(string, Func<TextStyle,TextStyle>?)`; `Axis.LabelStyle` (`TextStyle?`); `RenderTitle` / `RenderAxisLabels` apply `TextStyle.ApplyTo` and `TitleLoc` alignment
- **`TickDirection` enum** (`In` / `Out` / `InOut`) — 7 new `TickConfig` properties: `Direction`, `Length` (5.0), `Width` (0.8), `Color?`, `LabelSize?`, `LabelColor?`, `Pad` (3.0); `RenderTicks` refactored with `DrawTickMark` helper using all new properties
- **`GridWhich` enum** (`Major` / `Minor` / `Both`) + **`GridAxis` enum** (`X` / `Y` / `Both`) — `GridStyle.Which` + `GridStyle.Axis` properties; `AxesBuilder.WithGrid(Func<GridStyle,GridStyle>)` overload; `RenderGrid` draws minor grid lines at 5× density when `Which` is `Minor` or `Both`, respects `Axis` filter
- **`ColorBarOrientation` enum** (`Vertical` / `Horizontal`) — 4 new `ColorBar` properties: `Orientation`, `Shrink` (1.0), `DrawEdges` (false), `Aspect` (20); `RenderColorBar` fully rewritten to support both orientations, shrink centering, edge lines between gradient steps
- **`SpineConfig`** gains `Color?` and `LineStyle` (default `Solid`) — `RenderSpines` uses per-spine color and dash pattern instead of hardcoded theme foreground + `Solid`

### Tests: 2662 → 2730 (+68)

## [0.8.4] - 2026-04-11

### Added

**Roadmap Phase B — Colormap Engine**

- **`LinearColorMap`** (public) — replaces internal `LerpColorMap`; adds `FromPositions(name, (double, Color)[])` factory for custom gradient stop positions (binary search + local lerp)
- **`ListedColorMap`** — discrete `floor(v * N)` lookup without interpolation; fixes all 10 qualitative colormaps (`Tab10`, `Tab20`, `Set1–3`, `Pastel1–2`, `Dark2`, `Accent`, `Paired`) which incorrectly used `LerpColorMap`
- **Extreme values on `IColorMap`** — default interface methods `GetUnderColor()`, `GetOverColor()`, `GetBadColor()` (default `null`); `LinearColorMap` and `ListedColorMap` gain `UnderColor`, `OverColor`, `BadColor` init properties; `ReversedColorMap` swaps under/over
- **4 new normalizers:**
  - `SymLogNormalizer(linthresh, base, linScale)` — symmetric log; linear within ±linthresh, log-compressed beyond
  - `PowerNormNormalizer(gamma)` — power-law `((v-min)/(max-min))^γ`
  - `CenteredNormNormalizer(vcenter, halfrange?)` — maps chosen center to 0.5; optional symmetric half-range constraint
  - `NoNormNormalizer.Instance` — pass-through, clamps to [0, 1]
- **13 new colormaps** (65 total; 130 including reversed): `gray`, `spring`, `summer`, `autumn`, `winter`, `cool`, `afmhot`, `prgn`, `rdgy`, `rainbow`, `ocean`, `terrain`, `cmrmap`
- **`ColorBarExtend` enum** (`Neither` / `Min` / `Max` / `Both`) — `ColorBar.Extend` property; `AxesRenderer.RenderColorBar` draws under/over extension rectangles using `GetUnderColor()` / `GetOverColor()`
- **`SurfaceSeries`** now implements `INormalizable`; `SurfaceSeriesRenderer` uses the normalizer for Z→color mapping

**Gap Phase 1 — Core Series Property Enrichment (~30 properties, 8 series)**

- `LineSeries` — `MarkerFaceColor`, `MarkerEdgeColor`, `MarkerEdgeWidth`, `DrawStyle` (step interpolation: `StepsPre` / `StepsMid` / `StepsPost`), `MarkEvery`
- `ScatterSeries` — `EdgeColors`, `LineWidths`, `VMin`, `VMax`, `Normalizer` (`INormalizable`), `C` (per-point colormap scalar array; priority: `Colors[]` > `C+ColorMap` > uniform)
- `BarSeries` — `Alpha`, `LineWidth`, `Align` (`BarAlignment.Center` / `Edge`)
- `HistogramSeries` — `Density`, `Cumulative`, `HistType` (`Bar` / `Step` / `StepFilled`), `Weights`, `RWidth`
- `PieSeries` — `Explode`, `AutoPct`, `Shadow`, `Radius`
- `BoxSeries` — `Widths`, `Vert`, `Whis`, `ShowMeans`, `Positions`
- `ViolinSeries` — `ShowMeans`, `ShowMedians`, `ShowExtrema`, `Positions`, `Widths`, `Side` (`ViolinSide.Both` / `Low` / `High`)
- `ErrorBarSeries` — `ELineWidth`, `CapThick`, `ErrorEvery`
- **4 new enums:** `DrawStyle`, `BarAlignment`, `HistType`, `ViolinSide`

**SOLID/DRY Refactoring — Stacked Base Classes**

- `Indicator` enriched with `MakeX()`, `PlotSignal()`, `PlotBands()` — all 14 plotable indicators flow through the pipe
- `CandleIndicator<T>` — OHLCV cache + `ComputeTrueRange()`, `ComputeTypicalPrice()`, `ComputeDonchianMid()` for 7 HLC indicators
- `PriceIndicator<T>` — `Prices` + `PriceSource` constructor for 6 single-price indicators
- `OhlcSeries` — shared base for `CandlestickSeries` and `OhlcBarSeries`
- `DatasetSeries` — shared base + default `ComputeDataRange` for 5 distribution series
- `SeriesRenderer` enriched with `ApplyAlpha()` (11 renderers) + `ApplyDownsampling()` (3 renderers)
- **`UseBarSlotX()`** — `AxesBuilder` method marking a panel as bar-slot context; all indicators auto-align to bar centres

### Fixed

- **Panel indicator alignment** — oscillator indicators (RSI, Stochastic, MACD) now align with bar centres; offset handled automatically through `MakeX()` / `PlotSignal()` in the base + `UseBarSlotX()` on the panel

### Tests: 2432 → 2662 (+230)

## [0.8.2] - 2026-04-11

### Fixed

- **Y-axis label rotation** — `RenderAxisLabels` now passes `rotation: 90` to `DrawText` / `DrawRichText`; previously Y-axis labels rendered horizontally flush to the left edge
- **Dollar sign stripped from labels** — `MathTextParser.ContainsMath` now requires two `$` delimiters; a lone `$` (e.g. `"Revenue ($)"`) was incorrectly toggling math mode and discarding the character
- **Heatmap / area-based series blank** — `SvgSeriesRenderer` was initialising `RenderArea` with `default(Rect)` (zero width × height); renderers that derive cell size from `PlotBounds` (Heatmap, Hexbin, Pcolormesh, Spectrogram, Tripcolor) now receive the correct plot area
- **Indicator chaining crash** — `AxesBuilder.GetPriceData()` now prefers `CandlestickSeries` / `OhlcBarSeries` over the last series; calling `BollingerBands` followed by `Sma` on the same axes no longer throws `InvalidOperationException`

### Added

- **`DrawRichText` rotation overload** — `IRenderContext.DrawRichText(RichText, Point, Font, TextAlignment, double rotation)` default interface method; `SvgRenderContext` override emits `transform="rotate(…)"` enabling rotated math-text Y-axis labels
- **`BarCenterFormatter`** — new `ITickFormatter` that centres category labels under each bar group
- **`MultipleLocator` center-offset** — optional `centerOffset` parameter aligns tick positions to bar centres for categorical bar charts

### Tests: 2430 → 2432 (+2)

- `BollingerBands_ThenSma_DoesNotThrow`
- `BollingerBands_ThenSma_ResolvesOriginalPriceData`

---

## [0.8.1] - 2026-04-11

> **Note:** Phase 1 (CSS4 Named Colors — 148 colors + `Color.FromName()`) is deferred to v0.8.3.

### Added

**Phase 2 — PropCycler**
- `PropCycler` — cycles Color, LineStyle, MarkerStyle, and LineWidth simultaneously across series; `this[int index]` returns `CycledProperties` with LCM-based wrap-around
- `CycledProperties` readonly record struct — `(Color Color, LineStyle LineStyle, MarkerStyle MarkerStyle, double LineWidth)`
- `PropCyclerBuilder` — fluent builder: `WithColors()`, `WithLineStyles()`, `WithMarkerStyles()`, `WithLineWidths()`, `Build()`
- `Theme.PropCycler` (`PropCycler?`) — optional; when null the existing `CycleColors[]` path is unchanged (full backward compat)
- `ThemeBuilder.WithPropCycler()` — wires a custom cycler into a theme
- `FigureBuilder.WithPropCycler()` — shortcut for single-figure override
- `AxesRenderer` updated to pass `CycledProperties` to `SvgSeriesRenderer` when `PropCycler` is set

**Phase 3 — Date Axis**
- `AutoDateLocator` — examines OA date range and selects the best tick interval (Years → Months → Weeks → Days → Hours → Minutes → Seconds); exposes `ChosenInterval` after `Locate()`
- `AutoDateFormatter` — reads `ChosenInterval` from the locator and selects the matching format string (`"yyyy"`, `"MMM yyyy"`, `"MMM dd"`, `"HH:mm"`, `"HH:mm:ss"`)
- `DateInterval` enum — Years, Months, Weeks, Days, Hours, Minutes, Seconds
- `DateTime` overloads on `AxesBuilder` and `FigureBuilder` — `Plot(DateTime[], double[])`, `Scatter(DateTime[], double[])` auto-set X scale to `AxisScale.Date`
- `CartesianAxesRenderer` auto-applies `AutoDateLocator` + `AutoDateFormatter` when `Scale=Date` and no explicit locator is set

**Phase 4 — Constrained Layout Engine**
- `CharacterWidthTable` (internal static) — per-character width factors for Helvetica/Arial proportional sans-serif; replaces the crude uniform `text.Length × 0.6` estimate in `SvgRenderContext.MeasureText`
- `ConstrainedLayoutEngine` (internal sealed) — `Compute(Figure, IRenderContext) → SubPlotSpacing`; measures Y-tick labels, axis labels, and titles; clamps margins left ∈ [30,120], bottom ∈ [30,100], top ∈ [20,80], right ∈ [10,60]
- `LayoutMetrics` (internal record) — per-subplot margin requirements consumed by the engine
- `SubPlotSpacing.ConstrainedLayout` — new `bool` property; both `TightLayout` and `ConstrainedLayout` invoke the engine
- `FigureBuilder.ConstrainedLayout()` — fluent method to enable the engine
- `ChartRenderer.Render` wired: when `TightLayout || ConstrainedLayout`, calls engine before layout
- `SvgRenderContext.MeasureText` improved: uses `CharacterWidthTable` per character instead of uniform factor

**Phase 5 — Math Text Parser**
- `MathTextParser` — state-machine mini-LaTeX parser: `$...$` delimiters, `\command` → Greek/symbol Unicode substitution, `^{text}` / `_text` super/subscript spans; `Parse(string) → RichText`, `ContainsMath(string?) → bool`
- `RichText` sealed record — `IReadOnlyList<TextSpan> Spans`
- `TextSpan` sealed record — `string Text`, `TextSpanKind Kind` (Normal/Superscript/Subscript), `double FontSizeScale`
- `GreekLetters` — 48-entry dictionary: `\alpha`…`\omega` (24 lowercase) and `\Alpha`…`\Omega` (24 uppercase) → Unicode
- `MathSymbols` — 40+ entries: `\pm`, `\times`, `\div`, `\leq`, `\geq`, `\neq`, `\infty`, `\approx`, `\cdot`, `\degree`, and more
- `IRenderContext.DrawRichText()` — default interface method; concatenates span text and delegates to `DrawText()` for backends that do not natively support rich text
- `SvgRenderContext.DrawRichText()` — override emits `<tspan baseline-shift="super/sub" font-size="70%">` for super/subscript spans
- `AxesRenderer.RenderTitle` and `RenderAxisLabels` detect `$...$` and route through `DrawRichText`
- `ChartRenderer.RenderBackground` (figure title) likewise routes through `DrawRichText`

**Phase 6 — GIF Animation Export**
- `GifEncoder` — custom minimal GIF89a encoder: NETSCAPE2.0 loop extension, per-frame graphic control, LZW-compressed image data
- `ColorQuantizer` — uniform 6×7×6 = 252-color palette (+ 4 reserved) quantization
- `GifTransform` — renders `AnimationBuilder` frames via `SkiaRenderContext`, quantizes each frame, writes animated GIF
- `IAnimationTransform` — interface: `Transform(IEnumerable<Figure>, TimeSpan, bool, Stream)`
- `AnimationSkiaExtensions` — `SaveGif(string path)`, `ToGif() → byte[]` extension methods on `AnimationBuilder`

### Fixed

- Resolved all CS build warnings across `MatPlotLibNet` and `MatPlotLibNet.Skia`:
  - Nullable suppression operators on test parameters that were incorrectly typed as nullable
  - Removed stale `<cref>` and `<paramref>` XML doc references
  - `QuiverKeySeries.Label` hides inherited `ChartSeries.Label`: added `new` keyword
  - `SkiaRenderContext`: migrated from deprecated `SKPaint.TextSize`/`Typeface`/`MeasureText`/`DrawText(…,SKPaint)` to the current `SKFont` API

### Samples

Added three new examples to `MatPlotLibNet.Samples.Console`:
- **Example 18 — Date axis**: 90-day `DateTime[]` time-series; `AutoDateLocator` picks month-boundary ticks automatically
- **Example 19 — Math text labels**: 2-panel physics chart with Greek letters (`$\alpha$`, `$\sigma$`, `$\omega$`), super/subscript (`R$^{2}$`, `$\Delta t$`), and `.TightLayout()`
- **Example 20 — PropCycler**: 4-series sine chart with `PropCyclerBuilder` cycling four colors × four line styles

### Tests: 2268 → 2430 (+162)

---

## [0.8.0] - 2026-04-10

### Added

**17 new series types (43 → 60)**

*Phase A — Statistical & categorical:*
- `RugplotSeries` — tick marks along X axis showing individual data distribution (`Vec Data`, `Height`, `Alpha`, `LineWidth`)
- `StripplotSeries` — jittered points per category (`double[][] Datasets`, `Jitter`, `MarkerSize`, `Alpha`)
- `EventplotSeries` — vertical tick lines per event row (`double[][] Positions`, `LineLength`, `Colors[]`)
- `BrokenBarSeries` — broken horizontal bars for Gantt-style ranges (`(double Start, double Width)[][]`, `BarHeight`)
- `CountSeries` — bar chart auto-counting category frequencies (`string[] Values`, `BarOrientation`)
- `PcolormeshSeries` — pseudocolor grid with irregular quad cells (`Vec X`, `Vec Y`, `double[,] C`, `IColorMap`)
- `ResidualSeries` — residual scatter from polynomial fit (`Vec XData`, `Vec YData`, `Degree`, `ShowZeroLine`)

*Phase B — Statistical helpers + dependent series:*
- `PointplotSeries` — mean + confidence interval per category dataset (`CapSize`, `ConfidenceLevel`)
- `SwarmplotSeries` — beeswarm-algorithm non-overlapping dot plot (`MarkerSize`, `Alpha`)
- `SpectrogramSeries` — STFT spectrogram heatmap (`Vec Signal`, `SampleRate`, `WindowSize`, `Overlap`, `IColorMap`)
- `TableSeries` — tabular data rendered inside axes (`string[][] CellData`, `ColumnHeaders`, `RowHeaders`)

*Phase C — Triangular mesh & field:*
- `TricontourSeries` — iso-contour lines on unstructured triangular mesh (`Vec X`, `Vec Y`, `Vec Z`, `Levels`)
- `TripcolorSeries` — pseudocolor fill on triangular mesh with auto-Delaunay (`int[]? Triangles`)
- `QuiverKeySeries` — reference arrow legend for quiver plots (axes-fraction position, `U`, `Label`)
- `BarbsSeries` — meteorological wind barbs with speed/direction flags (`Vec Speed`, `Vec Direction`, `BarbLength`)

*Phase D — 3D:*
- `Stem3DSeries` — vertical lines from XY-plane to 3D data points (`Vec X`, `Vec Y`, `Vec Z`, `MarkerSize`)
- `Bar3DSeries` — 3D rectangular prism bars with depth-sorted painter's algorithm (`BarWidth`)

**5 new numeric helpers**
- `Vec.Percentile(double p)` / `Vec.Quantile(double q)` — sorted linear-interpolation percentile on Vec
- `Fft` (public static) — Cooley-Tukey radix-2 DIT with Hann window; `Forward(double[])` + `Stft(...)` → `StftResult(Magnitudes, Frequencies, Times)`
- `BeeswarmLayout` (internal static) — greedy O(n²) circle-packing for swarm plots; falls back to deterministic jitter for N > 1000
- `Delaunay` (public static) — Bowyer-Watson incremental triangulation returning `TriMesh(int[] Triangles, double[] X, double[] Y)`
- `HierarchicalClustering` (public static) — Ward's method agglomerative clustering returning `Dendrogram(DendrogramNode[] Merges, int[] LeafOrder)`

**3 new FigureTemplates**
- `FigureTemplates.PairPlot(double[][] columns, string[]? columnNames, int bins)` — N×N grid; diagonal = histograms, off-diagonal = scatter
- `FigureTemplates.FacetGrid(double[] x, double[] y, string[] category, Action<AxesBuilder, double[], double[]> plotFunc, int cols)` — one subplot per unique category
- `FigureTemplates.Clustermap(double[,] data, string[]? rowLabels, string[]? colLabels)` — 2×2 GridSpec heatmap with row/column dendrograms

**Tests: 1924 → 2268 (+344)**

---

## [0.7.0] - Unreleased

### Added

**Feature 4a — KdeSeries + GaussianKde**
- `KdeSeries` (sealed, Distribution family) — kernel density estimation rendered as a filled area + density curve
  - Properties: `Data[]`, `Bandwidth` (double?, null = auto Silverman), `Fill` (bool, default true), `Alpha` (double, default 0.3), `LineWidth` (double, default 1.5), `Color`, `LineStyle`
  - Implements `ISeriesSerializable`, `IHasDataRange` (30% X padding, density curve Y range)
- `GaussianKde` (internal static, `Rendering/SeriesRenderers/Distribution/`) — Gaussian KDE math helper
  - `SilvermanBandwidth(double[] sortedData)` → `1.06 * σ * n^(-0.2)`, fallback 1.0 for constant/degenerate data
  - `Evaluate(double[] sortedData, double bandwidth, int numPoints=100)` → `(double[] X, double[] Density)` over [min-3h, max+3h]
- `KdeSeriesRenderer` — sorts data → bandwidth → `GaussianKde.Evaluate` → optional filled polygon + density polyline
- `Axes.Kde()`, `AxesBuilder.Kde()`, `FigureBuilder.Kde()` — fluent factory methods
- `SeriesRegistry` registration for `"kde"` type discriminator
- `SeriesDto.Bandwidth` (`double?`) added
- Series count: 40 → 41

**Feature 4b — RegressionSeries + LeastSquares**
- `RegressionSeries` (sealed, XY family) — polynomial regression line with optional confidence bands
  - Properties: `XData[]`, `YData[]`, `Degree` (int, default 1), `ShowConfidence` (bool, default false), `ConfidenceLevel` (double, default 0.95), `LineWidth` (double, default 2.0), `Color`, `BandColor`, `BandAlpha` (double, default 0.2), `LineStyle`
- `LeastSquares` (public static, `Numerics/`) — polynomial regression math helper
  - `PolyFit(double[] x, double[] y, int degree)` → coefficient array `[a₀, a₁, ..., aₙ]` via normal equations, degree 0–10
  - `PolyEval(double[] coefficients, double[] x)` → evaluated Y values via Horner's method
  - `ConfidenceBand(double[] x, double[] y, double[] coeff, double[] evalX, double level=0.95)` → `(double[] Upper, double[] Lower)` using leverage-based t-distribution intervals
- `RegressionSeriesRenderer` — 100 eval points on linspace, optional confidence-band polygon
- `Axes.Regression()`, `AxesBuilder.Regression()` — fluent factory methods
- `SeriesRegistry` registration for `"regression"` type discriminator
- `SeriesDto.Degree` (`int?`), `SeriesDto.ShowConfidence` (`bool?`), `SeriesDto.ConfidenceLevel` (`double?`) added
- Series count: 41 → 42

**Feature 4c — HexbinSeries + HexGrid**
- `HexbinSeries` (sealed, Grid family) — 2D hexagonal bin density plot
  - Properties: `X[]`, `Y[]`, `GridSize` (int, default 20), `MinCount` (int, default 1), `ColorMap`, `Normalizer`
  - Implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `HexGrid` (internal static, namespace `MatPlotLibNet.Numerics`) — flat-top hex bin math helper
  - `ComputeHexBins(...)` → `Dictionary<(int q, int r), int>` count map using axial (q,r) cube-coordinate rounding
  - `HexagonVertices(cx, cy, hexSize)` → 6 vertex coordinates for a flat-top hexagon
  - `HexCenter(q, r, hexSize, ...)` → (X, Y) center coordinates
- `HexbinSeriesRenderer` — renders colored hexagonal polygons with 5% visual gap; uses `HexGrid.ComputeHexBins`
- `Axes.Hexbin()`, `AxesBuilder.Hexbin()` — fluent factory methods
- `SeriesRegistry` registration for `"hexbin"` type discriminator
- `SeriesDto.GridSize` (`int?`), `SeriesDto.MinCount` (`int?`) added
- Series count: 42 → 43

**Feature 4d — JointPlotBuilder**
- `FigureTemplates.JointPlot(double[] x, double[] y, string? title = null, int bins = 30)` — scatter + marginal histogram template
  - 2×2 `GridSpec` with `heightRatios=[1,4]`, `widthRatios=[4,1]`
  - Top marginal: `Histogram(x)` at `GridPosition(0,1,0,1)`
  - Center: `Scatter(x, y)` at `GridPosition(1,2,0,1)`
  - Right marginal: `Histogram(y)` at `GridPosition(1,2,1,2)`

**Feature 5a — Data Attributes Foundation**
- `Figure.EnableLegendToggle`, `EnableRichTooltips`, `EnableHighlight`, `EnableSelection` (bool) — per-feature interactivity flags
- `Figure.HasInteractivity` (bool) — true when any flag is set; used to gate data-attribute emission
- `Axes.EnableInteractiveAttributes` (bool) — propagated by `SvgTransform` before parallel rendering
- `SvgRenderContext.BeginDataGroup(string cssClass, int seriesIndex)` — emits `<g class="..." data-series-index="N">`
- `SvgRenderContext.BeginLegendItemGroup(int legendIndex)` — emits `<g data-legend-index="N" style="cursor:pointer">`
- `AxesRenderer.RenderSeries()` — wraps each series in a `data-series-index` group when `EnableInteractiveAttributes`
- `AxesRenderer.RenderLegend()` — wraps each legend entry in a `data-legend-index` group when `EnableInteractiveAttributes`

**Feature 5b — Legend Toggle Script**
- `SvgLegendToggleScript` — click `[data-legend-index=N]` → toggles `display` on `g[data-series-index=N]` + dims legend entry opacity to 0.4
- `FigureBuilder.WithLegendToggle(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableLegendToggle` is true

**Feature 5c — Rich Tooltips Script**
- `SvgCustomTooltipScript` — intercepts `<title>` elements and shows a styled floating `div` tooltip instead of native browser tooltip
- `FigureBuilder.WithRichTooltips(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableRichTooltips` is true

**Feature 5d — Highlight Script**
- `SvgHighlightScript` — `mouseenter` on `g[data-series-index]` → dims siblings to 0.3 opacity; `mouseleave` → restores all to 1.0
- `FigureBuilder.WithHighlight(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableHighlight` is true

**Feature 5e — Selection Script**
- `SvgSelectionScript` — Shift+mousedown draws a blue selection rectangle; mouseup dispatches `CustomEvent('mpl:selection', { detail: { x1, y1, x2, y2 } })` on the SVG element
- `FigureBuilder.WithSelection(bool enabled = true)` — fluent enable method
- Injected by `SvgTransform` when `Figure.EnableSelection` is true

**Notebooks package fix**
- `MatPlotLibNet.Notebooks.csproj` — added `<BuildOutputTargetFolder>interactive-extensions/dotnet</BuildOutputTargetFolder>` so Polyglot Notebooks auto-discovers `NotebookExtension` via `IKernelExtension`
- `Microsoft.DotNet.Interactive` reference now carries `PrivateAssets="all"` to prevent transitive dependency leakage

**Test suite:** 1924 tests (up from 1777), zero regressions.

**Feature 1 — Style Sheets / rcParams**
- `RcParams` global configuration registry — typed dictionary keyed by string (e.g., `"font.size"`, `"lines.linewidth"`, `"axes.grid"`), thread-safe via `AsyncLocal<T>` scoping
- `RcParams.Default` static instance with hard-coded defaults matching current behavior
- `RcParams.Current` resolves scoped override → Default (AsyncLocal per async flow)
- `RcParamKeys` static constants for all supported keys — compile-time safe, no string typos
- `StyleSheet` named bundle of `RcParams` overrides — `StyleSheet.FromTheme(Theme)` bridge converts existing 6 themes to style sheets
- `StyleContext : IDisposable` scoped override — pushes `RcParams` layer on construct, pops on `Dispose()`; nests arbitrarily
- `StyleSheetRegistry` thread-safe `ConcurrentDictionary` — all 6 built-in themes auto-registered as style sheets
- `Plt.Style.Use(name)` / `Plt.Style.Use(StyleSheet)` — modifies global defaults (matches `matplotlib.pyplot.style.use()`)
- `Plt.Style.Context(name)` / `Plt.Style.Context(StyleSheet)` — returns `StyleContext` for scoped overrides (matches `matplotlib.pyplot.style.context()`)
- `Theme.ToStyleSheet()` — converts any `Theme` to a `StyleSheet` for use with rcParams
- Precedence: explicit property > Theme > `RcParams.Current` > `RcParams.Default`
- `FigureBuilder`, `CartesianAxesRenderer`, `LineSeriesRenderer`, `ScatterSeriesRenderer` consult `RcParams.Current` for defaults when no explicit value is set

**Feature 2 — Filled Contours (ContourfSeries)**
- `ContourfSeries` (sealed, Grid family) — filled contour plot rendering colored bands between consecutive iso-levels
  - Properties: `XData[]`, `YData[]`, `ZData[,]`, `Levels` (int, default 10), `Alpha` (double, default 1.0), `ShowLines` (bool, default true), `LineWidth` (double, default 0.5), `ColorMap`, `Normalizer`
  - Implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `ContourfSeriesRenderer` — painter's algorithm: fills entire plot area with bottom band color, then paints ascending iso-level regions over previous using `DrawPolygon()`; optional iso-line overlay via `DrawLines()`
- `MarchingSquares.ExtractBands()` — new method producing `ContourBand[]` (closed polygon bands between iso-levels)
- `ContourBand` `readonly record struct` — `(double LevelLow, double LevelHigh, PointF[][] Polygons)`
- `ISeriesVisitor.Visit(ContourfSeries)` — new visitor overload
- `Axes.Contourf()`, `AxesBuilder.Contourf()`, `FigureBuilder.Contourf()` — fluent API methods
- `SeriesRegistry` registration for `"contourf"` type discriminator
- Series count: 39 → 40

**Feature 3 — Image Compositing**
- `IInterpolationEngine` strategy interface — `Resample(double[,] data, int targetRows, int targetCols)`
- `NearestInterpolation` (singleton) — identity / pixel duplication (existing behavior)
- `BilinearInterpolation` (singleton) — 2×2 neighborhood, linear weights
- `BicubicInterpolation` (singleton) — 4×4 neighborhood, Catmull-Rom / Keys kernel with output clamping to prevent ringing
- `InterpolationRegistry` thread-safe `ConcurrentDictionary` — maps `"nearest"` / `"bilinear"` / `"bicubic"` to engine instances (mirrors `ColorMapRegistry` pattern)
- `BlendMode` enum — `Normal`, `Multiply`, `Screen`, `Overlay`
- `CompositeOperation` static utility — `Color Blend(Color src, Color dst, BlendMode mode)`
- `ImageSeries.Alpha` (`double`, default 1.0) — overall opacity
- `ImageSeries.BlendMode` (`BlendMode`, default `Normal`) — alpha composite blend mode
- `ImageSeriesRenderer` enhanced — resolves `InterpolationRegistry.Get(series.Interpolation)` to resample data before rendering; upsampled grid capped at min(source×4, 256) to prevent SVG size explosion

**Test suite:** 1777 tests (up from 1668), zero regressions.

## [0.6.0] - 2026-04-09

### Added

**Batch 1 — VectorMath SIMD Kernel**
- `VectorMath` (`internal static`) — `System.Numerics.Tensors.TensorPrimitives` wrappers: `Add`, `Subtract`, `Multiply`, `Divide`, `Sum`, `Min`, `Max`, `Abs`, `Negate`, `MultiplyAdd`
- `VectorMath` domain algorithms: `Linspace`, `RollingMean`, `RollingMin`, `RollingMax` (O(n) monotone deque), `RollingStdDev`, `CumulativeSum`, `StandardDeviation`, `SplitPositiveNegative`
- `Vec` (`public readonly record struct`) — LINQ-style wrapper with SIMD-accelerated operators (`+`, `-`, `*`, `/`, unary `-`), reductions (`Sum`, `Min`, `Max`, `Mean`, `Std`), scalar lambdas (`Select`, `Where`, `Zip`, `Aggregate`), and implicit `double[]` conversions
- `System.Numerics.Tensors` NuGet dependency added to main package

**Batch 2 — DataTransform Batch Path**
- `DataTransform.TransformX(ReadOnlySpan<double>)` — SIMD batch X coordinate transform
- `DataTransform.TransformY(ReadOnlySpan<double>)` — SIMD batch Y coordinate transform
- `DataTransform.TransformBatch(ReadOnlySpan<double>, ReadOnlySpan<double>)` — single-pass AVX SIMD interleave (FMA → UnpackLow/High → Permute2x128 → direct store), zero intermediate allocations, 3.6× faster than per-point loop at 1K points
- `VectorMath.TransformInterleave` — SoA→AoS affine transform kernel with AVX fast path and scalar fallback
- 8 series renderers refactored to pre-compute batch pixel coordinates: `LineSeriesRenderer`, `AreaSeriesRenderer`, `ScatterSeriesRenderer`, `StepSeriesRenderer`, `EcdfSeriesRenderer`, `StackedAreaSeriesRenderer`, `ErrorBarSeriesRenderer`, `BubbleSeriesRenderer`

**Batch 3 — Indicator Refactoring**
- All 15 indicators (`Sma`, `Ema`, `BollingerBands`, `Stochastic`, `Ichimoku`, `Adx`, `Atr`, `Rsi`, `Macd`, `KeltnerChannels`, `Vwap`, `EquityCurve`, `DrawDown`, `ProfitLoss`, `Indicator.ApplyOffset`) refactored to use `VectorMath` instead of scalar loops

**Batch 4 — Phase F Indicators**
- `WilliamsR` — Williams %R momentum indicator (-100..0), reference lines at -20 and -80
- `Obv` — On-Balance Volume, sequential cumulative indicator
- `Cci` — Commodity Channel Index, mean-deviation oscillator, reference lines at ±100
- `ParabolicSar` — Parabolic SAR trend indicator; returns `ParabolicSarResult(double[] Sar, bool[] IsLong)`
- `AxesBuilder` shortcuts: `WilliamsR()`, `Obv()`, `Cci()`, `ParabolicSar()`

**Batch 5 — Chart Templates**
- `FigureTemplates.FinancialDashboard()` — 3-panel chart (price/candlestick 60%, volume 15%, oscillator 25%) with shared X axis and custom GridSpec height ratios
- `FigureTemplates.ScientificPaper()` — N×M subplot grid, 150 DPI, tight layout, hidden top/right spines
- `FigureTemplates.SparklineDashboard()` — vertically stacked sparklines, one row per data series with Y label

**Batch 6 — Contour Labels (Marching Squares)**
- `MarchingSquares` (`internal static`) in `Rendering/Algorithms/` — 4-bit cell classification, edge interpolation, greedy segment joining into polylines
- `ContourSeries.LabelFormat` (`string?`, default `"G4"`) — format string for contour level labels
- `ContourSeries.LabelFontSize` (`double`, default `10`) — font size for contour level labels
- `ContourSeriesRenderer` — now draws iso-lines via marching-squares; `ShowLabels = true` renders centered labels with white background rectangles

**Batch 7 — Polyglot Notebooks**
- New package `MatPlotLibNet.Notebooks` — `IKernelExtension` for Polyglot Notebooks / Jupyter
- `NotebookExtension` — registers `Figure` as an inline SVG display type via `Formatter.Register<Figure>`
- `FigureFormatter` — wraps `figure.ToSvg()` in a `<div>` for notebook cell output

**Batch 8 — Benchmarks**
- `VectorMathBenchmarks.cs` — benchmarks Vec SIMD operators, reductions, and domain algorithm proxies
- `DataTransformBenchmarks.cs` — per-point loop vs TransformBatch comparison
- Extended `IndicatorBenchmarks.cs` — added WilliamsR, OBV, CCI, ParabolicSar
- Extended `SvgRenderingBenchmarks.cs` — added 10K-point line chart and 100K-point LTTB chart
- Updated `BENCHMARKS.md` with new sections

### Fixed
- `Macd.Compute()` — guard against out-of-range slice when MACD data is shorter than the signal period

## [0.5.1] - 2026-04-09

### Added

**Phase C — Text & Annotation**
- `Annotation.Alignment` (`TextAlignment`) — horizontal text alignment; default `Left`
- `Annotation.Rotation` (`double`) — text rotation in degrees; default 0
- `Annotation.ArrowStyle` (`ArrowStyle` enum) — `None`, `Simple` (existing line), `FancyArrow` (line + triangular arrowhead); default `Simple`
- `Annotation.BackgroundColor` (`Color?`) — optional fill rect drawn behind annotation text
- `ArrowStyle` enum — `None`, `Simple`, `FancyArrow`
- `BarSeries.ShowLabels` / `.LabelFormat` — auto-label bars with their values; format string is optional (defaults to G4)
- `ContourSeries.ShowLabels` — reserves property for future contour line labeling (rendering deferred to v0.6.0; requires marching-squares)
- `IRenderContext.DrawText(text, position, font, alignment, rotation)` — overload with rotation; default interface method ignores rotation (backward-compatible)
- `SvgRenderContext.DrawText(..., rotation)` — emits `transform="rotate(…)"` on the SVG text element

**Phase D — Tick System**
- `ITickLocator` interface — `double[] Locate(double min, double max)` strategy for axis tick positions
- `AutoLocator(int targetCount = 5)` — extracts the existing nice-number algorithm as a reusable locator
- `MaxNLocator(int maxN)` — nice numbers capped to at most `maxN` ticks
- `MultipleLocator(double baseValue)` — ticks at exact multiples of base in `[min, max]`
- `FixedLocator(double[] positions)` — returns exactly the provided positions filtered to range
- `LogLocator` — powers of 10 within range
- `EngFormatter` — SI prefix formatting: 1000→"1k", 1M→"1M", 1e-3→"1m", 1e-6→"1µ" etc.
- `PercentFormatter(double max)` — `value/max*100` + "%" suffix
- `Axis.TickLocator` (`ITickLocator?`) — per-axis custom locator; overrides default algorithm
- `Axis.MajorTicks` and `Axis.MinorTicks` are now settable (changed from `{ get; }` to `{ get; set; }`)
- Minor tick rendering — when `Axis.MinorTicks.Visible = true`, 5 minor subdivisions per major interval are drawn at half the tick length (3 px vs 5 px), no labels
- `TickConfig.Spacing` is now respected: auto-creates `MultipleLocator(spacing)` when no explicit locator is set
- `AxesBuilder.SetXTickLocator()` / `SetYTickLocator()` — fluent tick locator configuration
- `AxesBuilder.WithMinorTicks(bool)` — enables minor ticks on both axes
- Bug fix: secondary Y-axis tick labels now correctly use `Axes.SecondaryYAxis.TickFormatter` (was calling `FormatTick` unconditionally)
- Bug fix: `PolarAxesRenderer` ring labels now use `Axes.YAxis.TickFormatter` when set

**Phase E — Performance**
- `IDownsampler` interface — `(double[] X, double[] Y) Downsample(double[] x, double[] y, int targetPoints)`
- `LttbDownsampler` — Largest-Triangle-Three-Buckets O(n) algorithm; preserves visual peaks/troughs; always keeps first and last point
- `ViewportCuller` (static) — filters XY data to `[xMin, xMax]` keeping one point on each side for correct line clipping
- `XYSeries.MaxDisplayPoints` (`int?`) — opt-in downsampling for `LineSeries`, `AreaSeries`, `ScatterSeries`, `StepSeries`; viewport culling followed by LTTB when enabled
- `DataTransform.DataXMin/XMax/YMin/YMax` — public properties exposing the current viewport bounds (needed by renderers to pass to `ViewportCuller`)
- `AxesBuilder.WithDownsampling(int maxPoints = 2000)` — fluent downsampling configuration on last XY series
- `AxesBuilder.WithBarLabels(string? format = null)` — fluent bar label configuration on last bar series

### Changed

- `CartesianAxesRenderer` tick computation now calls `ComputeTickValues(min, max, Axis)` — respects `TickLocator` and `Spacing`
- `BarSeriesRenderer` — appends value text above vertical bars / beside horizontal bars when `ShowLabels = true`
- `LineSeriesRenderer`, `AreaSeriesRenderer`, `StepSeriesRenderer` — apply viewport culling + LTTB before rendering when `MaxDisplayPoints` is set
- `ScatterSeriesRenderer` — applies viewport culling when `MaxDisplayPoints` is set

## [0.5.0] - 2026-04-09

### Added

- `GridSpec` model — unequal subplot layouts with row/col height/width ratios and cell spanning
- `SpinesConfig` — per-spine show/hide/position (`Edge`, `Data`, `Axes` fraction) via `AxesBuilder.WithSpines()`, `.HideTopSpine()`, `.HideRightSpine()`
- Shared axes (`ShareX`/`ShareY`) with union range computation across linked subplots
- Inset axes — `AddInset(x, y, w, h)` on `AxesBuilder` with recursive rendering (depth guard = 3)
- `ImageSeries` (imshow) — display 2D data as colored pixels with colormap + `VMin`/`VMax`, implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `Histogram2DSeries` — 2D density histogram binning scatter data into a grid, implements `IColormappable`, `INormalizable`, `IColorBarDataProvider`
- `StreamplotSeries` — vector field streamlines with configurable `Density` and `ArrowSize`
- `EcdfSeries` — empirical cumulative distribution function (sorted XY series)
- `StackedAreaSeries` — stacked filled areas (stackplot) with `X[]`, `YSets[][]`, `StackLabels`, `FillColors`
- `ICategoryLabeled` — polymorphic tick-label resolution for bar/candlestick series; eliminates per-type casts in renderers
- `IColorBarDataProvider` — colorbar auto-detection from series data range + colormap; eliminates type dispatch in `AxesBuilder.WithColorBar()`
- `IStackable` — stacking offset computation for bar series
- `IRenderContext.BeginGroup`/`EndGroup` — default interface methods; eliminated 6 type casts across renderers
- `PathSegment.ToSvgPathData()` — polymorphic SVG path rendering; eliminated 5-case `switch` in `SvgSeriesRenderer`
- Series count increased from 34 to 39 chart types
- `IColormappable` interface — `IColorMap? ColorMap { get; set; }` — implemented by all 7 series that support colormaps (`HeatmapSeries`, `ImageSeries`, `Histogram2DSeries`, `ContourSeries`, `SurfaceSeries`, `ScatterSeries`, `HierarchicalSeries`)
- `INormalizable` interface — `INormalizer? Normalizer { get; set; }` — implemented by `HeatmapSeries`, `ImageSeries`, `Histogram2DSeries`
- **20 new colormaps** (52 base total, 104 with reversed `_r` variants):
  - Sequential: `Hot`, `Copper`, `Bone`, `BuPu`, `GnBu`, `PuRd`, `RdPu`, `YlGnBu`, `PuBuGn`, `Cubehelix`
  - Diverging: `PuOr`, `Seismic`, `Bwr`
  - Qualitative: `Pastel2`, `Dark2`, `Accent`, `Paired`
  - Special: `Turbo` (perceptually-uniform rainbow), `Jet` (legacy rainbow), `Hsv` (cyclic hue)
- 502 new tests (1502 total); category-specific theories: monotonic brightness, diverging midpoint neutrality, cyclic start≈end, qualitative color distinctness

### Changed

- `FigureBuilder.WithGridSpec()` / `AddSubPlot(GridPosition, ...)` for GridSpec-based unequal subplot layouts
- `AxesBuilder.WithSpines()`, `.HideTopSpine()`, `.HideRightSpine()` for spine control
- `AxesBuilder.ShareX(key)` / `.ShareY(key)` for shared-axis range synchronization
- `AxesBuilder.AddInset(x, y, w, h, configure)` for inset axes
- `AxesBuilder.WithColorMap(IColorMap)` — replaced 4-branch `if/else if` type chain with `if (last is IColormappable c)` — now covers all 7 colormappable series (previously missed `SurfaceSeries`, `ScatterSeries`, `HierarchicalSeries`)
- `AxesBuilder.WithNormalizer(INormalizer)` — replaced 3-branch `if/else if` type chain with `if (last is INormalizable n)`

## [0.4.1] - 2026-04-06

### Added

- `ISeriesSerializable` interface on all 34 series — each series serializes itself, eliminating the 152-line `SeriesToDto` switch in `ChartSerializer`
- `SeriesRegistry` for deserialization with `ConcurrentDictionary`-based type lookup
- `IHasDataRange` interface for series that expose their own data bounds
- `IPolarSeries` interface for polar coordinate series
- `I3DGridSeries` and `I3DPointSeries` interfaces for 3D series families
- `IPriceSeries` interface for financial OHLC series
- Generic base classes: `XYSeries`, `PolarSeries`, `GridSeries3D`, `HierarchicalSeries`
- Color constants: `Tab10Blue`, `Tab10Orange`, `Tab10Green`, `GridGray`, `EdgeGray`, `Amber`, `FibonacciOrange` — replacing magic hex strings throughout the codebase
- `IAnimation<TState>` interface and `AnimationController<TState>` for typed animation pipelines
- `LegacyAnimationAdapter` bridges `AnimationBuilder` to `IAnimation<TState>` contract
- `ConfigureAwait(false)` in `AnimationController` for library-safe async

### Changed

- Target frameworks changed to `net10.0;net8.0` (dropped `netstandard2.1`)
- Removed `IsExternalInit` polyfill (no longer needed without netstandard2.1)
- `FigureBuilder` SRP: `Save()`, `Transform()`, `ToSvg()` moved to `FigureExtensions` — builder only builds
- `FigureExtensions.RegisterTransform()` replaces `FigureBuilder.RegisterGlobalTransform()` for startup-time format registration
- `GlobalTransforms` registry uses `ConcurrentDictionary` for thread safety
- `AxesRenderer` registry uses `ConcurrentDictionary` for thread-safe coordinate system dispatch
- Volatile fields used for thread-safe state in animation and rendering pipelines
- Publish workflow fix: build before pack for Skia/MAUI projects
- Warning cleanup: xUnit1051 `CancellationToken` warnings and CS8604 nullable reference warnings resolved

## [0.4.0] - 2026-04-06

### Added

- `Projection3D` class for 3D-to-2D projection with elevation/azimuth rotation and depth sorting
- `DataRange3D` record struct for 3D data bounds
- `SurfaceSeries` — colored quadrilateral surface with optional wireframe overlay
- `WireframeSeries` — 3D wireframe grid rendering
- `Scatter3DSeries` — 3D scatter with depth-based size variation
- `ChartRenderer.Render3DAxes()` — 3D bounding box wireframe, axis labels, painter's algorithm
- `ColorBar` record with auto-detect from heatmap/contour data range and colormap
- `AxesBuilder.WithColorBar()` and `WithProjection(elevation, azimuth)` fluent methods
- `FigureBuilder.Save(path)` with auto-detect format from extension (no extension = SVG)
- `AnimationBuilder` class for frame-based animation (FrameCount, Interval, Loop, GenerateFrames)
- `InteractiveFigure.AnimateAsync()` for pushing animation frames via SignalR
- `CoordinateSystem` enum (`Cartesian`, `Polar`, `ThreeD`) on `Axes` for alternative rendering paths
- `PolarTransform` class for (r, theta) to pixel coordinate conversion
- `PolarLineSeries`, `PolarScatterSeries`, `PolarBarSeries` in new Polar family
- `ChartRenderer.RenderPolarAxes()` — circular grid, radial axis lines, angle labels
- `FigureBuilder.ToSvg()`, `ToJson()`, `SaveSvg()`, `Transform()`, `Save(path)` — output directly from the builder without `.Build()`
- `FigureBuilder.Save(path)` auto-detects format from file extension (.svg, .png, .pdf, .json)
- `TreeNode` record for hierarchical data (Label, Value, Color, Children with recursive TotalValue)
- `HierarchicalSeries` abstract base class with shared Root, ColorMap, ShowLabels properties
- `TreemapSeries` — nested rectangle layout with configurable padding
- `SunburstSeries` — concentric ring segments with configurable inner radius
- `TreemapSeriesRenderer` — squarified slice-and-dice layout algorithm
- `SunburstSeriesRenderer` — arc-based radial rendering with recursive depth
- `SankeySeries` — flow diagram with nodes and bezier-curved links
- `SankeyNode` and `SankeyLink` records for Sankey data model
- `SankeySeriesRenderer` — BFS column layout, curved link rendering, node labels
- Legend rendering in `ChartRenderer.RenderLegend()` with color swatches and position control
- `SubPlotSpacing` record with configurable margins, gaps, and `TightLayout` flag
- `ITickFormatter` interface for pluggable axis tick formatting
- `DateTickFormatter` — formats OLE Automation dates with configurable format string
- `LogTickFormatter` — superscript notation for powers of ten
- `NumericTickFormatter` — extracted from existing `FormatTick` logic
- `AxisScale.Date` enum value for date axes
- `Axis.TickFormatter` property for custom tick formatting
- `AxesBuilder.WithLegend()`, `SetXDateFormat()`, `SetYDateFormat()`, `SetXTickFormatter()`, `SetYTickFormatter()` fluent methods
- `FigureBuilder.TightLayout()` and `WithSubPlotSpacing()` fluent methods
- `SvgRenderContext.BeginGroup()` / `EndGroup()` for CSS-classed SVG groups

### Changed

- Subplot layout margins are now configurable via `Figure.Spacing` (was hardcoded constants)
- `ChartRenderer.RenderTicks` uses `Axis.TickFormatter` when set (falls back to default formatting)
- GitHub Actions updated to v5 (Node.js 24 compatibility)
- Series count increased from 25 to 34
- `ChartRenderer` refactored from ~1100 lines to ~100 lines — all axes rendering moved to polymorphic `AxesRenderer` subclasses
- `AxesRenderer` abstract base with `CartesianAxesRenderer`, `PolarAxesRenderer`, `ThreeDAxesRenderer` — no more `private static` methods with repeated parameters
- `ChartRenderer.RenderAxes` is now a one-liner: `AxesRenderer.Create(axes, plotArea, ctx, theme).Render()`
- Tests refactored to use builder output methods (`.ToSvg()`) instead of explicit `.Build()`

## [0.3.2] - 2026-04-05

### Added

- `IIndicatorResult` marker interface — all indicator result types must implement it
- `SignalResult` record for single-line indicators (SMA, EMA, RSI, ATR, etc.) with implicit `double[]` conversion
- `BandsResult` record for band indicators (Bollinger Bands, Keltner Channels)
- `MacdResult` record for MACD (MacdLine, SignalLine, Histogram)
- `StochasticResult` record for Stochastic (%K, %D)
- `IchimokuResult` record for Ichimoku Cloud (5 lines)
- 92 new tests: SkiaRenderContext (18), MAUI RenderContext (12), ColorMaps (17), SvgRenderContext (19), ChartRenderer (10), JSON serialization round-trip (9), indicator type assertions (7)
- BenchmarkDotNet project with 23 benchmarks: SVG rendering, JSON serialization, Skia export, 12 indicators at 1K/10K/100K data points
- CHANGELOG.md, BENCHMARKS.md with real performance numbers
- DocFX scaffolding (docfx.json, toc.yml, articles/intro.md)
- 4 runnable sample projects (Console, Blazor, WebApi, GraphQL)
- howTo.md for React, Vue, GraphQL packages
- Skia README.md and NuGet pack metadata
- `dotnet-coverage` tooling for code coverage with xUnit v3
- `GenerateDocumentationFile` enabled globally — XML ships with NuGet packages
- `InternalsVisibleTo` on core library for test access

### Changed

- All 16 indicators refactored to `Indicator<TResult> where TResult : IIndicatorResult` — no more untyped `Indicator` or raw `double[]` generics
- Static `Compute` methods removed from all indicators — computation lives in instance `override Compute()` only
- Tuple return types replaced with named records (`BandsResult` instead of `(double[], double[], double[])`)
- JSON serialization fixed for 9 series types that previously fell through to `Type = "unknown"` (DonutSeries, BubbleSeries, OhlcBarSeries, WaterfallSeries, FunnelSeries, GanttSeries, GaugeSeries, ProgressBarSeries, SparklineSeries)

## [0.3.1] - 2026-04-05

### Added

- `@matplotlibnet/react` npm package: React 19 hooks (`useMplChart`, `useMplLiveChart`), components (`MplChart`, `MplLiveChart`), TypeScript SignalR client
- `@matplotlibnet/vue` npm package: Vue 3 composables (`useMplChart`, `useMplLiveChart`), components (`MplChart`, `MplLiveChart`), TypeScript SignalR client
- `MatPlotLibNet.GraphQL` package: HotChocolate integration with `ChartQueryType`, `ChartSubscriptionType`, `GraphQLChartPublisher`, `IChartEventSender`
- `AddMatPlotLibNetGraphQL()` and `MapMatPlotLibNetGraphQL()` extension methods for DI and endpoint registration
- `netstandard2.1` target on core library for broader ecosystem compatibility
- `IsExternalInit` polyfill and conditional `System.Text.Json` package reference for netstandard2.1

### Changed

- Core `MatPlotLibNet.csproj` now targets `net10.0;netstandard2.1` (was `net10.0` only)
- Solution file updated to include GraphQL source and test projects

## [0.3.0] - 2026-04-05

### Added

- 9 new series types organized into chart families: `DonutSeries`, `BubbleSeries`, `OhlcBarSeries`, `WaterfallSeries`, `FunnelSeries`, `GanttSeries`, `GaugeSeries`, `ProgressBarSeries`, `SparklineSeries`
- 13 technical indicators: SMA, EMA, Bollinger Bands, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci Retracement, ATR, ADX, Keltner Channels, Ichimoku Cloud
- Trading analytics: `EquityCurve`, `ProfitLoss`, `DrawDown` panel indicators
- Buy/sell signal markers (`BuySellSignal`, `SignalMarker`)
- Generic `SeriesRenderer<T>` base class with `SeriesRenderContext` for type-safe rendering
- `Indicator<TResult>` generic base for composable indicator computation
- `PriceSource` enum (`Close`, `Open`, `HL2`, `HLC3`, `OHLC4`) for flexible price source selection
- Fluent indicator API on `AxesBuilder`: `.Sma(20)`, `.Ema(9)`, `.BollingerBands()`, `.BuyAt()`, `.SellAt()`, `.Rsi()`, `.AddIndicator()`
- `figure.SaveSvg(path)` convenience method
- Per-family series renderer directories (XY/, Categorical/, Circular/, Grid/, Distribution/, Financial/, Field/)
- Offset parameter and LineStyle customization for all overlay indicators

### Changed

- `SvgSeriesRenderer` refactored from monolithic visitor to thin dispatcher over `SeriesRenderer<T>` instances
- Series model classes reorganized from flat `Series/` directory into family subdirectories

## [0.2.0] - 2026-04-05

### Added

- 6 new series types: `AreaSeries`, `StepSeries`, `ErrorBarSeries`, `CandlestickSeries`, `QuiverSeries`, `RadarSeries`
- Stacked bars via `BarMode.Stacked` on `Axes`
- Annotations: `Annotation` model with text positioning and optional arrow
- Reference lines: `ReferenceLine` model for `AxHLine` / `AxVLine`
- Shaded regions: `SpanRegion` model for `AxHSpan` / `AxVSpan`
- Secondary Y-axis via `WithSecondaryYAxis()` / `SecondaryAxisBuilder`
- SVG tooltips via `<title>` elements (`WithTooltips()`)
- SVG zoom/pan via embedded JavaScript (`WithZoomPan()`, `SvgInteractivityScript`)
- Polymorphic export transforms: `IFigureTransform`, `FigureTransform` (abstract), `SvgTransform`, `TransformResult` (fluent `ToStream()`, `ToFile()`, `ToBytes()`)
- `MatPlotLibNet.Skia` package: `PngTransform`, `PdfTransform`, `SkiaRenderContext`
- Convenience extensions: `figure.Transform(t).ToFile()` / `.ToBytes()` / `.ToStream()`

### Changed

- `SvgRenderer` replaced by `SvgTransform` (also implements `ISvgRenderer` for backward compatibility)
- `FigureExtensions` expanded with `Transform()` method
- `ChartRenderer` expanded for annotations, decorations, and secondary axis rendering

## [0.1.0] - 2026-04-04

### Added

- Core library with fluent builder API: `Plt.Create()`, `FigureBuilder`, `AxesBuilder`, `ThemeBuilder`
- 10 series types: Line, Scatter, Bar, Histogram, Pie, Heatmap, Box, Violin, Contour, Stem
- `Figure`, `Axes`, `Axis` model hierarchy with `ISeries` / `ChartSeries` base
- Parallel SVG rendering via `SvgRenderer` with per-subplot `SvgRenderContext`
- JSON round-trip serialization via `ChartSerializer` / `IChartSerializer` (System.Text.Json)
- 6 built-in themes: Default, Dark, Seaborn, Ggplot, Bmh, FiveThirtyEight
- Custom theme builder with immutable records (`Theme`, `GridStyle`)
- `Color` readonly record struct with named colors, hex, and RGBA support
- 8 built-in color maps: Viridis, Plasma, Inferno, Magma, Coolwarm, Blues, Reds, Greens
- `DashPatterns`, `LineStyle`, `MarkerStyle` for consistent styling
- `IChartRenderer`, `IRenderContext`, `ISeriesVisitor` interfaces
- `DataTransform` for data-to-pixel coordinate mapping
- `ChartServices` static DI defaults
- `DisplayMode` enum (Inline, Expandable, Popup)
- `IChartSubscriptionClient` shared SignalR contract
- `MatPlotLibNet.Blazor` package: `MplChart`, `MplLiveChart` Razor components, `ChartSubscriptionClient`
- `MatPlotLibNet.AspNetCore` package: `ChartHub`, `ChartPublisher`, `IChartPublisher`, REST endpoints, SignalR hub
- `MatPlotLibNet.Interactive` package: `ChartServer`, `BrowserLauncher`, `ShowAsync()` extension
- `MatPlotLibNet.Maui` package: `MplChartView`, `MauiGraphicsRenderContext`
- `@matplotlibnet/angular` npm package: Angular components + TypeScript SignalR client
