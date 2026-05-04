# v1.10 — Pair-Selection Visualisation Pack (Chart Types)

Second PR of the **v1.10 "Quant & Stats Pack"** — chart-type additions for portfolio / pair-selection workflows. Scope: **three new series types + an extension to `HeatmapSeries`** that any practitioner doing correlation-based asset clustering, dimensional EDA, or hierarchical risk parity will reach for.

**Target:** merge into `main` for v1.10 (after v1-10a statistical diagnostics).

> **Phase 4 update.** The original Phase 4 (`NetworkGraphSeries`) has been split out
> to its own follow-up release. See [`v1-10c-network-graph.md`](v1-10c-network-graph.md).
> Phase 4 in this doc is now **PairGridSeries** — the seaborn `pairplot` idiom — which
> better complements the correlation-and-clustering centre of gravity of v1.10
> (Heatmap → Dendrogram → Clustermap → PairGrid).

**Coverage gate:** ≥90% line AND ≥90% branch per public class.

**Key difference from v1.8/v1.9 indicator tiers:** these are **chart series types** plus property-level extensions. Each new series needs a `*Series` model, a `*SeriesRenderer`, an `AxesBuilder` extension, and tests at each layer. Grep the repo for `HeatmapSeries` + `HeatmapSeriesRenderer` + `BoxSeriesRenderer` before starting — copy-paste the skeleton and adapt.

**Pre-existing assets that must be reused:**

- `Numerics/HierarchicalClustering.cs` — Ward-linkage agglomerative clustering algorithm. Already produces a `TreeNode` tree. Don't reimplement the algorithm; build the renderer on top.
- `Models/Series/HierarchicalSeries.cs` — abstract base that already exposes `TreeNode Root`, `IColorMap? ColorMap`, `bool ShowLabels`. Extend it for the dendrogram.
- `Models/Series/Grid/HeatmapSeries.cs` — extend (not replace) for the cell-annotation and triangular-mask features.
- `TransferEntropy` indicator (Schreiber 2000, ships in v1.9.0) — produces the asymmetric edge weights needed for the lead-lag use case of `NetworkGraphSeries`. The series stays generic; the indicator is the user-facing producer.

---

## Chart types to add

| # | Type | Category | Inputs | Panel | Layering / reuse |
|---|---|---|---|---|---|
| 1 | **`HeatmapSeries.ShowValues` + `MaskMode`** | Extension | (existing) | (existing) | Property-level addition |
| 2 | **DendrogramSeries** | Hierarchical | `TreeNode` from `HierarchicalClustering` | Single subplot | Extends `HierarchicalSeries` |
| 3 | **ClustermapSeries** | Composite Grid | 2D matrix + optional row/col `TreeNode` | Single subplot, multi-panel layout | Composes Heatmap + 2× Dendrogram |
| 4 | **PairGridSeries** | Composite Grid | N variables (jagged) + optional hue groups | Single subplot, N×N panel layout | Composes Histogram / KDE / Scatter sub-renderers |

All four answer the same family of questions a quant asks during pair / asset selection: *which assets cluster together, how stable are those clusters, and what does the joint feature distribution look like across categories?*

---

## 1. HeatmapSeries extension — `ShowValues` + `MaskMode`

Annotated heatmaps and triangular-mask heatmaps are property-level features on the existing `HeatmapSeries`, not separate types. Adding them as new series would duplicate the renderer — wrong shape.

### Properties to add

```csharp
// Src/MatPlotLibNet/Models/Series/Grid/HeatmapSeries.cs
public sealed class HeatmapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    // ... existing members ...

    /// <summary>If true, render each cell's numeric value on top of the colour fill.</summary>
    public bool ShowValues { get; set; }

    /// <summary>Format string used for cell annotations when <see cref="ShowValues"/> is true (e.g. "F2", "P1"). Default: "F2".</summary>
    public string CellValueFormat { get; set; } = "F2";

    /// <summary>Mask mode that hides redundant cells in symmetric matrices (e.g. correlation matrices).</summary>
    public HeatmapMaskMode MaskMode { get; set; } = HeatmapMaskMode.None;

    /// <summary>Optional explicit cell-annotation colour. When null, the renderer picks black/white per cell to maximise contrast against the fill.</summary>
    public Color? CellValueColor { get; set; }
}

public enum HeatmapMaskMode
{
    None,
    UpperTriangle,        // hide cells where col > row (keep diagonal + lower)
    LowerTriangle,        // hide cells where col < row (keep diagonal + upper)
    UpperTriangleStrict,  // hide diagonal too
    LowerTriangleStrict,  // hide diagonal too
}
```

