// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
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
    public SpinePosition Position { get; init; } = RestingPosition;

    /// <summary>Where a spine stands when it is told otherwise: at the edge of the plot area.</summary>
    internal const SpinePosition RestingPosition = SpinePosition.Edge;

    /// <summary>Where the spine stands when <see cref="Position"/> is not <see cref="SpinePosition.Edge"/>: a data value
    /// on the axis the spine crosses (the Y value of a bottom or top spine, the X value of a left or right one), or a
    /// fraction from 0 (left or bottom) to 1 (right or top) for <see cref="SpinePosition.Axes"/>. Ignored at the edge.</summary>
    public double PositionValue { get; init; }

    /// <summary>The default width of a spine line, in pixels: what <see cref="LineWidth"/> reads when none was given, and what
    /// every theme that names no width of its own draws.</summary>
    internal const double DefaultLineWidth = 0.8;

    /// <summary>The width of the spine line, in pixels. A spine that was given none reads <see cref="DefaultLineWidth"/> and is drawn as wide as its
    /// theme says (<see cref="DefaultLineWidth"/> pixels unless the theme names another: the matplotlib themes draw matplotlib's
    /// <c>axes.linewidth</c>, 0.8 pt under the default style and 1 pt under the classic one, at 100 dpi). A width that was given,
    /// the default among them, is drawn as it is under every theme.</summary>
    public double LineWidth { get => GivenLineWidth ?? DefaultLineWidth; init => GivenLineWidth = value; }

    /// <summary>The width the spine is drawn with under <paramref name="theme"/>: the one it was given, else the theme's own.</summary>
    /// <param name="theme">The theme the spine is drawn under.</param>
    /// <returns>The width, in pixels.</returns>
    internal double LineWidthIn(Theme theme) => theme.Looks.Frame.SpineWidthOver(GivenLineWidth, DefaultLineWidth);

    /// <summary>The width the spine was given, <see langword="null"/> when it was given none and follows its theme.</summary>
    internal double? GivenLineWidth { get; private init; }

    /// <summary>The colour of the spine. <see langword="null"/> (the default) takes the theme's text colour.</summary>
    public Color? Color { get; init; }

    /// <summary>The colour the spine is drawn in under <paramref name="theme"/>: the one it was given, else the theme's text
    /// colour. The one rule the spines of a Cartesian axes and the circle of a polar axes are drawn by.</summary>
    /// <param name="theme">The theme the spine is drawn under.</param>
    /// <returns>The colour.</returns>
    internal Color ColorIn(Theme theme) => Color ?? theme.ForegroundText;

    /// <summary>The line the spine is drawn with under <paramref name="theme"/>: its colour, its width and its dash pattern, by the rules
    /// <see cref="ColorIn"/> and <see cref="LineWidthIn"/> give them. The one place a spine becomes a stroke, for the spines of a
    /// Cartesian axes, the circle of a polar axes, the frame of a radar chart and the outline of a map.</summary>
    /// <param name="theme">The theme the spine is drawn under.</param>
    /// <returns>The stroke.</returns>
    internal StrokeStyle StrokeIn(Theme theme) => StrokeStyle.Of(ColorIn(theme), LineWidthIn(theme), LineStyle);

    /// <summary>Half the width the spine is drawn with under <paramref name="theme"/>: how far its line reaches past its centre, the
    /// room a tick keeps clear of it.</summary>
    /// <param name="theme">The theme the spine is drawn under.</param>
    /// <returns>Half the width, in pixels.</returns>
    internal double HalfWidthIn(Theme theme) => LineWidthIn(theme).Reach;

    /// <summary>The dash pattern of the spine. Solid by default.</summary>
    public LineStyle LineStyle { get; init; } = RestingLineStyle;

    /// <summary>The dash pattern of a spine when it is told none.</summary>
    internal const LineStyle RestingLineStyle = LineStyle.Solid;

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
        init => _boundsFrom = value?.ThrowIfNotFinite(nameof(BoundsFrom));
    }

    /// <summary>Where the spine ends along its own axis, in data units of that axis; <see langword="null"/> ends it at the
    /// edge of the plot. See <see cref="BoundsFrom"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a number or is infinite.</exception>
    public double? BoundsTo
    {
        get => _boundsTo;
        init => _boundsTo = value?.ThrowIfNotFinite(nameof(BoundsTo));
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

    /// <summary>Whether a spine stands anywhere but on its edge (a data value, a fraction of the axes): then what hangs from it stands
    /// where it stands, and the room past the plot depends on the plot's size.</summary>
    internal bool AnyOffItsEdge =>
        Top.Position != SpinePosition.Edge || Bottom.Position != SpinePosition.Edge
        || Left.Position != SpinePosition.Edge || Right.Position != SpinePosition.Edge;
}
