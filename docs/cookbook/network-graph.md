# Network Graph

Nodes and edges in 2D — for correlation networks, lead-lag flows, Louvain community
visualisation, and minimum spanning trees. Three deterministic layouts ship in v1.10
PR 1: `Manual` (pass-through coords), `Circular` (unit-circle), `Hierarchical` (BFS top-down).
`ForceDirected` (Fruchterman–Reingold) is reserved at enum ordinal `1` and lands in PR 2.

## Basic graph

```csharp
GraphNode[] nodes =
[
    new("AAPL"),
    new("MSFT"),
    new("GOOG"),
];
GraphEdge[] edges =
[
    new("AAPL", "MSFT"),
    new("AAPL", "GOOG"),
    new("MSFT", "GOOG"),
];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges))
    .ToSvg();
```

The default `Circular` layout places nodes evenly on the unit circle; no need to supply
coordinates yourself.

## Directed edges with weights

```csharp
GraphEdge[] edges =
[
    new("AAPL", "MSFT", Weight: 0.85, IsDirected: true),
    new("AAPL", "GOOG", Weight: 0.42, IsDirected: true),
    new("MSFT", "GOOG", Weight: 0.55, IsDirected: false),
];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
    {
        s.ShowEdgeWeights = true;
        s.EdgeThicknessScale = 4.0;       // multiplier on per-edge Weight → stroke width
    }))
    .ToSvg();
```

`IsDirected` edges get an arrowhead at the target end, reusing
`ArrowHeadBuilder.FancyArrow` for visual consistency with annotation arrows.

## Per-node colour and size

```csharp
GraphNode[] nodes =
[
    new("AAPL", ColorScalar: 0.2, SizeScalar: 1.5),  // small, dark colour-map sample
    new("MSFT", ColorScalar: 0.5, SizeScalar: 2.5),
    new("GOOG", ColorScalar: 0.8, SizeScalar: 3.0),  // large, bright sample
];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
    {
        s.ColorMap = ColorMaps.Viridis;
        s.NodeRadiusScale = 8.0;          // multiplier on per-node SizeScalar
    }))
    .ToSvg();
```

`ColorScalar` is mapped through `ColorMap` (defaults to Viridis); the renderer clamps to
`[0, 1]`. `SizeScalar` × `NodeRadiusScale` = pixel radius.

## Layouts

### Circular (default)
All nodes on the unit circle at evenly-spaced angles. Ignores edges, ignores any
pre-set `X`/`Y`. O(N), deterministic. Good first pick for any small graph.

### Hierarchical
BFS top-down layering from node 0. Depth becomes Y; within-depth order becomes X
(centred around 0, single-node layer at X=0). Cycles are tolerated via the visited set;
disconnected components stay at depth 0.

```csharp
s.Layout = GraphLayout.Hierarchical;
```

### Manual
Pass-through: each node's pre-set `X` / `Y` is used verbatim. Use this when you have
a custom layout algorithm of your own (e.g. from a t-SNE embedding) and want
NetworkGraph to render-only.

```csharp
GraphNode[] nodes =
[
    new("a", X: 0, Y: 0),
    new("b", X: 1, Y: 1),
    new("c", X: 2, Y: 0),
];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
    {
        s.Layout = GraphLayout.Manual;
    }))
    .ToSvg();
```

### ForceDirected (Fruchterman–Reingold spring-embedder)

Repulsive force `k²/d` between every pair of nodes; attractive spring force `d²/k` on
each edge; `k = √(area/N)`. Random initial positions seeded by `LayoutSeed` (default 0)
for bit-identical reproducibility across runs. Step size cools linearly across iterations.
Final positions are normalised to fit `[-1, 1]²`.

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s =>
    {
        s.Layout = GraphLayout.ForceDirected;
        s.LayoutSeed = 42;                // any int — same seed → identical layout
        s.LayoutIterations = 100;         // default 50; higher = better quality at quadratic cost
        s.ConvergenceThreshold = 0.5;     // optional: early-stop when energy delta drops below
    }))
    .ToSvg();
