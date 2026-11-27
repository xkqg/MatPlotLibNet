// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where the one tick drawing (<see cref="AxisTickExtensions"/>, through a <see cref="TickRun"/>) stands a tick of a given value, and whether the plot
/// shows that value at all. The value is what the label reads; the placement decides the pixel. A <see cref="DataTransform"/>
/// stands a tick at its own value, as every axis does; a <see cref="LinkedTickPlacement"/> stands the tick of a linked axis where
/// the primary axis shows the value it was mapped from, so a label and its position can differ.</summary>
internal interface ITickPlacement
{
    /// <summary>Where a tick of <paramref name="value"/> stands along its edge, in pixels, when the plot shows it: the value lies
    /// on the range the axis shows there, ends included, give or take a ten-billionth of its width
    /// (<see cref="Range1D.HoldsClose"/>), judged in the space the axis' values live in before any pixel (as matplotlib keeps a
    /// tick in view; a pixel carries the rounding of the mapping, which for a date near 46,000 days across a range of minutes is
    /// larger than that tolerance), and the placement has a place for it. Only the coordinate along the edge is worked out.</summary>
    /// <param name="value">The tick's value, in the axis' own unit.</param>
    /// <param name="alongX">Whether the value is an X (a top or bottom edge); a Y otherwise.</param>
    /// <returns>The pixel along the edge, or <see langword="null"/> when the plot does not show the value.</returns>
    double? OnPlot(double value, bool alongX);

    /// <summary>The scale the ticks along that edge are drawn on, whose minor locator places the minor ticks: the axis' own; for a
    /// linked axis a log scale over a log primary and a linear one over any other, as matplotlib draws a secondary axis on its
    /// <c>functionlog</c> or <c>function</c> scale (<see cref="ScaleRule.UnderALink"/>).</summary>
    /// <param name="alongX">Whether the ticks run along a top or bottom edge.</param>
    /// <returns>The rule of that scale.</returns>
    ScaleRule ScaleAlong(bool alongX);

    /// <summary>The ends the axis shows along that edge, in the unit of its tick values, the view a minor locator places its ticks
    /// over; <see langword="null"/> when it shows none.</summary>
    /// <param name="alongX">Whether the ticks run along a top or bottom edge.</param>
    /// <returns>The view, or <see langword="null"/>.</returns>
    Range1D? ViewAlong(bool alongX);
}
