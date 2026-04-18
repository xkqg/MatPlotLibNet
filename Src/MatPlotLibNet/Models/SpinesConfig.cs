// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies how a spine is positioned relative to the axes.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
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
    public bool Visible { get; init; } = true;

    public SpinePosition Position { get; init; } = SpinePosition.Edge;

    public double PositionValue { get; init; }

    public double LineWidth { get; init; } = 0.8;

    public Color? Color { get; init; }

    public LineStyle LineStyle { get; init; } = LineStyle.Solid;
}

/// <summary>Configures all four spines of a Cartesian axes.</summary>
public sealed record SpinesConfig
{
    public SpineConfig Top { get; init; } = new();

    public SpineConfig Bottom { get; init; } = new();

    public SpineConfig Left { get; init; } = new();

    public SpineConfig Right { get; init; } = new();
}
