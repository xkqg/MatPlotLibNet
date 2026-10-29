// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>The figure JSON's form of a subplot's <see cref="GridChange"/>: each property the author set, with its value, and
/// nothing for the rest. A property that is absent is the theme's, where <see cref="GridDto"/> carries a whole grid.</summary>
internal sealed record GridChangeDto
{
    public bool? Visible { get; init; }
    public Color? Color { get; init; }
    public LineStyle? LineStyle { get; init; }
    public double? LineWidth { get; init; }
    public double? Alpha { get; init; }
    public GridWhich? Which { get; init; }
    public GridAxis? Axis { get; init; }

    /// <summary>The JSON's form of <paramref name="change"/>.</summary>
    internal static GridChangeDto From(GridChange change) => new()
    {
        Visible = change.Visible,
        Color = change.Color,
        LineStyle = change.LineStyle,
        LineWidth = change.LineWidth,
        Alpha = change.Alpha,
        Which = change.Which,
        Axis = change.Axis,
    };

    /// <summary>The change this record describes.</summary>
    internal GridChange ToGridChange() => new(Visible, Color, LineStyle, LineWidth, Alpha, Which, Axis);
}
