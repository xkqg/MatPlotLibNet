// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Horizontal alignment strategy for Sankey node columns.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum SankeyNodeAlignment
{
    /// <summary>Columns fill the plot width evenly (default, matches D3 <c>sankeyJustify</c>).</summary>
    Justify = 0,
    /// <summary>All nodes pulled as far left as their topology allows.</summary>
    Left = 1,
    /// <summary>All nodes pulled as far right as their topology allows.</summary>
    Right = 2,
    /// <summary>Nodes centred horizontally between their earliest and latest possible column.</summary>
    Center = 3,
}

/// <summary>Colour resolution strategy for Sankey links.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum SankeyLinkColorMode
{
    /// <summary>Link filled with the source node's colour (classic flat Sankey).</summary>
    Source = 0,
    /// <summary>Link filled with the target node's colour.</summary>
    Target = 1,
    /// <summary>Link filled with a horizontal <c>&lt;linearGradient&gt;</c> from source → target colour.</summary>
    Gradient = 2,
}

/// <summary>Overall orientation of a Sankey diagram.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum SankeyOrientation
{
    /// <summary>Columns span left-to-right, stems flow horizontally (default).</summary>
    Horizontal = 0,
    /// <summary>Columns span top-to-bottom, stems flow vertically.</summary>
    Vertical = 1,
}

