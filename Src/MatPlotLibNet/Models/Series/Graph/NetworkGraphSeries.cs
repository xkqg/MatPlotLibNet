// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Nodes-and-edges-in-2D series. Used for correlation networks (Pearson edge
/// weights), lead-lag flow (TransferEntropy directed edges), Louvain community
/// visualisation (node colour = community ID), and minimum spanning trees. Layout is
/// pluggable via <see cref="GraphLayout"/>: <see cref="GraphLayout.Manual"/> keeps the
/// coordinates of the nodes, and <see cref="GraphLayout.Circular"/>,
/// <see cref="GraphLayout.Hierarchical"/>, <see cref="GraphLayout.Arc"/> and <see cref="GraphLayout.ForceDirected"/> place them.</summary>
/// <remarks>The model carries node + edge data and presentation knobs. The nodes are placed by the chosen layout once for
/// each change of the series, so its data range and its drawing read the same positions and the layout runs once for a
/// picture, not twice; rendering lives in <c>NetworkGraphSeriesRenderer</c>. <see cref="ColorMap"/> maps each node's
/// <see cref="GraphNode.ColorScalar"/> to a fill colour; <see cref="EdgeThicknessScale"/>
/// and <see cref="NodeRadiusScale"/> are pixel-space multipliers on the per-node /
/// per-edge scalars.</remarks>
public sealed class NetworkGraphSeries : ChartSeries, IColormappable, ISeriesKind<NetworkGraphSeries>, IGivenTextSize
{
    /// <summary>The graph's nodes. Read-only after construction; mutate via a fresh
    /// series instance.</summary>
    public IReadOnlyList<GraphNode> Nodes { get; }

    /// <summary>The graph's edges. Read-only after construction.</summary>
    public IReadOnlyList<GraphEdge> Edges { get; }

    private const GraphLayout RestingLayout = GraphLayout.Circular;
    /// <summary>Layout algorithm. Default <see cref="RestingLayout"/> — sensible
    /// auto-placement that ignores edges. <see cref="GraphLayout.ForceDirected"/> places connected
    /// nodes closer together.</summary>
    public GraphLayout Layout { get; set => Change(ref field, value); } = RestingLayout;

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    /// <remarks>Maps each node's <see cref="GraphNode.ColorScalar"/> through the colour
    /// map. Defaults to <c>Viridis</c> at render time when null.</remarks>
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    private const bool RestingShowNodeLabels = true;
    /// <summary>Render a label next to each node when true. Falls back to
    /// <see cref="GraphNode.Id"/> when <see cref="GraphNode.Label"/> is null.
    /// Default <see cref="RestingShowNodeLabels"/>.</summary>
    public bool ShowNodeLabels { get; set => Change(ref field, value); } = RestingShowNodeLabels;

    /// <summary>Render the numeric weight on top of each edge when true.
    /// Default <see langword="false"/>.</summary>
    public bool ShowEdgeWeights { get; set => Change(ref field, value); }

    private const double RestingEdgeThicknessScale = 1.0;
    /// <summary>Multiplier on raw <see cref="GraphEdge.Weight"/> to derive stroke
    /// thickness in pixels. Default <see cref="RestingEdgeThicknessScale"/>.</summary>
    public double EdgeThicknessScale { get; set => Change(ref field, value); } = RestingEdgeThicknessScale;

    private const double RestingNodeRadiusScale = 5.0;
    /// <summary>Multiplier on per-node <see cref="GraphNode.SizeScalar"/> to derive node
    /// radius in pixels. Default <see cref="RestingNodeRadiusScale"/>.</summary>
    public double NodeRadiusScale { get; set => Change(ref field, value); } = RestingNodeRadiusScale;

    /// <summary>Seed for layout-algorithm RNG (used by
    /// <see cref="GraphLayout.ForceDirected"/>). Default <c>0</c>; deterministic
    /// across runs at the same seed. The deterministic layouts (Manual/Circular/Hierarchical)
    /// ignore this value.</summary>
    public int LayoutSeed { get; set => Change(ref field, value); }

    /// <summary>Maximum iteration count for the <see cref="GraphLayout.ForceDirected"/>
    /// spring-embedder. Default <see cref="ForceDirectedGraphLayout.DefaultIterations"/>. Higher = better visual quality at quadratic
    /// cost (each iteration is <c>O(N² + E)</c>). Ignored by the deterministic layouts.</summary>
    public int LayoutIterations { get; set => Change(ref field, value); } = ForceDirectedGraphLayout.DefaultIterations;