### Renderer changes

In `HeatmapSeriesRenderer.cs`:
- After painting each cell fill, if `ShowValues` is true emit a `<text>` element with the formatted value. Auto-contrast colour: pick the cell-annotation colour with the larger luminance distance from the cell fill (use existing `Color.Luminance()` helper if present; else add one).
- Skip painting cells that fall under the active `MaskMode` predicate. Mask predicate is purely geometric (row/col indices); no data inspection.

### Tests

- `HeatmapSeriesTests` — new cases for `ShowValues`, `CellValueFormat`, `MaskMode` enum behaviour.
- `HeatmapRenderTests` — golden SVG cases for each `MaskMode`, with and without `ShowValues`.
- `HeatmapSerializationTests` — round-trip the new properties through the DTO.

### Why first

This unblocks every realistic correlation-matrix figure (annotated diagonals, no-redundancy lower-triangle). It's also the smallest change — no new files, just property additions on an existing series.

---

## 2. DendrogramSeries

Renders the output of `HierarchicalClustering` as a tree diagram. The clustering algorithm already exists in `Numerics/HierarchicalClustering.cs`; only the visualisation is missing.

### Visual form

Tree of horizontal/vertical line segments. Each leaf is an input observation (label = leaf name). Each internal node is a merge: a horizontal bar at height = merge distance connecting two children. Optional cut line: a horizontal/vertical reference line at a user-chosen distance, with colour-by-cluster-membership for the leaves below.

Orientations:
- **Top** (default) — root at top, leaves on the bottom axis (labels horizontal).
- **Bottom** — root at bottom, leaves on top.
- **Left** — root at left, leaves on the right axis (labels horizontal, tree grows rightward).
- **Right** — mirror of Left.

Left/Right orientations are essential for the row-dendrogram in `ClustermapSeries`.

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/Hierarchical/DendrogramSeries.cs
public sealed class DendrogramSeries : HierarchicalSeries
{
    public DendrogramOrientation Orientation { get; set; } = DendrogramOrientation.Top;

    /// <summary>If set, a reference line is drawn at this distance and leaves are coloured by the K clusters formed by cutting the tree at this height. When null, no cut line is drawn and the tree is rendered in a single colour.</summary>
    public double? CutHeight { get; set; }

    /// <summary>Colour of the cut reference line. Defaults to the axes' foreground colour when null.</summary>
    public Color? CutLineColor { get; set; }

    /// <summary>If true, the colour palette is applied per cluster below the cut line; if false, all branches use a single colour.</summary>
    public bool ColorByCluster { get; set; } = true;

    public DendrogramSeries(TreeNode root) : base(root) { }
}

public enum DendrogramOrientation { Top, Bottom, Left, Right }
```

### Renderer

`DendrogramSeriesRenderer` walks the tree depth-first and emits `<line>` elements. Layout algorithm:

```
1. Assign each leaf a coordinate along the leaf-axis (0..N-1, evenly spaced).
2. For each internal node bottom-up: its leaf-axis coordinate = mean of its children's coordinates.
3. Its merge-axis coordinate = the merge distance from the TreeNode.
4. Emit two segments per merge: a "U" shape — left-vertical, top-horizontal, right-vertical.
```

`Orientation` swaps the leaf/merge axis mapping. For Left/Right, the renderer also rotates leaf labels accordingly.

`CutHeight` colouring: when not null, walk the tree marking nodes whose merge distance is < cut height; the connected components below those nodes are the clusters. Emit each cluster's segments in a distinct colour from the assigned `IColorMap`.

### `AxesBuilder` extension

```csharp
public static AxesBuilder Dendrogram(this AxesBuilder axes, TreeNode root, Action<DendrogramSeries>? configure = null)
```

### Tests

- `DendrogramSeriesTests` — orientation/cut-height/colour-by-cluster property behaviour.
- `DendrogramRenderTests` — golden SVGs for each orientation; cut-line rendering; cluster colouring with a 3-leaf and 7-leaf example.
- `DendrogramSerializationTests` — DTO round-trip.

### Honest scope

This is the smallest of the three new series. The algorithm exists; the renderer is layout + segment emission. ~250 LOC for the renderer + ~150 LOC for the model + tests.

---

## 3. ClustermapSeries

Seaborn-style clustermap: a heatmap with row and/or column dendrograms in the margins. The dendrograms reorder the rows/columns of the data so visually contiguous blocks correspond to clusters.

### Visual form

Three or four panels in a fixed layout:

```
                 [column dendrogram]
