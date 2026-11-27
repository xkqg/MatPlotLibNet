// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How the dash and gap of each broken <see cref="LineStyle"/> are laid out: the pattern of each style, whether the lengths grow
/// with the width of the line, and how many pixels one unit of the pattern is. A value of the theme (<see cref="StrokeLook.Dashes"/>), taken by
/// <see cref="DashPatterns.Lengths"/>, the one owner of the lengths every backend strokes with.</summary>
internal sealed record DashLook : IThemeKeys<DashLook>
{
    /// <summary>The library's own dashes, in pixels as they stand: 5 on and 2 off for a dashed line, 1 and 3 for a dotted one, 5, 2, 1
    /// and 2 for a dash-dot one (matplotlib's <c>'--'</c> of 3.7 pt and 1.6 pt, <c>':'</c> and <c>'-.'</c> at about 96 dpi), the same
    /// whatever the width of the line.</summary>
    internal static DashLook Library { get; } = new();

    /// <summary>The dash and gap of a dashed line, in order.</summary>
    public double[] Dashed { get; init; } = [5, 2];

    /// <summary>The dash and gap of a dotted line, in order.</summary>
    public double[] Dotted { get; init; } = [1, 3];

    /// <summary>The dashes and gaps of a dash-dot line, in order.</summary>
    public double[] DashDot { get; init; } = [5, 2, 1, 2];

    /// <summary>Whether every length grows with the width of the line, as matplotlib's <c>lines.scale_dashes</c> does it under the default
    /// style (a pattern of 3.7 and 1.6 is in line widths). Otherwise the lengths are the same whatever the width, as under the classic
    /// style and the library's own look.</summary>
    public bool ScalesWithWidth { get; init; }

    /// <summary>How many pixels one unit of the pattern is: 1 where the pattern is in pixels, <c>100 / 72</c> where it is in points, as
    /// matplotlib's classic patterns are.</summary>
    public double Unit { get; init; } = 1;

    /// <summary>The dashes and gaps of <paramref name="style"/> as this look names them, before the width and the unit.</summary>
    /// <param name="style">The line style.</param>
    /// <returns>The pattern; empty for a solid line and for no line.</returns>
    internal ReadOnlySpan<double> RatiosOf(LineStyle style) => style switch
    {
        LineStyle.Dashed => Dashed,
        LineStyle.Dotted => Dotted,
        LineStyle.DashDot => DashDot,
        _ => [],
    };

    /// <inheritdoc />
    /// <remarks>A look other than the library's writes its three patterns whole, the library's none.</remarks>
    public void WriteKeys(ThemeDto keys)
    {
        bool library = ReferenceEquals(this, Library);
        keys.DashedPattern = library ? null : Dashed;
        keys.DottedPattern = library ? null : Dotted;
        keys.DashDotPattern = library ? null : DashDot;
        keys.DashesScaleWithWidth = ScalesWithWidth.OrNullAtRest(false);
        keys.DashUnit = Unit.OrNullAtRest(Library.Unit);
    }

    /// <inheritdoc />
    /// <remarks>Each key that is absent is the library's own value, and a record that names none is the library's look itself.</remarks>
    public static DashLook ReadKeys(ThemeDto keys) =>
        keys is { DashedPattern: null, DottedPattern: null, DashDotPattern: null, DashesScaleWithWidth: null, DashUnit: null }
            ? Library
            : new DashLook
            {
                Dashed = keys.DashedPattern ?? Library.Dashed,
                Dotted = keys.DottedPattern ?? Library.Dotted,
                DashDot = keys.DashDotPattern ?? Library.DashDot,
                ScalesWithWidth = keys.DashesScaleWithWidth ?? false,
                Unit = keys.DashUnit ?? Library.Unit,
            };
}