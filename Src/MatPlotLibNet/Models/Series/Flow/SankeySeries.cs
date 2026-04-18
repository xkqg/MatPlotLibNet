// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Horizontal alignment strategy for Sankey node columns.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
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
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
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
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum SankeyOrientation
{
    /// <summary>Columns span left-to-right, stems flow horizontally (default).</summary>
    Horizontal = 0,
    /// <summary>Columns span top-to-bottom, stems flow vertically.</summary>
    Vertical = 1,
}

/// <summary>Represents a Sankey diagram that renders flows between nodes as curved links.</summary>
public sealed class SankeySeries : ChartSeries
{
    public IReadOnlyList<SankeyNode> Nodes { get; }

    public IReadOnlyList<SankeyLink> Links { get; }

    public double NodeWidth { get; set; } = 20;

    public double NodePadding { get; set; } = 10;

    public double LinkAlpha { get; set; } = 0.4;

    /// <summary>Horizontal alignment strategy for node columns. Default <see cref="SankeyNodeAlignment.Justify"/>.</summary>
    public SankeyNodeAlignment NodeAlignment { get; set; } = SankeyNodeAlignment.Justify;

    /// <summary>Number of vertical-relaxation passes used to minimise link crossings. Each pass
    /// shifts every node toward the value-weighted average of its neighbour positions (matplotlib
    /// / D3 sankey use 6 by default). Set to 0 to disable relaxation and keep the greedy initial
    /// vertical order.</summary>
    public int Iterations { get; set; } = 6;

    /// <summary>Colour resolution strategy for links. Default <see cref="SankeyLinkColorMode.Gradient"/>
    /// produces presentation-quality source → target colour blends via SVG <c>&lt;linearGradient&gt;</c>.</summary>
    public SankeyLinkColorMode LinkColorMode { get; set; } = SankeyLinkColorMode.Gradient;

    /// <summary>Diagram orientation. Default <see cref="SankeyOrientation.Horizontal"/>.</summary>
    public SankeyOrientation Orient { get; set; } = SankeyOrientation.Horizontal;

    /// <summary>When true, node labels whose measured width fits inside the node rectangle are
    /// drawn centred inside the rect instead of to the outer side. matplotlib and amCharts both
    /// switch to inside-labels automatically for wide nodes; we expose it as an opt-in for now
    /// because our default <see cref="NodeWidth"/> of 20 px is almost always too narrow to host
    /// text. Set <c>NodeWidth</c> to 60+ px before enabling. Default <see langword="false"/>.</summary>
    public bool InsideLabels { get; set; }

    /// <summary>Initializes a new instance with the specified nodes and links.</summary>
    public SankeySeries(SankeyNode[] nodes, SankeyLink[] links)
    {
        Nodes = nodes;
        Links = links;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "sankey" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
