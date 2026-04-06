// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series types that render hierarchical <see cref="TreeNode"/> data.</summary>
public abstract class HierarchicalSeries : ChartSeries
{
    /// <summary>Gets the root node of the tree hierarchy.</summary>
    public TreeNode Root { get; }

    /// <summary>Gets or sets the color map used to map depth or value to colors.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets whether labels are drawn on elements.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Initializes a new instance with the specified root node.</summary>
    protected HierarchicalSeries(TreeNode root) => Root = root;
}