[row             [    HEATMAP     ]
 dendrogram]     [                ]
                 [    colorbar    ]
```

Margins are optional: a `ClustermapSeries` with only `RowDendrogram` set hides the column-dendrogram panel and vice versa.

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/Grid/ClustermapSeries.cs
public sealed class ClustermapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public double[,] Data { get; }
    public string[]? RowLabels { get; init; }
    public string[]? ColumnLabels { get; init; }

    /// <summary>Optional dendrogram for the rows. When set, the data rows are reordered by the leaf order of this tree before rendering.</summary>
    public TreeNode? RowDendrogram { get; set; }

    /// <summary>Optional dendrogram for the columns. When set, the data columns are reordered by the leaf order of this tree before rendering.</summary>
    public TreeNode? ColumnDendrogram { get; set; }

    public IColorMap? ColorMap { get; set; }
    public INormalizer? Normalizer { get; set; }

    /// <summary>If true, render each cell's value on top of the colour fill (delegates to the same logic as <see cref="HeatmapSeries.ShowValues"/>).</summary>
    public bool ShowValues { get; set; }

    public string CellValueFormat { get; set; } = "F2";

    /// <summary>Fraction of the figure width allocated to the row dendrogram panel. Default: 0.15.</summary>
    public double RowDendrogramWidth { get; set; } = 0.15;

    /// <summary>Fraction of the figure height allocated to the column dendrogram panel. Default: 0.15.</summary>
    public double ColumnDendrogramHeight { get; set; } = 0.15;

    public ClustermapSeries(double[,] data) { Data = data; }
}
```

### Renderer

`ClustermapSeriesRenderer` is a **composite** that delegates to:
1. `HeatmapSeriesRenderer` for the central heatmap panel (with reordered data).
2. `DendrogramSeriesRenderer` for each margin panel that has a non-null tree.

Reordering: derive the leaf order by post-order traversal of each dendrogram, then build the reordered matrix by index-mapping. **Don't mutate `Data` in place** — produce a new `double[,]` for rendering only.

The composite splits the axes' render area into the four panels (using the `*Width`/`*Height` ratios) and dispatches each sub-render to its panel rectangle. This is similar to how `FigureTemplates.FinancialDashboard` lays out subpanels — reuse that pattern.

### `AxesBuilder` extension

```csharp
public static AxesBuilder Clustermap(this AxesBuilder axes, double[,] data, Action<ClustermapSeries>? configure = null)
```

### Tests

- `ClustermapSeriesTests` — property behaviour (null dendrograms, reorder correctness, panel ratios).
- `ClustermapRenderTests` — golden SVGs for: heatmap-only (both nulls), row-only, column-only, full clustermap.
- `ClustermapSerializationTests` — DTO round-trip.
- `ClustermapReorderingTests` — pure unit test on the leaf-order → row-permutation function (extract this as a private helper or inner type and test it directly).

### Honest scope

Largest of the three. ~400 LOC for the renderer + composition logic, ~200 LOC for the model + reordering helper + tests. The reordering helper is the trickiest piece — make sure it has direct unit tests, not only golden-SVG coverage.

---

## 4. PairGridSeries

Multi-panel scatter matrix — the seaborn `pairplot` / `PairGrid` idiom. Given N
variables, render an N×N grid where the diagonal shows a univariate distribution
of variable *i* and each off-diagonal cell shows a bivariate scatter of *i* vs *j*.
Optional hue groups colour the off-diagonal scatters by category (e.g. cluster ID
from a Louvain partition, species label, regime tag).

