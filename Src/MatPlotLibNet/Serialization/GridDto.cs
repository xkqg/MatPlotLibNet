// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a subplot's <see cref="GridStyle"/>: whether it is shown (always written, as it
/// always was), and each other property where it differs from a plain grid's.</summary>
internal sealed record GridDto
{
    public bool Visible { get; init; }
    public Color? Color { get; init; }
    public LineStyle? LineStyle { get; init; }
    public double? LineWidth { get; init; }
    public double? Alpha { get; init; }
    public GridWhich? Which { get; init; }
    public GridAxis? Axis { get; init; }

    /// <summary>The JSON's form of <paramref name="grid"/>.</summary>
    internal static GridDto From(GridStyle grid) => new()
    {
        Visible = grid.Visible,
        Color = grid.Color.OrNullAtRest(GridStyle.AtRest.Color),
        LineStyle = grid.LineStyle.OrNullAtRest(GridStyle.AtRest.LineStyle),
        LineWidth = grid.LineWidth.OrNullAtRest(GridStyle.AtRest.LineWidth),
        Alpha = grid.Alpha.OrNullAtRest(GridStyle.AtRest.Alpha),
        Which = grid.Which.OrNullAtRest(GridStyle.AtRest.Which),
        Axis = grid.Axis.OrNullAtRest(GridStyle.AtRest.Axis),
    };

    /// <summary>The grid this record describes.</summary>
    internal GridStyle ToGridStyle() => new()
    {
        Visible = Visible,
        Color = Color ?? GridStyle.AtRest.Color,
        LineStyle = LineStyle ?? GridStyle.AtRest.LineStyle,
        LineWidth = LineWidth ?? GridStyle.AtRest.LineWidth,
        Alpha = Alpha ?? GridStyle.AtRest.Alpha,
        Which = Which ?? GridStyle.AtRest.Which,
        Axis = Axis ?? GridStyle.AtRest.Axis,
    };
}
