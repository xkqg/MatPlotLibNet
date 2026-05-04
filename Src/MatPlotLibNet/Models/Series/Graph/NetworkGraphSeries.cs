// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Nodes-and-edges-in-2D series. Used for correlation networks (Pearson edge
/// weights), lead-lag flow (TransferEntropy directed edges), Louvain community
/// visualisation (node colour = community ID), and minimum spanning trees. Layout is
/// pluggable via <see cref="GraphLayout"/>; PR 1 of v1.10 ships
/// <see cref="GraphLayout.Manual"/>, <see cref="GraphLayout.Circular"/>, and
/// <see cref="GraphLayout.Hierarchical"/>; <see cref="GraphLayout.ForceDirected"/>
/// is reserved for PR 2.</summary>
/// <remarks>The model carries node + edge data and presentation knobs; layout
/// computation lives in <c>NetworkGraphLayouts</c> and rendering in
/// <c>NetworkGraphSeriesRenderer</c>. <see cref="ColorMap"/> maps each node's
/// <see cref="GraphNode.ColorScalar"/> to a fill colour; <see cref="EdgeThicknessScale"/>
/// and <see cref="NodeRadiusScale"/> are pixel-space multipliers on the per-node /
/// per-edge scalars.</remarks>
public sealed class NetworkGraphSeries : ChartSeries, IColormappable
{
    /// <summary>The graph's nodes. Read-only after construction; mutate via a fresh
    /// series instance.</summary>
    public IReadOnlyList<GraphNode> Nodes { get; }

    /// <summary>The graph's edges. Read-only after construction.</summary>
    public IReadOnlyList<GraphEdge> Edges { get; }

    /// <summary>Layout algorithm. Default <see cref="GraphLayout.Circular"/> — sensible
    /// auto-placement that ignores edges. PR 2 may switch the default to
    /// <see cref="GraphLayout.ForceDirected"/> once that layout is active.</summary>
    public GraphLayout Layout { get; set; } = GraphLayout.Circular;

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    /// <remarks>Maps each node's <see cref="GraphNode.ColorScalar"/> through the colour
    /// map. Defaults to <c>Viridis</c> at render time when null.</remarks>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Render a label next to each node when true. Falls back to
    /// <see cref="GraphNode.Id"/> when <see cref="GraphNode.Label"/> is null.
    /// Default <see langword="true"/>.</summary>
    public bool ShowNodeLabels { get; set; } = true;

    /// <summary>Render the numeric weight on top of each edge when true.
    /// Default <see langword="false"/>.</summary>
    public bool ShowEdgeWeights { get; set; }

    /// <summary>Multiplier on raw <see cref="GraphEdge.Weight"/> to derive stroke
    /// thickness in pixels. Default <c>1.0</c>.</summary>
    public double EdgeThicknessScale { get; set; } = 1.0;

    /// <summary>Multiplier on per-node <see cref="GraphNode.SizeScalar"/> to derive node
    /// radius in pixels. Default <c>5.0</c>.</summary>
    public double NodeRadiusScale { get; set; } = 5.0;

    /// <summary>Seed for layout-algorithm RNG (used by
    /// <see cref="GraphLayout.ForceDirected"/>). Default <c>0</c>; deterministic
    /// across runs at the same seed. The deterministic layouts (Manual/Circular/Hierarchical)
    /// ignore this value.</summary>
    public int LayoutSeed { get; set; }

    /// <summary>Maximum iteration count for the <see cref="GraphLayout.ForceDirected"/>
    /// spring-embedder. Default <c>50</c>. Higher = better visual quality at quadratic
    /// cost (each iteration is <c>O(N² + E)</c>). Ignored by the deterministic layouts.</summary>
    public int LayoutIterations { get; set; } = NetworkGraphLayouts.DefaultForceDirectedIterations;

    /// <summary>Optional energy threshold for <see cref="GraphLayout.ForceDirected"/>
    /// early-stop convergence. When set and the per-iteration total displacement-energy
    /// drops below this value, the layout loop exits before
    /// <see cref="LayoutIterations"/>. Default <see langword="null"/> = run the full
    /// fixed-iteration count. Useful for sparse / well-separated graphs that stabilise
    /// quickly. Ignored by the deterministic layouts.</summary>
    public double? ConvergenceThreshold { get; set; }

    /// <summary>Initializes a new <see cref="NetworkGraphSeries"/>.</summary>
    /// <param name="nodes">The nodes. May be empty (renders an empty figure).</param>
    /// <param name="edges">The edges. May be empty (renders nodes only).</param>
    public NetworkGraphSeries(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <inheritdoc />
    /// <remarks>Computes the bounding box of node positions after applying the active
    /// layout. Empty input returns a null range so callers (axes auto-scale, colorbar)
    /// fall back to their own defaults.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Nodes.Count == 0) return new(null, null, null, null);

        var positioned = NetworkGraphLayouts.Apply(Layout, Nodes, Edges,
            LayoutSeed, LayoutIterations, ConvergenceThreshold);
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var p in positioned)
        {
            if (p.X < xMin) xMin = p.X;
            if (p.X > xMax) xMax = p.X;
            if (p.Y < yMin) yMin = p.Y;
            if (p.Y > yMax) yMax = p.Y;
        }
        // Degenerate guard: single node or all-coincident nodes still need a finite range.
        if (xMin == xMax) { xMin -= 0.5; xMax += 0.5; }
        if (yMin == yMax) { yMin -= 0.5; yMax += 0.5; }
        return new(xMin, xMax, yMin, yMax);
    }

    /// <inheritdoc />
    /// <remarks>Default-suppression mirrors other series in the v1.10 pack — only
    /// non-default values are emitted to keep persisted JSON tight.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "networkgraph",
        GraphNodes = ChartSerializer.ToGraphNodeDtos(Nodes),
        GraphEdges = ChartSerializer.ToGraphEdgeDtos(Edges),
        ColorMapName = ColorMap?.Name,
        NetworkGraphLayout             = Layout != GraphLayout.Circular ? Layout.ToString() : null,
        NetworkGraphShowNodeLabels     = !ShowNodeLabels ? false : null,
        NetworkGraphShowEdgeWeights    = ShowEdgeWeights ? true  : null,
        NetworkGraphEdgeThicknessScale = EdgeThicknessScale != 1.0 ? EdgeThicknessScale : null,
        NetworkGraphNodeRadiusScale    = NodeRadiusScale    != 5.0 ? NodeRadiusScale    : null,
        NetworkGraphLayoutSeed         = LayoutSeed         != 0   ? LayoutSeed         : null,
        NetworkGraphLayoutIterations   = LayoutIterations != NetworkGraphLayouts.DefaultForceDirectedIterations ? LayoutIterations : null,
        NetworkGraphConvergenceThreshold = ConvergenceThreshold,
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
