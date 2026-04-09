// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a Sankey diagram that renders flows between nodes as curved links.</summary>
public sealed class SankeySeries : ChartSeries
{
    /// <summary>Gets the nodes in the diagram.</summary>
    public IReadOnlyList<SankeyNode> Nodes { get; }

    /// <summary>Gets the links connecting nodes.</summary>
    public IReadOnlyList<SankeyLink> Links { get; }

    /// <summary>Gets or sets the width of node rectangles in pixels.</summary>
    public double NodeWidth { get; set; } = 20;

    /// <summary>Gets or sets the vertical padding between nodes in pixels.</summary>
    public double NodePadding { get; set; } = 10;

    /// <summary>Gets or sets the opacity of link curves.</summary>
    public double LinkAlpha { get; set; } = 0.4;

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