```

> ⚠️ **Performance cliff.** ForceDirected is **O(N² × iterations)** for the repulsive
> pass. At default 50 iterations the practical limit is roughly **N ≤ 500**. Beyond
> that, render time grows quadratically — N=1000 takes seconds; N=5000 takes minutes.
> For larger graphs **switch to `GraphLayout.Hierarchical`** (O(N + E)) or
> `GraphLayout.Circular` (O(N)). Edge density also matters: dense graphs (E ≈ N²/2)
> add measurable spring-force overhead on top of the always-quadratic repulsion.
> See `Benchmarks/MatPlotLibNet.Benchmarks/NetworkGraphBenchmarks.cs` for the full
> N × edge-density matrix.

**Seeded determinism.** Two figures rendered with the same `LayoutSeed`, `LayoutIterations`,
and `ConvergenceThreshold` produce byte-identical SVG. This is what makes ForceDirected
testable and what unblocks "snapshot" workflows where the layout must be reproducible
across builds.

**Convergence-mode early-stop.** Set `ConvergenceThreshold` to a positive value and the
loop exits as soon as the per-iteration total displacement-energy drops below that
threshold. Sparse / well-separated topologies (e.g. clear cluster structure) converge in
10–20 iterations and benefit; dense uniform graphs typically don't converge below any
reasonable threshold and run the full `LayoutIterations` count.

## DataFrame extension

For edge-list DataFrames, `df.NetworkGraph(...)` derives nodes from the union of
distinct values in the source/target columns (first-seen order):

```csharp
using MatPlotLibNet;
using Microsoft.Data.Analysis;

DataFrame df = ...; // columns: source, target, weight, directed

string svg = df.NetworkGraph(
        edgeFromCol: "source",
        edgeToCol:   "target",
        weightCol:   "weight",
        directedCol: "directed")
    .WithTitle("Asset correlation network")
    .ToSvg();
```

`weightCol` and `directedCol` are optional. Missing nodes (edges that reference an ID
not in the union) are silently skipped at render time — defensive fallback rather
than throwing.

## Configuration reference

| Property | Type | Default | Effect |
|---|---|---|---|
| `Nodes` | `IReadOnlyList<GraphNode>` | (constructor arg) | The graph's nodes. |
| `Edges` | `IReadOnlyList<GraphEdge>` | (constructor arg) | The graph's edges. |
| `Layout` | `GraphLayout` | `Circular` | `Manual` / `Circular` / `Hierarchical` / `ForceDirected`. |
| `ColorMap` | `IColorMap?` | `null` → Viridis | Per-node colour from `ColorScalar`. |
| `ShowNodeLabels` | `bool` | `true` | Render `Label` (or `Id`) next to each node. |
| `ShowEdgeWeights` | `bool` | `false` | Render numeric weight on top of each edge. |
| `EdgeThicknessScale` | `double` | `1.0` | Multiplier on per-edge `Weight` → stroke width. |
| `NodeRadiusScale` | `double` | `5.0` | Multiplier on per-node `SizeScalar` → circle radius. |
| `LayoutSeed` | `int` | `0` | Seed for ForceDirected RNG. Ignored by deterministic layouts. |
| `LayoutIterations` | `int` | `50` | Max iterations for ForceDirected (O(N²) per iteration). Ignored by deterministic layouts. |
| `ConvergenceThreshold` | `double?` | `null` | Optional early-stop for ForceDirected when per-iter energy drops below this. Null = run full `LayoutIterations`. |

## See also

- [Sankey Diagrams](sankey.md) — flow visualisation when edges have explicit thicknesses
- [Treemaps](treemaps.md) — hierarchical containers when the relationship is strictly nesting
- [Dendrograms](dendrograms.md) — hierarchical clustering trees with merge distances
