// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>A tree becomes wire data and back here, recursively — the one place shared by every
/// <see cref="Models.Series.HierarchicalSeries"/> (Treemap, Sunburst, Icicle, Dendrogram) and
/// <see cref="Models.Series.ClustermapSeries"/>'s RowTree/ColumnTree.</summary>
internal static class TreeNodeSerializationExtensions
{
    extension(Models.TreeNode node)
    {
        /// <summary>Converts this tree, recursively, to its DTO.</summary>
        internal TreeNodeDto TreeNodeToDto() => new()
        {
            Label = node.Label,
            Value = node.Value,
            Color = node.Color,
            ColorValue = node.ColorValue,
            Headline = node.Headline,
            Url = node.Url,
            Expanded = node.Expanded,
            Hatching = node.Hatching,
            Children = node.Children.Count > 0 ? [.. node.Children.Select(child => child.TreeNodeToDto())] : null,
        };
    }

    extension(TreeNodeDto dto)
    {
        /// <summary>Converts this DTO, recursively, back to a <see cref="Models.TreeNode"/> — the counterpart of
        /// <see cref="TreeNodeToDto"/>.</summary>
        internal Models.TreeNode DtoToTreeNode() => new()
        {
            Label = dto.Label,
            Value = dto.Value,
            Color = dto.Color,
            ColorValue = dto.ColorValue,
            Headline = dto.Headline,
            Url = dto.Url,
            Expanded = dto.Expanded,
            Hatch = dto.Hatching.Pattern,
            HatchColor = dto.Hatching.Color,
            Children = dto.Children is { } children ? [.. children.Select(c => c.DtoToTreeNode())] : [],
        };
    }

    extension(TreeNodeDto? dto)
    {
        /// <summary>The tree a hierarchy reads back: the one the document carries, or a bare root named <c>Root</c> when it carries
        /// none, so a hierarchy read from any document has a tree to draw.</summary>
        /// <returns>The tree.</returns>
        internal Models.TreeNode TreeOrBareRoot() => dto?.DtoToTreeNode() ?? new Models.TreeNode { Label = "Root" };
    }
}