### Why this completes the v1.10 arc

`HeatmapSeries.ShowValues` shows correlations as numbers. `DendrogramSeries` /
`ClustermapSeries` show clusters as a tree on top of those correlations. **PairGrid
shows the underlying joint distributions** — the data behind the correlations.
Together the four phases give the practitioner a complete EDA loop:
*correlation matrix → cluster tree → reordered heatmap → drill-down on the actual
feature distributions per cluster.*

### Visual form

N×N grid of small subplots in a single axes:
```
[hist v0] [scatter v0/v1] [scatter v0/v2]
[scatter v1/v0] [hist v1] [scatter v1/v2]
[scatter v2/v0] [scatter v2/v1] [hist v2]
```

When `Triangular = LowerOnly` or `UpperOnly`, the redundant half is left blank
(saves render time on large N where ~half the cells carry no extra information).

### Scope decisions

| Decision | v1.10 initial | Deferred |
|---|---|---|
| Diagonal kind | Histogram (default) + KDE (opt-in) | — |
| Off-diagonal kind | Scatter (default) | Hexbin → v1.10 |
| Triangle modes | Both / LowerOnly / UpperOnly | Asymmetric upper-vs-lower kinds (e.g. KDE upper, scatter lower) → v1.10 |
| Hue grouping | Yes — int[] groups + optional string[] labels | — |

Hexbin is reserved as ordinal `2` on `PairGridOffDiagonalKind` so the v1.10
addition is append-only enum evolution, not a renumber.

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/Grid/PairGridSeries.cs
public sealed class PairGridSeries : ChartSeries
{
    /// <summary>The N variables — each <c>double[]</c> is one variable's samples.
    /// All sub-arrays must have equal length. The N×N grid of cells corresponds
    /// to all (i,j) pairs over <c>Variables.Length</c>.</summary>
    public double[][] Variables { get; }

    /// <summary>Optional axis labels, length must equal <c>Variables.Length</c>.
    /// When null, the renderer falls back to <c>"v0", "v1", …</c>.</summary>
    public string[]? Labels { get; init; }

    /// <summary>Optional grouping vector parallel to each variable's samples.
    /// Length must equal <c>Variables[0].Length</c>. When set the off-diagonal
    /// scatter is split into one series per distinct group, coloured from
    /// <see cref="HuePalette"/>.</summary>
    public int[]? HueGroups { get; init; }

    /// <summary>Optional human-readable labels for the distinct hue groups,
    /// indexed by group ID (i.e. <c>HueLabels[g]</c> is the legend label for
    /// rows where <c>HueGroups[i] == g</c>). When null, the legend renders the
    /// integer group IDs.</summary>
    public string[]? HueLabels { get; init; }

    public PairGridDiagonalKind     DiagonalKind     { get; set; } = PairGridDiagonalKind.Histogram;
    public PairGridOffDiagonalKind  OffDiagonalKind  { get; set; } = PairGridOffDiagonalKind.Scatter;

    /// <summary>Which triangle of the N×N grid to render. <c>Both</c> = full grid (default).</summary>
    public PairGridTriangle Triangular { get; set; } = PairGridTriangle.Both;

    /// <summary>Bin count for the diagonal histograms. Default 20.</summary>
    public int DiagonalBins { get; set; } = 20;

    /// <summary>Marker radius (px) for off-diagonal scatter dots. Default 3.0.</summary>
    public double MarkerSize { get; set; } = 3.0;

    /// <summary>Fraction of the plot bounds reserved as gutter between cells.
    /// Clamped to [0, 0.2].</summary>
    public double CellSpacing { get; set; } = 0.02;

    /// <summary>Optional palette for hue groups. When null the renderer uses
    /// the active theme's PropCycler.</summary>
    public Color[]? HuePalette { get; set; }

    public PairGridSeries(double[][] variables) { /* validate jagged-equal-length */ Variables = variables; }
}

public enum PairGridDiagonalKind
{
    Histogram = 0,    // default
    Kde       = 1,    // smoothed density
    None      = 2,    // suppress diagonal cells (use surrounding scatter only)
}

