// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using GridColor = MatPlotLibNet.Styling.Color;
using GridLine = MatPlotLibNet.Styling.LineStyle;

namespace MatPlotLibNet.Styling;

/// <summary>What an author changed about a grid, as a value: the properties they set, each with the value they set it to,
/// and nothing else. It is applied over the grid of whatever theme the figure is drawn under (<see cref="ApplyTo"/>), so
/// "the theme's grid, but dashed" stays that when the figure is written to JSON and drawn under another theme, which a
/// grid resolved once under the theme it was built in cannot do. A property left <see langword="null"/> is the theme's.</summary>
/// <param name="Visible">Whether the grid is drawn, when the author said so.</param>
/// <param name="Color">The line colour, when the author set it.</param>
/// <param name="LineStyle">The line style, when the author set it.</param>
/// <param name="LineWidth">The line width in pixels, when the author set it.</param>
/// <param name="Alpha">The opacity, when the author set it.</param>
/// <param name="Which">Whether major, minor or both tick sets get lines, when the author set it.</param>
/// <param name="Axis">Which axis the lines run perpendicular to, when the author set it.</param>
internal readonly record struct GridChange(
    bool? Visible = null,
    GridColor? Color = null,
    GridLine? LineStyle = null,
    double? LineWidth = null,
    double? Alpha = null,
    GridWhich? Which = null,
    GridAxis? Axis = null)
{
    // Two grids that differ in every property. A callback is run on both: a property it sets comes out the same from
    // both, whatever it is set to (the value the theme happens to draw included); a property it leaves alone comes out as
    // it went in. Neither can be told from the other by running the callback on one grid, which is why one is not enough.
    private static readonly GridStyle ProbeA = new()
    {
        Visible = true, Color = new GridColor(0x12, 0x34, 0x56), LineStyle = GridLine.Dashed, LineWidth = 1.25,
        Alpha = 0.25, Which = GridWhich.Major, Axis = GridAxis.X,
    };

    private static readonly GridStyle ProbeB = new()
    {
        Visible = false, Color = new GridColor(0xAB, 0xCD, 0xEF), LineStyle = GridLine.Dotted, LineWidth = 3.5,
        Alpha = 0.85, Which = GridWhich.Minor, Axis = GridAxis.Y,
    };

    /// <summary>The grid with this change applied: each property the change sets replaces the grid's, the rest are the grid's.</summary>
    internal GridStyle ApplyTo(GridStyle grid) => grid with
    {
        Visible = Visible ?? grid.Visible,
        Color = Color ?? grid.Color,
        LineStyle = LineStyle ?? grid.LineStyle,
        LineWidth = LineWidth ?? grid.LineWidth,
        Alpha = Alpha ?? grid.Alpha,
        Which = Which ?? grid.Which,
        Axis = Axis ?? grid.Axis,
    };

    /// <summary>This change followed by <paramref name="later"/>: a property the later change sets wins, the rest stay as this
    /// change has them. A second <c>WithGrid</c> builds on the first and does not replace it.</summary>
    internal GridChange Then(GridChange later) => new(
        later.Visible ?? Visible, later.Color ?? Color, later.LineStyle ?? LineStyle, later.LineWidth ?? LineWidth,
        later.Alpha ?? Alpha, later.Which ?? Which, later.Axis ?? Axis);

    /// <summary>The change that sets every property to the value <paramref name="grid"/> has: the grid itself, wherever it is
    /// applied. It is what a grid written as "the axes' own" before changes had a form of reads back as.</summary>
    internal static GridChange AllOf(GridStyle grid) => new(
        grid.Visible, grid.Color, grid.LineStyle, grid.LineWidth, grid.Alpha, grid.Which, grid.Axis);

    /// <summary>The change <paramref name="change"/> makes, read off by running it on two grids that differ in every property
    /// (and once on a plain grid). A property it sets to a value is recorded with that value. A property it computes from the
    /// grid it is handed (half the opacity it was given) has no value of its own, and is recorded as what the callback makes
    /// of a plain grid: the narrow, named loss of keeping a change as data. <c>WithGrid</c> callbacks name values.</summary>
    /// <remarks>The callback runs three times; it should not have effects of its own.</remarks>
    internal static GridChange Of(Func<GridStyle, GridStyle> change)
    {
        var onA = change(ProbeA);
        var onB = change(ProbeB);
        var onPlain = change(new GridStyle());
        return new(
            Read(ProbeA.Visible, onA.Visible, ProbeB.Visible, onB.Visible, onPlain.Visible),
            Read(ProbeA.Color, onA.Color, ProbeB.Color, onB.Color, onPlain.Color),
            Read(ProbeA.LineStyle, onA.LineStyle, ProbeB.LineStyle, onB.LineStyle, onPlain.LineStyle),
            Read(ProbeA.LineWidth, onA.LineWidth, ProbeB.LineWidth, onB.LineWidth, onPlain.LineWidth),
            Read(ProbeA.Alpha, onA.Alpha, ProbeB.Alpha, onB.Alpha, onPlain.Alpha),
            Read(ProbeA.Which, onA.Which, ProbeB.Which, onB.Which, onPlain.Which),
            Read(ProbeA.Axis, onA.Axis, ProbeB.Axis, onB.Axis, onPlain.Axis));
    }

    /// <summary>One property: set when both probes come out the same, left alone when each comes out as it went in, and
    /// otherwise computed from its input, in which case it is what the callback made of a plain grid.</summary>
    private static T? Read<T>(T inA, T outA, T inB, T outB, T onPlain) where T : struct
    {
        var same = EqualityComparer<T>.Default;
        if (same.Equals(outA, outB)) return outA;
        if (same.Equals(outA, inA) && same.Equals(outB, inB)) return null;
        return onPlain;
    }
}
