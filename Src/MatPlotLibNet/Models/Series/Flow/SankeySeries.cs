// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a Sankey diagram that renders flows between nodes as curved links.</summary>
public sealed class SankeySeries : ChartSeries
{
    public IReadOnlyList<SankeyNode> Nodes { get; }

    public IReadOnlyList<SankeyLink> Links { get; }

    public double NodeWidth { get; set; } = 20;

    public double NodePadding { get; set; } = 10;

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