public enum PairGridOffDiagonalKind
{
    Scatter = 0,      // default
    None    = 1,      // suppress off-diagonal cells (diagonal-only "marginals" view)
    // Hexbin = 2 — reserved for v1.10 (off-diagonal density via hexagonal binning).
}

public enum PairGridTriangle
{
    Both       = 0,   // full N×N grid (default)
    LowerOnly  = 1,   // hide upper triangle (i < j)
    UpperOnly  = 2,   // hide lower triangle (i > j)
}
```

Constructor validates: `Variables.Length ≥ 1`, all sub-arrays equal length,
`Labels.Length == Variables.Length` if non-null, `HueGroups.Length == Variables[0].Length`
if non-null. Invalid input throws `ArgumentException`.

### Layout — pure function

```csharp
// Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/PairGridLayout.cs
internal static class PairGridLayout
{
    /// <summary>Sub-pixel suppression gate matching the Clustermap precedent.
    /// Cells whose pixel size falls below this are skipped instead of rendered
    /// as one-pixel slivers.</summary>
    internal const double MinPanelPx = 4.0;

    /// <summary>Splits the plot bounds into an <paramref name="n"/>×<paramref name="n"/>
    /// grid of cell rectangles with a uniform <paramref name="cellSpacing"/>
    /// gutter (as a fraction of total bounds, clamped at the call site).</summary>
    internal static Rect[,] ComputeCellRects(Rect plotBounds, int n, double cellSpacing);
}
```

Pure function — direct unit tests on output, no SVG dependency.

### Renderer

```csharp
// Src/MatPlotLibNet/Rendering/SeriesRenderers/Grid/PairGridSeriesRenderer.cs
internal sealed class PairGridSeriesRenderer : SeriesRenderer<PairGridSeries>
{
    public override void Render(PairGridSeries series)
    {
        int n = series.Variables.Length;
        var cells = PairGridLayout.ComputeCellRects(Context.Area.PlotBounds, n, series.CellSpacing);

        for (int r = 0; r < n; r++)
        for (int c = 0; c < n; c++)
        {
            if (!ShouldRender(r, c, series.Triangular)) continue;
            if (cells[r, c].Width < PairGridLayout.MinPanelPx) continue;
            if (cells[r, c].Height < PairGridLayout.MinPanelPx) continue;

            if (r == c) RenderDiagonal(series, r, cells[r, c]);
            else        RenderOffDiagonal(series, r, c, cells[r, c]);
        }
    }
}
```

`ShouldRender(r, c, tri)`:
- `Both`      → always true
- `LowerOnly` → `r >= c`
- `UpperOnly` → `r <= c`

Sub-renderers fresh-instantiated per cell with `Context with { Area = cellArea }`,
matching the W2 design call from Phase 3 — each cell needs a different
`RenderArea`, so caching the sub-renderer instance at composite-renderer level
is incorrect.

### Hue handling

When `HueGroups` is non-null:
- **Off-diagonal:** distinct group IDs → one `ScatterSeries` per group, colour from
  `HuePalette[g % palette.Length]` (theme PropCycler when palette is null).
- **Diagonal histograms:** stacked-overlapping per group with `Alpha = 0.6`
  (matches the existing `df.Hist(hue:)` precedent in DataFrame extensions).
- **Diagonal KDE:** one curve per group, same palette.

`HueLabels[g]` is forwarded to each sub-series' `Label` property so the figure-level
legend renders human-readable strings.

### Serialization

DTO additions on `SeriesDto`:
```csharp
public List<List<double>>? Variables          { get; init; }
public string[]?           PairGridLabels     { get; init; }
public int[]?              PairGridHueGroups  { get; init; }
public string[]?           PairGridHueLabels  { get; init; }
public string?             PairGridDiagonal   { get; init; }   // enum name
public string?             PairGridOffDiagonal{ get; init; }   // enum name
public string?             PairGridTriangular { get; init; }   // enum name
public int?                PairGridDiagonalBins   { get; init; }
public double?             PairGridMarkerSize     { get; init; }
public double?             PairGridCellSpacing    { get; init; }
```

`HuePalette` is **not** serialised — same project-wide convention as `Normalizer` and
`HierarchicalSeries.RowTree/ColumnTree`.

`ChartSerializer.CreatePairGrid(Axes, SeriesDto)` is the named factory registered as
`Register("pairgrid", ChartSerializer.CreatePairGrid)` (W8 named-method precedent).

### `AxesBuilder` / `FigureBuilder` shortcuts

```csharp
public static PairGridSeries PairGrid(this AxesBuilder axes,
    double[][] variables,
    Action<PairGridSeries>? configure = null);