    /// <summary>Optional energy threshold for <see cref="GraphLayout.ForceDirected"/>
    /// early-stop convergence. When set and the per-iteration total displacement-energy
    /// drops below this value, the layout loop exits before
    /// <see cref="LayoutIterations"/>. Default <see langword="null"/> = run the full
    /// fixed-iteration count. Useful for sparse / well-separated graphs that stabilise
    /// quickly. Ignored by the deterministic layouts.</summary>
    public double? ConvergenceThreshold { get; set => Change(ref field, value); }

    /// <summary>Initializes a new <see cref="NetworkGraphSeries"/>.</summary>
    /// <param name="nodes">The nodes. May be empty (renders an empty figure).</param>
    /// <param name="edges">The edges. May be empty (renders nodes only).</param>
    public NetworkGraphSeries(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    private GraphPlacement? _placement;

    /// <summary>The nodes placed by the layout the series asks for, worked out once for each change of the series and handed
    /// out again while nothing changes: the data range and the drawing read the same placement instead of running the layout
    /// twice for one picture. The nodes and edges are read-only after construction, so a change of any setting is what can
    /// move them.</summary>
    internal GraphPlacement Placement
    {
        get
        {
            long now = LatestChange;
            if (_placement is { } held && held.Stamp == now) return held;
            var algorithm = GraphLayoutAlgorithm.For(this);
            var placed = new GraphPlacement(now, algorithm, algorithm.Place(Nodes, Edges));
            _placement = placed;
            return placed;
        }
    }

    /// <inheritdoc />
    /// <remarks>The bounding box of the placed nodes (placed once for each change of the series), and of the edges where the
    /// layout draws them above the nodes. Empty input returns a null range so callers (axes auto-scale, colorbar) fall back to
    /// their own defaults.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Nodes.Count == 0) return default;

        var (_, algorithm, positioned) = Placement;
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var p in positioned)
        {
            if (p.X < xMin) xMin = p.X;
            if (p.X > xMax) xMax = p.X;
            if (p.Y < yMin) yMin = p.Y;
            if (p.Y > yMax) yMax = p.Y;
        }
        yMax = algorithm.Top(positioned, Edges, yMax);
        // A single node, or nodes on one line, still need a finite range: a flat span opens by half a unit, as the axis' does.
        return DataRangeContribution.Opened(new(xMin, xMax), new(yMin, yMax));
    }

    static string ISeriesKind<NetworkGraphSeries>.Kind => "networkgraph";

    /// <summary>The size of the weight written on each edge (<see cref="ShowEdgeWeights"/>), in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingWeightFontSize"/>. Given no size, the weight written on each edge (<see cref="ShowEdgeWeights"/>)
    /// is the size of the theme's text under the matplotlib themes, as matplotlib draws the text it writes beside its data
    /// (<c>font.size</c>), and <see cref="RestingWeightFontSize"/> under every other theme; a size that is given, the default included, is drawn as given.</summary>
    public double WeightFontSize { get => GivenWeightFontSize ?? RestingWeightFontSize; set => GivenWeightFontSize = value; }

    /// <summary>The size a weight takes under a theme that names none.</summary>
    private const double RestingWeightFontSize = 9;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenWeightFontSize { get; private set => Change(ref field, value); }

    double? IGivenTextSize.GivenTextSize => GivenWeightFontSize;

    double IGivenTextSize.RestingTextSize => RestingWeightFontSize;

    /// <inheritdoc />
    /// <remarks>Default-suppression mirrors the other series — only
    /// non-default values are emitted to keep persisted JSON tight.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        FontSize = GivenWeightFontSize,
        GraphNodes = ToGraphNodeDtos(Nodes),
        GraphEdges = ToGraphEdgeDtos(Edges),
        ColorScale = this.ColorScaleOnTheWire(),
        NetworkGraphLayout             = Layout.OrNullAtRest(RestingLayout)?.ToString(),
        NetworkGraphShowNodeLabels     = ShowNodeLabels.OrNullAtRest(RestingShowNodeLabels),
        NetworkGraphShowEdgeWeights    = ShowEdgeWeights.OrNullAtRest(false),
        NetworkGraphEdgeThicknessScale = EdgeThicknessScale.OrNullAtRest(RestingEdgeThicknessScale),
        NetworkGraphNodeRadiusScale    = NodeRadiusScale.OrNullAtRest(RestingNodeRadiusScale),
        NetworkGraphLayoutSeed         = LayoutSeed.OrNullAtRest(0),
        NetworkGraphLayoutIterations   = LayoutIterations.OrNullAtRest(ForceDirectedGraphLayout.DefaultIterations),
        NetworkGraphConvergenceThreshold = ConvergenceThreshold,
    };

    /// <summary>The reader of a <see cref="NetworkGraphSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static NetworkGraphSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var nodes = FromGraphNodeDtos(dto.GraphNodes);
        var edges = FromGraphEdgeDtos(dto.GraphEdges);
        var s = axes.NetworkGraph(nodes, edges);
        s.RestoreColorScale(dto);
        if (dto.NetworkGraphLayout.WireEnum<GraphLayout>() is { } networkGraphLayout) s.Layout = networkGraphLayout;
        if (dto.NetworkGraphShowNodeLabels is { } networkGraphShowNodeLabels) s.ShowNodeLabels = networkGraphShowNodeLabels;
        if (dto.NetworkGraphShowEdgeWeights is { } networkGraphShowEdgeWeights) s.ShowEdgeWeights = networkGraphShowEdgeWeights;
        if (dto.NetworkGraphEdgeThicknessScale is { } networkGraphEdgeThicknessScale) s.EdgeThicknessScale = networkGraphEdgeThicknessScale;
        if (dto.NetworkGraphNodeRadiusScale is { } networkGraphNodeRadiusScale) s.NodeRadiusScale = networkGraphNodeRadiusScale;
        if (dto.NetworkGraphLayoutSeed is { } networkGraphLayoutSeed) s.LayoutSeed = networkGraphLayoutSeed;
        if (dto.NetworkGraphLayoutIterations is { } networkGraphLayoutIterations) s.LayoutIterations = networkGraphLayoutIterations;
        if (dto.NetworkGraphConvergenceThreshold is { } networkGraphConvergenceThreshold) s.ConvergenceThreshold = networkGraphConvergenceThreshold;
        if (dto.FontSize is { } fontSize) s.WeightFontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The edges, by node id and weight — the topology, which is what the layout draws a picture of.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>(Edges.Count);
        foreach (var edge in Edges)
        {
            rows.Add([DataCell.FromText(edge.From), DataCell.FromText(edge.To), DataCell.FromNumber(edge.Weight)]);
        }

        return new ChartDataTable(null,
            [new("from", DataColumnKind.Text), new("to", DataColumnKind.Text), new(Label ?? "weight")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Converts this list of nodes to its DTO form for JSON serialisation.</summary>
    /// <param name="nodes">The value it works on.</param>
    private static List<GraphNodeDto> ToGraphNodeDtos(IReadOnlyList<Models.Series.GraphNode> nodes)
    {
        var result = new List<GraphNodeDto>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            result.Add(new GraphNodeDto(n.Id, n.X, n.Y, n.ColorScalar, n.SizeScalar, n.Label));
        }
        return result;
    }

    /// <summary>Converts this list of edges to its DTO form for JSON serialisation.</summary>
    /// <param name="edges">The value it works on.</param>
    private static List<GraphEdgeDto> ToGraphEdgeDtos(IReadOnlyList<Models.Series.GraphEdge> edges)
    {
        var result = new List<GraphEdgeDto>(edges.Count);
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            result.Add(new GraphEdgeDto(e.From, e.To, e.Weight, e.IsDirected));
        }
        return result;
    }

    /// <summary>Converts this list of DTOs back to model <see cref="Models.Series.GraphNode"/> records; an
    /// empty array when <see langword="null"/> or empty.</summary>
    /// <param name="dtos">The value it works on.</param>
    private static Models.Series.GraphNode[] FromGraphNodeDtos(List<GraphNodeDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return [];
        }
        var result = new Models.Series.GraphNode[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            var d = dtos[i];
            result[i] = new Models.Series.GraphNode(d.Id, d.X, d.Y, d.ColorScalar, d.SizeScalar, d.Label);
        }
        return result;
    }

    /// <summary>Converts this list of DTOs back to model <see cref="Models.Series.GraphEdge"/> records; an
    /// empty array when <see langword="null"/> or empty.</summary>
    /// <param name="dtos">The value it works on.</param>
    private static Models.Series.GraphEdge[] FromGraphEdgeDtos(List<GraphEdgeDto>? dtos)
    {
        if (dtos is null || dtos.Count == 0)
        {
            return [];
        }
        var result = new Models.Series.GraphEdge[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            var d = dtos[i];
            result[i] = new Models.Series.GraphEdge(d.From, d.To, d.Weight, d.IsDirected);
        }
        return result;
    }
}
