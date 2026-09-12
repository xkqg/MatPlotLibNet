# Network Graph

NetworkGraph draws nodes and edges in 2D. Use it for correlation networks, lead-lag
flows, Louvain community visualisation, and minimum spanning trees. Three deterministic
layouts ship in v1.10 PR 1: `Manual` (uses the coordinates you pass), `Circular` (unit
circle) and `Hierarchical` (BFS top-down). `ForceDirected` (Fruchterman–Reingold) is
reserved at enum ordinal `1` and lands in PR 2.

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

The default `Circular` layout places nodes evenly on the unit circle. You do not have to
supply coordinates yourself.

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

An edge with `IsDirected` set gets an arrowhead at the target end. It reuses
`ArrowHeadBuilder.FancyArrow`, so it looks the same as an annotation arrow.

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

The renderer maps `ColorScalar` through `ColorMap`, which defaults to Viridis, and clamps
the value to `[0, 1]`. A node's pixel radius is `SizeScalar` multiplied by
`NodeRadiusScale`.

## Layouts

### Circular (default)
This layout puts every node on the unit circle at evenly spaced angles. It ignores the
edges and it ignores any pre-set `X`/`Y`. It is O(N) and deterministic. It is a good first
pick for any small graph.

### Hierarchical
This layout walks the graph breadth-first from node 0 and lays out one layer per depth,
top down. Depth becomes Y. The order within a depth becomes X, centred around 0, with a
single-node layer at X=0. A visited set handles cycles. Disconnected components stay
at depth 0.

```csharp
s.Layout = GraphLayout.Hierarchical;
```

### Manual
This layout uses each node's pre-set `X` / `Y` exactly as you set it. Use it when you have
a custom layout algorithm of your own, for example from a t-SNE embedding, and want
NetworkGraph only to render.

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

Every pair of nodes pushes apart with a repulsive force of `k²/d`. Every edge pulls its
two nodes together with a spring force of `d²/k`. Here `k = √(area/N)`. The starting
positions are random and seeded by `LayoutSeed` (default 0), so repeated runs are
bit-identical. The step size cools linearly across the iterations. The final positions are
normalised to fit `[-1, 1]²`.

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

> ⚠️ **Performance limit.** ForceDirected is **O(N² × iterations)** for the repulsive
> pass. At default 50 iterations the practical limit is roughly **N ≤ 500**. Beyond
> that, render time grows quadratically: N=1000 takes seconds, N=5000 takes minutes.
> For larger graphs, **switch to `GraphLayout.Hierarchical`** (O(N + E)) or
> `GraphLayout.Circular` (O(N)). Edge density matters too: dense graphs (E ≈ N²/2)
> add measurable spring-force overhead on top of the repulsion, which is always quadratic.
> See `Benchmarks/MatPlotLibNet.Benchmarks/NetworkGraphBenchmarks.cs` for the full
> N × edge-density matrix.

**Seeded determinism.** Two figures rendered with the same `LayoutSeed`, `LayoutIterations`
and `ConvergenceThreshold` produce byte-identical SVG. That makes ForceDirected testable,
and it lets you use snapshot workflows that need the same layout on every build.

**Convergence-mode early-stop.** Set `ConvergenceThreshold` to a positive value and the
loop exits as soon as the total displacement energy of an iteration drops below that
threshold. Sparse or well-separated topologies, such as a clear cluster structure,
converge in 10–20 iterations and benefit from this. Dense uniform graphs usually do not
converge below any reasonable threshold and run the full `LayoutIterations` count.

## DataFrame extension

For an edge-list DataFrame, `df.NetworkGraph(...)` builds the nodes from the union of the
distinct values in the source and target columns, in first-seen order:

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

`weightCol` and `directedCol` are optional. Missing nodes, meaning edges that reference an
ID that is not in the union, are silently skipped at render time. This is a defensive
fallback: the renderer does not throw.

## Configuration reference

| Property | Type | Default | Effect |
|---|---|---|---|
| `Nodes` | `IReadOnlyList<GraphNode>` | (constructor arg) | The graph's nodes. |
| `Edges` | `IReadOnlyList<GraphEdge>` | (constructor arg) | The graph's edges. |
| `Layout` | `GraphLayout` | `Circular` | `Manual` / `Circular` / `Hierarchical` / `ForceDirected`. |
| `ColorMap` | `IColorMap?` | `null`, meaning Viridis | Per-node colour from `ColorScalar`. |
| `ShowNodeLabels` | `bool` | `true` | Render `Label` (or `Id`) next to each node. |
| `ShowEdgeWeights` | `bool` | `false` | Render numeric weight on top of each edge. |
| `EdgeThicknessScale` | `double` | `1.0` | Multiplier on per-edge `Weight` to get the stroke width. |
| `NodeRadiusScale` | `double` | `5.0` | Multiplier on per-node `SizeScalar` to get the circle radius. |
| `LayoutSeed` | `int` | `0` | Seed for ForceDirected RNG. Ignored by deterministic layouts. |
| `LayoutIterations` | `int` | `50` | Max iterations for ForceDirected (O(N²) per iteration). Ignored by deterministic layouts. |
| `ConvergenceThreshold` | `double?` | `null` | Optional early-stop for ForceDirected when the per-iteration energy drops below this. Null runs the full `LayoutIterations`. |

## See also

- [Sankey Diagrams](sankey.md) — flow visualisation when edges have explicit thicknesses
- [Treemaps](treemaps.md) — hierarchical containers when the relationship is strictly nesting
- [Dendrograms](dendrograms.md) — hierarchical clustering trees with merge distances