public static FigureBuilder PairGrid(this FigureBuilder fig,
    double[][] variables,
    Action<PairGridSeries>? configure = null);
```

### `DataFrame` extension

```csharp
public static FigureBuilder PairGrid(this MsDataFrame df,
    string[] columns,
    string?  hue     = null,    // optional categorical column → HueGroups + HueLabels
    Color[]? palette = null,
    Action<PairGridSeries>? configure = null);
```

When `hue` is supplied, the extension reads it via `StringCol(hue)`, builds the
distinct-value index map, and populates **both** `HueGroups` (int IDs) and
`HueLabels` (the original strings in order). This is precisely why `HueLabels`
exists on the model — the DataFrame entry point would otherwise lose the human
readable group names.

### Tests

- `PairGridLayoutTests` — pure-function unit tests on `ComputeCellRects` for
  N = 1, 2, 3, 10; spacing = 0, 0.02, 0.2; plus the `MinPanelPx` suppression path.
- `PairGridSeriesTests` — defaults, validation throws, clamping, label fallback,
  `ToSeriesDto` round-trip-of-properties.
- `PairGridRenderTests` — SVG output for each combination of:
  Diagonal × OffDiagonal × Triangular (≥6 cases), with and without `HueGroups`.
- `PairGridSerializationTests` — `CreatePairGrid` round-trip: every configurable
  property + a Normalizer-equivalent test confirming `HuePalette` is intentionally
  not preserved.
- `DataFrameFigureExtensionsTests.PairGrid_*` — 7 tests mirroring the v1.10 Clustermap
  extension precedent (default scatter; with hue produces correct group count and labels;
  unknown column throws; chainable; etc.).

### Honest scope

~250 LOC model (constructor validation is the bulk) + ~350 LOC renderer + ~150 LOC
layout + ~700 LOC tests. Comparable to Clustermap; simpler in that there's no
tree-walking, more involved because of the N×N dispatch and per-cell hue series.

### TDD ordering (one PR end-to-end)

1. `PairGridLayout.ComputeCellRects` (pure)
2. `PairGridSeries` model (defaults, validation, `ToSeriesDto`)
3. Visitor + `AxesBuilder`/`FigureBuilder` shortcuts
4. Renderer skeleton — diagonal histograms only (`OffDiagonalKind = None`)
5. Off-diagonal scatter (`Triangular = Both`)
6. `Triangular = LowerOnly` / `UpperOnly` paths
7. `DiagonalKind = Kde` path
8. `HueGroups` → per-group scatters + per-group histogram overlay; `HueLabels` propagation
9. Serialization: `CreatePairGrid` + DTO + registry
10. DataFrame extension: `df.PairGrid(string[], hue: string?)` + 7 tests
11. Cookbook (`docs/cookbook/pairplot.md`) + wiki Chart-Types entry + counts (76 → 77)

### API example

```csharp
double[][] vars = [petalLength, petalWidth, sepalLength, sepalWidth];
string[]   labs = ["Petal L", "Petal W", "Sepal L", "Sepal W"];
int[]      hue  = species.Select(s => (int)s).ToArray();
string[]   hLab = ["Setosa", "Versicolor", "Virginica"];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.Labels      = labs;
        s.HueGroups   = hue;
        s.HueLabels   = hLab;
        s.DiagonalKind = PairGridDiagonalKind.Kde;
        s.Triangular   = PairGridTriangle.LowerOnly;
        s.MarkerSize   = 4;
    }))
    .ToSvg();

