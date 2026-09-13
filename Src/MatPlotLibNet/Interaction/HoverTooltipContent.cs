// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Content for a hover tooltip to be displayed by a native control. Carries the
/// formatted text and the pixel position where the tooltip should appear.</summary>
public sealed record HoverTooltipContent(
    string Text,
    double PixelX, double PixelY)
{
    /// <summary>Position of the hovered point in the series' own arrays, or <c>-1</c> when the tooltip was
    /// built without one. Hover and click resolve their point through the same search; only the click used to
    /// pass the identity on, so a host that highlights the matching row on hover had nothing to match it
    /// by.</summary>
    public int PointIndex { get; init; } = -1;

    /// <summary>Position of the hovered series within the axes, or <c>-1</c> when the tooltip was built
    /// without one.</summary>
    public int SeriesIndex { get; init; } = -1;
}
