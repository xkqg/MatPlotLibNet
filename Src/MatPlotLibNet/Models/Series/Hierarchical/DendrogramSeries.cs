// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Renders a hierarchical-clustering result as a dendrogram tree of "U"-shaped
/// segments. Each leaf is an input observation; each internal node is a merge whose
/// merge-axis coordinate equals <see cref="TreeNode.Value"/> (the merge distance produced
/// by the clustering algorithm).</summary>
public sealed class DendrogramSeries : HierarchicalSeries
{
    /// <summary>Tree orientation. <see cref="DendrogramOrientation.Top"/> places the root at
    /// the top of the plot with leaves along the bottom; <see cref="DendrogramOrientation.Left"/>
    /// places the root at the left with leaves on the right (essential for the row dendrogram
    /// in <c>ClustermapSeries</c>).</summary>
    public DendrogramOrientation Orientation { get; set; } = DendrogramOrientation.Top;

    /// <summary>Optional cut height. When set, a reference line is drawn at this merge
    /// distance and the leaves below the cut are coloured by cluster membership when
    /// <see cref="ColorByCluster"/> is <see langword="true"/>.</summary>
    public double? CutHeight { get; set; }

    /// <summary>Colour of the cut reference line. Falls back to a theme-neutral default when
    /// <see langword="null"/>.</summary>
    public Color? CutLineColor { get; set; }

    /// <summary>If <see langword="true"/> (default) and <see cref="CutHeight"/> is set, each
    /// connected component below the cut is coloured by sampling the assigned
    /// <see cref="HierarchicalSeries.ColorMap"/>; if <see langword="false"/>, the entire tree
    /// is rendered in the cycled series colour.</summary>
    public bool ColorByCluster { get; set; } = true;

    /// <summary>Initializes a new instance of <see cref="DendrogramSeries"/> with the
    /// specified root node.</summary>
    /// <param name="root">The root <see cref="TreeNode"/>. Internal nodes must carry their
    /// merge distance in <see cref="TreeNode.Value"/>.</param>
    public DendrogramSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "dendrogram",
        DendrogramOrientation = Orientation != Models.Series.DendrogramOrientation.Top
            ? Orientation : null,
        CutHeight = CutHeight,
        CutLineColor = CutLineColor,
        ColorByCluster = ColorByCluster ? null : false,
        ColorMapName = ColorMap?.Name,
        ShowLabels = ShowLabels ? null : false,
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}

/// <summary>Orientation of a <see cref="DendrogramSeries"/>: which side of the plot holds
/// the root and which side holds the leaves.</summary>
public enum DendrogramOrientation
{
    /// <summary>Root at top, leaves on the bottom axis (default).</summary>
    Top = 0,

    /// <summary>Root at bottom, leaves on the top axis.</summary>
    Bottom = 1,

    /// <summary>Root at left, leaves on the right axis (used by row-dendrogram in clustermap).</summary>
    Left = 2,

    /// <summary>Root at right, leaves on the left axis.</summary>
    Right = 3,
}
