// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies how a spine is positioned relative to the axes.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum SpinePosition
{
    /// <summary>Default position at the axes edge.</summary>
    Edge = 0,

    /// <summary>Position at a specific data coordinate.</summary>
    Data = 1,

    /// <summary>Position at a fraction of the axes (0.0 = left/bottom, 1.0 = right/top).</summary>
    Axes = 2,
}

/// <summary>Configures a single axis spine (border line).</summary>
public sealed record SpineConfig
{
    /// <summary>Whether the spine is drawn. On by default.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>How the spine is placed: at the edge of the plot area (the default), at a data coordinate, or at a
    /// fraction of the axes. <see cref="PositionValue"/> says where for the last two.</summary>
    public SpinePosition Position { get; init; } = SpinePosition.Edge;

    /// <summary>Where the spine stands when <see cref="Position"/> is not <see cref="SpinePosition.Edge"/>: a data value
    /// on the axis the spine crosses (the Y value of a bottom or top spine, the X value of a left or right one), or a
    /// fraction from 0 (left or bottom) to 1 (right or top) for <see cref="SpinePosition.Axes"/>. Ignored at the edge.</summary>
    public double PositionValue { get; init; }

    /// <summary>The width of the spine line, in pixels. Default 0.8.</summary>
    public double LineWidth { get; init; } = 0.8;

    /// <summary>The colour of the spine. <see langword="null"/> (the default) takes the theme's text colour.</summary>
    public Color? Color { get; init; }

    /// <summary>The dash pattern of the spine. Solid by default.</summary>
    public LineStyle LineStyle { get; init; } = LineStyle.Solid;

    private readonly double? _boundsFrom;
    private readonly double? _boundsTo;

    /// <summary>Where the spine begins along its own axis, in data units of that axis (X for a bottom or top spine, Y for a
    /// left or right one), as matplotlib's <c>set_bounds</c> says it. <see langword="null"/>, the default, starts the spine
    /// at the edge of the plot. Together with <see cref="BoundsTo"/> it makes a range frame: an axis line that spans only
    /// the data's range. The tick marks are not clipped to the bounds, as matplotlib's are not. A bound has to be a finite
    /// number; one given the other way round (<see cref="BoundsFrom"/> above <see cref="BoundsTo"/>) draws the same line.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a number or is infinite.</exception>
    public double? BoundsFrom
    {
        get => _boundsFrom;
        init => _boundsFrom = value.Finite(nameof(BoundsFrom));
    }

    /// <summary>Where the spine ends along its own axis, in data units of that axis; <see langword="null"/> ends it at the
    /// edge of the plot. See <see cref="BoundsFrom"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a number or is infinite.</exception>
    public double? BoundsTo
    {
        get => _boundsTo;
        init => _boundsTo = value.Finite(nameof(BoundsTo));
    }
}

internal static class SpineBoundExtensions
{
    extension(double? bound)
    {
        /// <summary>The bound itself, refused by name when it is not a finite number.</summary>
        internal double? Finite(string name) =>
            bound is { } value && !double.IsFinite(value)
                ? throw new ArgumentOutOfRangeException(name, value, "A spine bound is a finite number in data units.")
                : bound;
    }
}

/// <summary>Configures all four spines of a Cartesian axes.</summary>
public sealed record SpinesConfig
{
    /// <summary>The spine along the top edge of the plot area. Drawn by default.</summary>
    public SpineConfig Top { get; init; } = new();

    /// <summary>The spine along the bottom edge of the plot area, the X axis line. Drawn by default.</summary>
    public SpineConfig Bottom { get; init; } = new();

    /// <summary>The spine along the left edge of the plot area, the Y axis line. Drawn by default.</summary>
    public SpineConfig Left { get; init; } = new();

    /// <summary>The spine along the right edge of the plot area. Drawn by default.</summary>
    public SpineConfig Right { get; init; } = new();
}
