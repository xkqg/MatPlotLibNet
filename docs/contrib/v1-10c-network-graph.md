# v1.10 — NetworkGraphSeries (Graph category)

Follow-up release after v1.10. Originally specced as Phase 4 of the v1.10 Pair-Selection
pack, but split out so v1.10 can ship the four-phase pack centred on
correlation-and-clustering visualisations (Heatmap extensions, Dendrogram, Clustermap,
**PairGrid**). Network graphs are a different shape — directed/undirected edges, force-
directed layouts, arrowhead glyphs — and deserve their own release window.

**Target:** merge into `main` for v1.10 (after v1.10 is released).

**Coverage gate:** ≥90% line AND ≥90% branch per public class.

---

## What it is

Nodes and edges in 2D. Used for correlation networks (Pearson edge weights), lead-lag flow
(TransferEntropy edge weights, directed), Louvain community visualisation
(node colour = community ID), and minimum spanning trees.

### Visual form

Circles (nodes) connected by line segments (edges). Optional features:
- Node colour from `IColorMap` mapped to a per-node scalar (e.g. cluster ID, centrality).
- Node size proportional to a per-node scalar (e.g. degree, market cap).
- Edge thickness proportional to weight.
- Directed edges → arrowhead at the target end.
- Node labels (optional, default on).

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/Graph/NetworkGraphSeries.cs
public sealed class NetworkGraphSeries : ChartSeries, IColormappable
{
    public IReadOnlyList<GraphNode> Nodes { get; }
    public IReadOnlyList<GraphEdge> Edges { get; }

    public GraphLayout Layout { get; set; } = GraphLayout.ForceDirected;
    public IColorMap? ColorMap { get; set; }

    public bool ShowNodeLabels { get; set; } = true;
    public bool ShowEdgeWeights { get; set; }

    /// <summary>Multiplier on raw edge weight to derive stroke thickness in pixels. Default 1.0.</summary>
    public double EdgeThicknessScale { get; set; } = 1.0;

    /// <summary>Multiplier on raw node-size scalar to derive radius in pixels. Default 5.0.</summary>
    public double NodeRadiusScale { get; set; } = 5.0;

    public NetworkGraphSeries(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }
}

public readonly record struct GraphNode(
    string Id,
    double X,                    // user-supplied coordinate (used when Layout = Manual)
    double Y,
    double ColorScalar = 0.0,    // mapped through ColorMap
    double SizeScalar = 1.0,     // multiplied by NodeRadiusScale
    string? Label = null);

public readonly record struct GraphEdge(
    string From,
    string To,
    double Weight = 1.0,
    bool IsDirected = false);

public enum GraphLayout
{
    Manual,           // use the X/Y coords on each node verbatim
    ForceDirected,    // Fruchterman-Reingold spring-embedder
    Circular,         // nodes on a circle, evenly spaced
    Hierarchical,     // top-down BFS layering (root must be specified — use first node)
}
```

### Layout algorithms

- **Manual** — no computation. Pure pass-through.
- **Circular** — `θ_i = 2π·i/N`, position node `i` at `(cos θ_i, sin θ_i)`. ~10 LOC.
- **ForceDirected** — Fruchterman-Reingold (1991). 50-100 iterations, spring force `k = √(area/N)`, attractive force `f_a = d²/k`, repulsive force `f_r = -k²/d`, temperature-cooled step size. ~80 LOC. **Reference:** Fruchterman, T.M.J., Reingold, E.M. (1991). *Graph drawing by force-directed placement.* Software: Practice and Experience, 21(11), 1129-1164.
- **Hierarchical** — BFS from node 0, assign y by BFS depth, x by within-layer order. ~30 LOC.

For force-directed, prefer determinism: use a seeded `Random` (default seed 0) so SVG output is reproducible for golden tests. Expose `int LayoutSeed { get; set; } = 0` for users who want different starts.

### Renderer

`NetworkGraphSeriesRenderer`:
1. Run the chosen layout algorithm to populate per-node `(X, Y)` (Manual = identity).
2. Compute axes data range from node positions; apply margin.
3. Emit edge `<line>` elements first (so nodes paint over them). For directed edges, append a small arrowhead `<polygon>` at the target end.
4. Emit node `<circle>` elements with fill = ColorMap.Sample(ColorScalar) and radius = SizeScalar × NodeRadiusScale.
5. Emit `<text>` labels if `ShowNodeLabels`.

### `AxesBuilder` extension

```csharp
public static AxesBuilder NetworkGraph(this AxesBuilder axes,
    IReadOnlyList<GraphNode> nodes,
    IReadOnlyList<GraphEdge> edges,
    Action<NetworkGraphSeries>? configure = null)
```

### Tests

- `NetworkGraphSeriesTests` — node/edge collection invariants, enum behaviour, defaults.
- `NetworkGraphLayoutTests` — pure unit tests on each layout algorithm output (deterministic given seed). Force-directed: assert positions are within the axes range and the spring-embedder converges (final step Δ < threshold).
- `NetworkGraphRenderTests` — golden SVGs for each layout × directed/undirected.
- `NetworkGraphSerializationTests` — DTO round-trip including `GraphNode`/`GraphEdge` records.

### Honest scope

~250 LOC renderer + ~200 LOC for the four layouts (force-directed is most of it) + ~150 LOC model + tests. Force-directed convergence behaviour deserves a dedicated test class (`ForceDirectedLayoutTests`) with deterministic-seed assertions on stability; golden SVG alone is not enough.

---

## What this is NOT

- **Not a portfolio-management package.** No asset-selection logic, no return computation, no clustering driver. The series accepts pre-computed inputs (`GraphNode[]`, `GraphEdge[]`) — clustering and statistics belong in user code or in a separate `MatPlotLibNet.Quant` package.
- **Not crypto-specific.** No references to trading, exchanges, or any Ait.RL concepts in the production code or XML doc comments.
- **Not a streaming feature.** Network layout is too expensive per-tick; use static snapshots.
- **Not a new package.** Lands in the existing `MatPlotLibNet` core package under `Models/Series/Graph/`.

---

## Closing checklist before the PR

- TDD: failing test landed first, build green only after implementation.
- ≥90% line AND branch coverage on every new public class.
- `CHANGELOG.md` entry under `[Unreleased]` matching the v1.9.0 / v1.10 indicator-entry detail level.
- New series listed in the cookbook under `docs/cookbook/network-graph.md`.
- API docs regenerated (`docfx`).
- No version bump — that is the maintainer's call.
- No `Co-Authored-By:` trailer on commits.
