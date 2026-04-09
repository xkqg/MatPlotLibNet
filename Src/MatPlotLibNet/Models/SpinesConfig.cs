// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
    /// <summary>Gets whether this spine is visible.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Gets the position mode for this spine.</summary>
    public SpinePosition Position { get; init; } = SpinePosition.Edge;

    /// <summary>Gets the data or axes-fraction value when <see cref="Position"/> is <see cref="SpinePosition.Data"/> or <see cref="SpinePosition.Axes"/>.</summary>
    public double PositionValue { get; init; }

    /// <summary>Gets the line width of the spine.</summary>
    public double LineWidth { get; init; } = 1.0;
}

/// <summary>Configures all four spines of a Cartesian axes.</summary>
public sealed record SpinesConfig
{
    /// <summary>Gets the top spine configuration.</summary>
    public SpineConfig Top { get; init; } = new();

    /// <summary>Gets the bottom spine configuration.</summary>
    public SpineConfig Bottom { get; init; } = new();

    /// <summary>Gets the left spine configuration.</summary>
    public SpineConfig Left { get; init; } = new();

    /// <summary>Gets the right spine configuration.</summary>
    public SpineConfig Right { get; init; } = new();
}
