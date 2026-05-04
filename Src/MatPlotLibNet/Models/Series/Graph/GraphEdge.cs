// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One edge in a <see cref="NetworkGraphSeries"/>: the source and target node
/// IDs, an optional weight (drives stroke thickness), and a directionality flag (drives
/// arrowhead emission).</summary>
/// <param name="From">Source-node identifier; must match a <see cref="GraphNode.Id"/>
/// in the parent series.</param>
/// <param name="To">Target-node identifier; must match a <see cref="GraphNode.Id"/>
/// in the parent series.</param>
/// <param name="Weight">Per-edge value mapped to stroke thickness via
/// <see cref="NetworkGraphSeries.EdgeThicknessScale"/>. Default <c>1.0</c>.</param>
/// <param name="IsDirected">When <see langword="true"/> the renderer paints an arrowhead
/// at the <see cref="To"/> end of the edge. Default <see langword="false"/>
/// (undirected — straight line).</param>
public readonly record struct GraphEdge(
    string From,
    string To,
    double Weight = 1.0,
    bool   IsDirected = false);