/// <summary>Represents a Sankey diagram that renders flows between nodes as curved links.</summary>
public sealed class SankeySeries : ChartSeries, ISeriesKind<SankeySeries>
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <inheritdoc />
    internal override bool KeepsAxesBackground => true;

    /// <summary>The nodes of the diagram, drawn as labelled rectangles in columns; the links refer to them by
    /// index.</summary>
    public IReadOnlyList<SankeyNode> Nodes { get; }

    /// <summary>The flows between the nodes: each link names its source and target node by index into
    /// <see cref="Nodes"/>, and its value sets how thick the link is drawn.</summary>
    public IReadOnlyList<SankeyLink> Links { get; }

    private const double RestingNodeWidth = 20;
    /// <summary>Width of each node rectangle along the direction of the flow, in pixels. Default <see cref="RestingNodeWidth"/>.</summary>
    public double NodeWidth { get; set => Change(ref field, value); } = RestingNodeWidth;

    private const double RestingNodePadding = 10;
    /// <summary>The gap kept between neighbouring nodes of one column, in pixels. Default <see cref="RestingNodePadding"/>.</summary>
    public double NodePadding { get; set => Change(ref field, value); } = RestingNodePadding;

    private const double RestingLinkAlpha = 0.4;
    /// <summary>Opacity of the links, from 0 (invisible) to 1 (solid). Default <see cref="RestingLinkAlpha"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double LinkAlpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(LinkAlpha))); } = RestingLinkAlpha;

    private const SankeyNodeAlignment RestingNodeAlignment = SankeyNodeAlignment.Justify;
    /// <summary>Horizontal alignment strategy for node columns. Default <see cref="RestingNodeAlignment"/>.</summary>
    public SankeyNodeAlignment NodeAlignment { get; set => Change(ref field, value); } = RestingNodeAlignment;

    private const int RestingIterations = 6;
    /// <summary>Number of vertical-relaxation passes used to minimise link crossings. Each pass
    /// shifts every node toward the value-weighted average of its neighbour positions (matplotlib
    /// / D3 sankey use <see cref="RestingIterations"/> by default). Set to 0 to disable relaxation and keep the greedy initial
    /// vertical order.</summary>
    public int Iterations { get; set => Change(ref field, value); } = RestingIterations;

    private const SankeyLinkColorMode RestingLinkColorMode = SankeyLinkColorMode.Gradient;
    /// <summary>Colour resolution strategy for links. Default <see cref="RestingLinkColorMode"/>
    /// produces presentation-quality source → target colour blends via SVG <c>&lt;linearGradient&gt;</c>.</summary>
    public SankeyLinkColorMode LinkColorMode { get; set => Change(ref field, value); } = RestingLinkColorMode;

    private const SankeyOrientation RestingOrient = SankeyOrientation.Horizontal;
    /// <summary>Diagram orientation. Default <see cref="RestingOrient"/>.</summary>
    public SankeyOrientation Orient { get; set => Change(ref field, value); } = RestingOrient;

    /// <summary>When true, node labels whose measured width fits inside the node rectangle are
    /// drawn centred inside the rect instead of to the outer side. It is off by default: the default
    /// <see cref="NodeWidth"/> of 20 px is almost always too narrow to host text, so a figure that
    /// widens its nodes asks for it. Set <c>NodeWidth</c> to 60+ px before enabling. A label inside a node is written in the theme's text colour
    /// when that can be read against the node's colour, and in black or white, whichever stands out, when it cannot; a
    /// node's own <see cref="SankeyNode.SubLabelColor"/> still wins for its sub-label. Default <see langword="false"/>.</summary>
    public bool InsideLabels { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance with the specified nodes and links.</summary>
    public SankeySeries(SankeyNode[] nodes, SankeyLink[] links)
    {
        Nodes = nodes;
        Links = links;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

    static string ISeriesKind<SankeySeries>.Kind => "sankey";

    /// <summary>The size of the name of each node, in pixels (the unit of <see cref="Font.Size"/>). Default <see cref="RestingFontSize"/>. Given no size,
    /// a name is the theme's text capped at <see cref="RestingFontSize"/> under every theme, so a long name stays inside its band (matplotlib's
    /// own Sankey is a flow diagram of another shape, with nothing to match); a size that is given, the default included, is drawn as given.</summary>
    public double FontSize { get => GivenFontSize ?? RestingFontSize; set => GivenFontSize = value; }

    /// <summary>The largest a node's name is drawn at when it names no size.</summary>
    private const double RestingFontSize = 10;

    /// <summary>The size that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFontSize { get; private set => Change(ref field, value); }

    /// <summary>The size a node's name is drawn at under <paramref name="theme"/>: the one given, else the theme's text capped at
    /// <see cref="RestingFontSize"/>.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
    internal double LabelSizeUnder(Theme theme) => GivenFontSize ?? Math.Min(RestingFontSize, theme.DataTextFont().Size);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        FontSize = GivenFontSize,
        NodeWidth = NodeWidth.OrNullAtRest(RestingNodeWidth),
        NodePadding = NodePadding.OrNullAtRest(RestingNodePadding),
        LinkAlpha = LinkAlpha.OrNullAtRest(RestingLinkAlpha),
        NodeAlignment = NodeAlignment.OrNullAtRest(RestingNodeAlignment),
        Iterations = Iterations.OrNullAtRest(RestingIterations),
        LinkColorMode = LinkColorMode.OrNullAtRest(RestingLinkColorMode),
        SankeyOrientation = Orient.OrNullAtRest(RestingOrient),
        InsideLabels = InsideLabels.OrNullAtRest(false),
        SankeyNodes = [.. Nodes.Select(n => new SankeyNodeDto(n.Label, n.Color, n.SubLabel, n.SubLabelColor, n.Column))],
        SankeyLinks = [.. Links.Select(l => new SankeyLinkDto(l.SourceIndex, l.TargetIndex, l.Value))],
    };

    /// <summary>The reader of a <see cref="SankeySeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SankeySeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var nodes = (dto.SankeyNodes ?? [])
            .Select(n => new SankeyNode(n.Label, n.Color, n.SubLabel, n.SubLabelColor, n.Column))
            .ToArray();
        var links = (dto.SankeyLinks ?? [])
            .Select(l => new SankeyLink(l.SourceIndex, l.TargetIndex, l.Value))
            .ToArray();
        var s = axes.Sankey(nodes.Length > 0 ? nodes : [new SankeyNode("A")], links);
        if (dto.NodeWidth is { } nodeWidth) s.NodeWidth = nodeWidth;
        if (dto.NodePadding is { } nodePadding) s.NodePadding = nodePadding;
        if (dto.LinkAlpha is { } linkAlpha) s.LinkAlpha = linkAlpha;
        if (dto.NodeAlignment is { } nodeAlignment) s.NodeAlignment = nodeAlignment;
        if (dto.Iterations is { } iterations) s.Iterations = iterations;
        if (dto.LinkColorMode is { } linkColorMode) s.LinkColorMode = linkColorMode;
        if (dto.SankeyOrientation is { } sankeyOrientation) s.Orient = sankeyOrientation;
        if (dto.InsideLabels is { } insideLabels) s.InsideLabels = insideLabels;
        if (dto.FontSize is { } fontSize) s.FontSize = fontSize;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The links, by the names of the nodes they join: a Sankey diagram IS its flows, and a reader
    /// needs "A to B, 10", not a node list and an index.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        string[] names = [.. Nodes.Select(node => node.Label)];
        var rows = new List<IReadOnlyList<DataCell>>(Links.Count);
        foreach (var link in Links)
        {
            rows.Add(
            [
                DataCell.FromText(NodeName(link.SourceIndex)),
                DataCell.FromText(NodeName(link.TargetIndex)),
                DataCell.FromNumber(link.Value),
            ]);
        }

        return new ChartDataTable(null,
            [new("source", DataColumnKind.Text), new("target", DataColumnKind.Text), new(Label ?? "value")], rows);

        string NodeName(int index) => names.ElementOr(index, static n => n.InvariantText());
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
