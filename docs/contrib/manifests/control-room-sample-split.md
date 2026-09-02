# Path manifest — the control room becomes its own sample

**Change.** Move the control-room wall, its domain and its simulator out of `MatPlotLibNet.Samples.Blazor`
into a sample of its own, `MatPlotLibNet.Samples.ControlRoom`. Structural: files move between assemblies,
namespaces change, two DI registrations move, and the solution gains a project.

**Why.** The Blazor sample exists to show the `MplChart` / `MplLiveChart` controls in a Blazor host — a bar
chart, a scatter, a live SignalR chart. The control room is no longer an example of that; it is a reference
implementation of a screen, with its own domain (bus → process → lane, alarm conditioning, a staleness clock)
and a background simulator. Two audiences, two lifetimes, one project: the smaller sample was already the
harder one to read, and every control-room change now risks a file the Blazor sample owns.

## Files read end-to-end (not grep'd)

- `Samples/MatPlotLibNet.Samples.Blazor/Program.cs` (all 20 lines)
- `Samples/MatPlotLibNet.Samples.Blazor/MatPlotLibNet.Samples.Blazor.csproj`
- `Components/App.razor`, `Components/Routes.razor`, `Components/_Imports.razor` (all three, complete)
- `Components/Pages/Home.razor`, `LiveDashboard.razor` (complete), `ObsDashboard.razor` (complete)
- `Services/Bus.cs`, `Services/BusTelemetrySimulator.cs` (public surface + the evolve/roll-up paths)
- `MatPlotLibNet.slnx` (the /Samples/ folder), `MatPlotLibNet.CI.slnf` (complete), `Samples/README.md`

## What moves, and what does not

| item | verdict |
|---|---|
| `Components/Pages/ObsDashboard.razor` | MOVES — it is the control room |
| `Services/Bus.cs` (Bus · Process · Lane · Conditioned · OpsState) | MOVES — the observability domain, and nothing else references it |
| `Services/BusTelemetrySimulator.cs` | MOVES — its four chart ids are the control room's own |
| `Program.cs` lines registering `BusTelemetrySimulator` (singleton + hosted service) | MOVE to the new host; the Blazor sample stops hosting a background service it no longer shows |
| `Home.razor`'s link to `/obs-dashboard` | REMOVED from the Blazor sample — a link into an app that is no longer running there is worse than no link |
| `Components/App.razor`, `Routes.razor`, `_Imports.razor`, `MapChartHub()`, `AddMatPlotLibNetSignalR()` | COPIED, not moved: the new host needs its own, and the Blazor sample still needs its own for `/live` |
| `LiveDashboard.razor`, `Home.razor`, `Interactive.razor` | STAY — verified independent: `LiveDashboard` drives its own timer and publisher, never the simulator |

## Caller chains → root

| chain | state |
|---|---|
| `BusTelemetrySimulator` ← `Program.cs` (AddSingleton + AddHostedService) ← nothing else | COMPLETE — grep over the sample tree returns those two lines and `ObsDashboard.razor`'s `@inject` |
| `Bus` / `Process` / `Lane` ← `BusTelemetrySimulator`, `ObsDashboard.razor` | COMPLETE — no other file in the repo names them |
| `ProcessesChartId` / `ProcessTrendChartId` / `ThroughputChartId` / `LatencyChartId` ← `ObsDashboard.razor` + the simulator itself | COMPLETE |
| `/obs-dashboard` route ← `Home.razor`'s anchor, `Samples/README.md` | COMPLETE — both updated in this change |

## Callee chains → leaf

| chain | state |
|---|---|
| the page → `MplChart` / `MplLiveChart` → `MatPlotLibNet.Blazor` → `MatPlotLibNet` | COMPLETE — the new csproj carries the same three project references |
| the simulator → `IChartPublisher` / `IChartSubscriptions` → `MatPlotLibNet.AspNetCore` → SignalR | COMPLETE — the new host must call `AddMatPlotLibNetSignalR()` **and** `MapChartHub()`, or the live charts render nothing |

## DI / reflection / serialization touchpoints

- `Routes.razor` binds `AppAssembly="typeof(Program).Assembly"` — the router discovers pages by ASSEMBLY, so a
  page that moves assemblies is found by the new host and lost by the old one automatically. Nothing to update,
  and nothing that would have warned me if I had moved only one of the two.
- No reflection over the domain types, no serialization: the simulator publishes rendered SVG, never objects.

## Tests exercising the path

- None. No test project references `Samples/`; the test tree mirrors the library assemblies only. So the
  verification for this move is the RENDER check that verified the descent: fetch all three levels from the
  running app and count the blocks, the rail entries, the lane rows and the doorways.

## Assumptions NOT made

| assumption | verified by reading? |
|---|---|
| "the simulator is only used by the obs page" | **y** — Program.cs and the page are the only references |
| "LiveDashboard shares the simulator" | **n — REFUTED.** It owns a `Timer` and calls `IChartPublisher` directly; it moves nothing and breaks nothing |
| "the CI filter needs the new project" | **n — REFUTED.** The filter lists Src and Tst only; no sample is in it, and adding one would change what CI builds |
| "the router needs a route table update" | **n — REFUTED.** It scans the assembly |

**Verdict: zero UNTRACED rows. Code is unblocked.**
