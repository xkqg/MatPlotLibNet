// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Configures the margins and gaps between subplots in a figure.</summary>
public sealed record SubPlotSpacing
{
    /// <summary>Gets the left margin in pixels.</summary>
    public double MarginLeft { get; init; } = 60;

    /// <summary>Gets the right margin in pixels.</summary>
    public double MarginRight { get; init; } = 20;

    /// <summary>Gets the top margin in pixels.</summary>
    public double MarginTop { get; init; } = 40;

    /// <summary>Gets the bottom margin in pixels.</summary>
    public double MarginBottom { get; init; } = 50;

    /// <summary>Gets the horizontal gap between subplots in pixels.</summary>
    public double HorizontalGap { get; init; } = 40;

    /// <summary>Gets the vertical gap between subplots in pixels.</summary>
    public double VerticalGap { get; init; } = 40;

    /// <summary>Gets whether tight layout is enabled, which computes minimal margins automatically.</summary>
    public bool TightLayout { get; init; }
}
