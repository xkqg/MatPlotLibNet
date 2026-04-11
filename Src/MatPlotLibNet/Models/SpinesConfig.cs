// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies how a spine is positioned relative to the axes.</summary>
public enum SpinePosition
{
    /// <summary>Default position at the axes edge.</summary>
    Edge,

    /// <summary>Position at a specific data coordinate.</summary>
    Data,

    /// <summary>Position at a fraction of the axes (0.0 = left/bottom, 1.0 = right/top).</summary>
    Axes
}

/// <summary>Configures a single axis spine (border line).</summary>
public sealed record SpineConfig
{
    public bool Visible { get; init; } = true;

    public SpinePosition Position { get; init; } = SpinePosition.Edge;

    public double PositionValue { get; init; }

    public double LineWidth { get; init; } = 1.0;

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
