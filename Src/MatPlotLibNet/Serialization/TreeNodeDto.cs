// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>DTO for one <see cref="Models.TreeNode"/>, recursively.</summary>
public sealed record TreeNodeDto
{
    /// <summary>The node's name (<see cref="Models.TreeNode.Label"/>).</summary>
    public string Label { get; init; } = "";
    /// <summary>The node's value (<see cref="Models.TreeNode.Value"/>): its area in a treemap, its size in a sunburst, and
    /// for an inner node of a dendrogram its merge distance.</summary>
    public double Value { get; init; }
    /// <summary>The node's own colour (<see cref="Models.TreeNode.Color"/>); absent for the colour the series gives
    /// it.</summary>
    public Color? Color { get; init; }
    /// <summary>The second value the node's colour is read from, through the series' normalizer and colour map
    /// (<see cref="Models.TreeNode.ColorValue"/>); absent for none.</summary>
    public double? ColorValue { get; init; }
    /// <summary>The number or short text a treemap cell shows under the node's name
    /// (<see cref="Models.TreeNode.Headline"/>); absent for none.</summary>
    public string? Headline { get; init; }
    /// <summary>The address the node's rectangle links to (<see cref="Models.TreeNode.Url"/>); absent for none.</summary>
    public string? Url { get; init; }
    /// <summary>Whether the subtree the node leads to is currently shown (<see cref="Models.TreeNode.Expanded"/>); absent
    /// for a node that discloses nothing.</summary>
    public bool? Expanded { get; init; }
    /// <summary>The fill pattern over the node's rectangle (<see cref="Models.TreeNode.Hatch"/>);
    /// <see cref="HatchPattern.None"/> for a plain fill.</summary>
    public HatchPattern Hatch { get; init; } = HatchPattern.None;
    /// <summary>The colour of the hatch strokes (<see cref="Models.TreeNode.HatchColor"/>); absent for a colour that
    /// contrasts with the fill.</summary>
    public Color? HatchColor { get; init; }
    /// <summary>The pattern and the colour of the hatch as the one value a tree node holds; the pattern is always written, as it has
    /// been, <see cref="HatchPattern.None"/> for a plain fill.</summary>
    internal HatchFill Hatching
    {
        get => HatchFill.FromTheWire(Hatch, HatchColor);
        init
        {
            Hatch = value.Pattern;
            HatchColor = value.Color;
        }
    }
    /// <summary>The node's children, each a node in turn (<see cref="Models.TreeNode.Children"/>); written only for a node
    /// that has some.</summary>
    public List<TreeNodeDto>? Children { get; init; }
}
