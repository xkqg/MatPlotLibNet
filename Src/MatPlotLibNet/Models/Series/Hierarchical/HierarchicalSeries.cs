// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series types that render hierarchical <see cref="TreeNode"/> data.</summary>
public abstract class HierarchicalSeries : ChartSeries, IColormappable
{
    public TreeNode Root { get; }

    public IColorMap? ColorMap { get; set; }

    public bool ShowLabels { get; set; } = true;

    /// <summary>Initializes a new instance with the specified root node.</summary>
    protected HierarchicalSeries(TreeNode root) => Root = root;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);
}
