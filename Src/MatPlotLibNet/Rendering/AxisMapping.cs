// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>How one direction of a transform maps its values before they are spread over the plot
/// (<see cref="DataTransform.Of"/>): the rule of its axis' scale, picked once and bound to the axis' threshold, and that axis'
/// breaks, with the room its parts stand apart by.</summary>
/// <param name="Rule">The rule of the axis' scale, bound to its threshold.</param>
/// <param name="Breaks">The axis' breaks.</param>
/// <param name="Spacing">The share of the mean part the parts of the axis stand apart by (<see cref="AlongExtensions"/>'
/// <c>BreakSpacingAlong</c>); 0 cuts every break to nothing.</param>
internal readonly record struct AxisMapping(ScaleRule Rule, IReadOnlyList<AxisBreak> Breaks, double Spacing = 0)
{
    /// <summary>The mapping of <paramref name="axis"/> with <paramref name="breaks"/>.</summary>
    /// <param name="axis">The axis: its scale and symlog threshold.</param>
    /// <param name="breaks">Its breaks.</param>
    /// <param name="theme">The theme the axis is drawn under, whose symlog threshold an axis given none is drawn with, and whose share
    /// between subplots the parts of a broken axis stand apart by.</param>
    /// <param name="isX">Whether the axis runs along X.</param>
    /// <returns>The mapping.</returns>
    internal static AxisMapping Of(Axis axis, IReadOnlyList<AxisBreak> breaks, Styling.Theme theme, bool isX) =>
        new(axis.ScaleRuleUnder(theme), breaks, breaks.Count > 0 ? theme.BreakSpacingAlong(isX) : 0);

    /// <summary>Whether this direction is drawn in a space other than the data's own, or with a hole: it has a break, or a scale
    /// that shapes its space (<see cref="ScaleRule.ShapesItsSpace"/>: log, symlog, logit).</summary>
    internal bool IsShaped => Breaks.Count > 0 || Rule.ShapesItsSpace;

    /// <summary>The map of this direction's breaks over [<paramref name="min"/>, <paramref name="max"/>] in its scale, its parts
    /// standing <see cref="Spacing"/> apart; <see langword="null"/> when it has none.</summary>
    /// <param name="min">The low end of the full range, in data units.</param>
    /// <param name="max">The high end of the full range, in data units.</param>
    internal AxisBreakMap? MapOver(double min, double max) =>
        Breaks.Count > 0 ? new AxisBreakMap(Breaks, min, max, Rule, Spacing) : null;

    /// <summary>The map of this direction's breaks over the whole axis, every value its scale has a place for (from its
    /// lowest placed value up, <see cref="ScaleRule.LowestPlaced"/>): the space a pan moves a range in,
    /// so a break beyond the range on screen is cut out too and an end carried across it lands past it. Within any range the
    /// place it gives a value differs from <see cref="MapOver"/>'s only by a constant, so a shift the pointer measured on
    /// screen is the same shift here while no break crosses an end of the range. With no breaks it is the scale alone. Its breaks are cut
    /// to nothing: a whole axis has no mean part to stand its parts apart by.</summary>
    /// <returns>The map.</returns>
    internal AxisBreakMap OverTheWholeAxis() => new(Breaks, Rule.LowestPlaced, double.PositiveInfinity, Rule);
}