// or via DataFrame — hue: "species" auto-fills both HueGroups and HueLabels
df.PairGrid(["petal_l", "petal_w", "sepal_l", "sepal_w"], hue: "species").ToSvg();
```

---

## Implementation order

```
1. HeatmapSeries.ShowValues + MaskMode      ✅ (v1.10 Phase 1, shipped)
2. DendrogramSeries                          ✅ (v1.10 Phase 2, shipped)
3. ClustermapSeries                          ✅ (v1.10 Phase 3, shipped)
4. PairGridSeries                            ⏳ (v1.10 Phase 4, in planning)
```

Each step is a separate PR. Don't bundle. (Original Phase 4 — `NetworkGraphSeries` —
moved to [v1.10](v1-10c-network-graph.md).)

---

## What this is NOT

- **Not a portfolio-management package.** No asset-selection logic, no return computation, no clustering driver. The series accept pre-computed inputs (`TreeNode`, `double[,]`, `double[][]`) — clustering, dimensional reduction, and statistics belong in user code or in a separate `MatPlotLibNet.Quant` package.
- **Not crypto-specific.** No references to trading, exchanges, or any Ait.RL concepts in the production code or XML doc comments. Consumer-side examples in `Samples/` may use a financial dataset for illustration, but the series themselves stay generic.
- **Not a streaming feature.** Cluster computation and N×N pair-grid layout are too expensive per-tick; both are periodic / offline workflows. No `Streaming*` variants of these series.
- **Not new packages.** All four additions land in the existing `MatPlotLibNet` core package. The file structure (`Models/Series/Grid/` for ClustermapSeries and PairGridSeries; `Models/Series/Hierarchical/` for DendrogramSeries) handles the categorisation.

---

## Recommended workflow (Claude-assisted)

This spec is intended to be implementable by a Claude Code session running in `C:\Ait\MatPlotLibNet`. The recommended chain for each of the four phases:

### 1. Planning — Plan agent

Start each phase with:

> *"Maak een implementatieplan voor `docs/contrib/v1-10b-pair-selection-series.md`, fase N (HeatmapSeries.ShowValues + MaskMode | DendrogramSeries | ClustermapSeries | PairGridSeries)."*

Use the **Plan agent** (software-architect). Do **not** run a code-review pass on the spec
— review tooling is built to inspect *existing C# code*, not Markdown specs. A review pass
on a spec produces shallow feedback about the spec itself, not the engineering it describes.

### 2. Implementation — TDD per phase

Each phase is a separate PR (CONTRIBUTING.md rule: don't bundle).

- Red: write failing tests for the public surface (model, renderer, serialization).
- Green: implement the minimum for tests to pass.
- Refactor: extract helpers, wire into `AxesBuilder`, update cookbook.

Stay within the phase. Do not pre-implement helpers needed by a later phase — that's a TDD drift signal.

### 3. Code review per phase, after implementation

Before merging each PR run a structured code review on the actually-written code: SOLID,
DRY, dead code, design issues, ≥90/90 coverage gate verification. Run it on **the
actually-written code**, not the spec.

### 4. Knowledge-graph update — after all 4 phases

Once all four phases are merged:

```
/graphify C:\Ait\MatPlotLibNet --update
```

This refreshes the source-code graph (currently 11890 nodes / 14113 edges / 935 communities, per `CLAUDE.md`) so future Claude sessions can use `query_graph`, `god_nodes`, `get_neighbors`, `shortest_path`, `get_community` to navigate the new series. Do not run mid-implementation — wait for all four to land. The graph rebuild is non-trivial and only valuable after a coherent set of changes.

### Workflow summary

```
Plan agent           — implementatieplan per fase
   ↓
TDD implementatie    — red → green → refactor (per fase = PR)
   ↓
Code review          — review per fase voor merge
   ↓
merge → repeat for next phase
   ↓ (after all 4 phases merged)
/graphify ... --update — knowledge graph sync
```

No review on the spec; review on the implementations. No graphify mid-flight; graphify once after the v1.10 body of work is complete.

---

## Closing checklist before each PR

- TDD: failing test landed first, build green only after implementation.
- ≥90% line AND branch coverage on every new public class.
- `CHANGELOG.md` entry under `[Unreleased]` with the same level of detail as the v1.9.0 indicator entries.
- New series listed in the cookbook under `docs/cookbook/` (one new MD per series following the pattern of existing entries).
- API docs regenerated (`docfx`).
- No version bump — that is the maintainer's call.
- No `Co-Authored-By:` trailer on commits.
