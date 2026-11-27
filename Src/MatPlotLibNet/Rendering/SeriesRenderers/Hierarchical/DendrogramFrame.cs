// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Where a dendrogram stands in its plot: one sealed leaf for each <see cref="DendrogramOrientation"/>, chosen in one
/// place (<see cref="Of"/>). A frame maps a leaf (0..N-1) and a merge distance to pixels, turns the two into a point, and places
/// a leaf's label, so the renderer draws the tree, the cut line and the labels once for every orientation.</summary>
/// <remarks>An upright tree (<see cref="DendrogramOrientation.Top"/>, <see cref="DendrogramOrientation.Bottom"/>) spreads its
/// leaves along X and its merges along Y; a tree on its side spreads the leaves along Y and the merges along X. The orientation
/// names where the root stands.</remarks>
internal abstract class DendrogramFrame
{
    private static readonly TopDendrogramFrame Top = new();
    private static readonly BottomDendrogramFrame Bottom = new();
    private static readonly LeftDendrogramFrame Left = new();
    private static readonly RightDendrogramFrame Right = new();

    /// <summary>The frame of <paramref name="orientation"/>: the one place a dendrogram orientation is mapped to its frame, the
    /// same instance on every call. A value the enum does not declare stands the root on the right.</summary>
    /// <param name="orientation">The orientation the series asks for.</param>
    internal static DendrogramFrame Of(DendrogramOrientation orientation) => orientation switch
    {
        DendrogramOrientation.Top => Top,
        DendrogramOrientation.Bottom => Bottom,
        DendrogramOrientation.Left => Left,
        _ => Right,
    };

    /// <summary>Maps a leaf index (0..N-1) to its pixel along the leaf axis. A lone leaf (<paramref name="leafCount"/> ≤ 1)
    /// stands in the middle of the plot, so it is anchored where the eye looks for it.</summary>
    /// <param name="leafCoord">The leaf's place among the leaves.</param>
    /// <param name="bounds">The plot area.</param>
    /// <param name="leafCount">How many leaves the tree has.</param>
    internal abstract double LeafPixel(double leafCoord, Rect bounds, int leafCount);

    /// <summary>Maps a merge distance to its pixel along the merge axis.</summary>
    /// <param name="mergeCoord">The merge distance.</param>
    /// <param name="bounds">The plot area.</param>
    /// <param name="maxMerge">The largest merge distance of the tree.</param>
    internal abstract double MergePixel(double mergeCoord, Rect bounds, double maxMerge);

    /// <summary>The point at <paramref name="mergePx"/> along the merge axis and <paramref name="leafPx"/> along the leaf axis.</summary>
    internal abstract Point At(double mergePx, double leafPx);

    /// <summary>Draws a leaf's label beyond the leaves, away from the tree.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="label">The leaf's label.</param>
    /// <param name="leafPx">The leaf's pixel along the leaf axis.</param>
    /// <param name="mergeBaselinePx">The pixel of merge distance 0, where the leaves end.</param>
    /// <param name="font">The label font.</param>
    internal abstract void DrawLabel(IRenderContext ctx, string label, double leafPx, double mergeBaselinePx, Font font);

    /// <summary>How far along the leaf axis a leaf stands, as a share of the axis.</summary>
    private protected static double LeafShare(double leafCoord, int leafCount) =>
        leafCount <= 1 ? 0.5 : (leafCoord + 0.5) / leafCount;
}

/// <summary>An upright tree: the leaves along X, the merges along Y.</summary>
internal abstract class UprightDendrogramFrame : DendrogramFrame
{
    /// <inheritdoc />
    internal sealed override double LeafPixel(double leafCoord, Rect bounds, int leafCount) =>
        bounds.X + LeafShare(leafCoord, leafCount) * bounds.Width;

    /// <inheritdoc />
    internal sealed override Point At(double mergePx, double leafPx) => new(leafPx, mergePx);
}

/// <summary>A tree on its side: the leaves along Y, the merges along X.</summary>
internal abstract class SidewaysDendrogramFrame : DendrogramFrame
{
    /// <inheritdoc />
    internal sealed override double LeafPixel(double leafCoord, Rect bounds, int leafCount) =>
        bounds.Y + LeafShare(leafCoord, leafCount) * bounds.Height;

    /// <inheritdoc />
    internal sealed override Point At(double mergePx, double leafPx) => new(mergePx, leafPx);
}

/// <summary>The root at the top, the leaves along the bottom edge and their labels below them.</summary>
internal sealed class TopDendrogramFrame : UprightDendrogramFrame
{
    /// <inheritdoc />
    internal override double MergePixel(double mergeCoord, Rect bounds, double maxMerge) =>
        bounds.Bottom - mergeCoord / maxMerge * bounds.Height;

    /// <inheritdoc />
    internal override void DrawLabel(IRenderContext ctx, string label, double leafPx, double mergeBaselinePx, Font font) =>
        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(leafPx, mergeBaselinePx + HierarchicalLayout.Dendrogram.LabelOffsetPx), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top });
}

/// <summary>The root at the bottom, the leaves along the top edge and their labels above them.</summary>
internal sealed class BottomDendrogramFrame : UprightDendrogramFrame
{
    /// <inheritdoc />
    internal override double MergePixel(double mergeCoord, Rect bounds, double maxMerge) =>
        bounds.Y + mergeCoord / maxMerge * bounds.Height;

    /// <inheritdoc />
    internal override void DrawLabel(IRenderContext ctx, string label, double leafPx, double mergeBaselinePx, Font font) =>
        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(leafPx, mergeBaselinePx - HierarchicalLayout.Dendrogram.LabelOffsetPx), TextAlignment.Center));
}

/// <summary>The root at the left, the leaves along the right edge and their labels, turned, right of them.</summary>
internal sealed class LeftDendrogramFrame : SidewaysDendrogramFrame
{
    /// <inheritdoc />
    internal override double MergePixel(double mergeCoord, Rect bounds, double maxMerge) =>
        bounds.Right - mergeCoord / maxMerge * bounds.Width;

    /// <inheritdoc />
    internal override void DrawLabel(IRenderContext ctx, string label, double leafPx, double mergeBaselinePx, Font font) =>
        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(mergeBaselinePx + HierarchicalLayout.Dendrogram.LabelOffsetPx, leafPx), TextAlignment.Left) { Rotation = 90 });
}

/// <summary>The root at the right, the leaves along the left edge and their labels, turned, left of them.</summary>
internal sealed class RightDendrogramFrame : SidewaysDendrogramFrame
{
    /// <inheritdoc />
    internal override double MergePixel(double mergeCoord, Rect bounds, double maxMerge) =>
        bounds.X + mergeCoord / maxMerge * bounds.Width;

    /// <inheritdoc />
    internal override void DrawLabel(IRenderContext ctx, string label, double leafPx, double mergeBaselinePx, Font font) =>
        ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(mergeBaselinePx - HierarchicalLayout.Dendrogram.LabelOffsetPx, leafPx), TextAlignment.Right) { Rotation = 90 });
}
