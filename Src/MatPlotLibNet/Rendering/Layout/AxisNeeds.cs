// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What one axes needs at the two ends of one direction of the grid, as <see cref="ConstrainedLayoutEngine"/> measures it:
/// beside its plot (<see cref="AxesEdgeNeeds.MeasureSides"/>: its left and its right) or above and below it
/// (<see cref="AxesEdgeNeeds.MeasureEnds"/>, at the width the plot is drawn at: its top and its bottom).
/// <see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins"/> takes it into the margins and the gap of that direction.</summary>
/// <param name="Start">Room the start side needs (left, top): the left stack of the Y axis container and the pad, or the top band
/// and the axes title; an outside legend there past it.</param>
/// <param name="End">Room the end side needs (right, bottom): the right band and a colour bar in the library's margin past it, or the
/// X tick labels and the X axis title and a colour bar below them; an outside legend there past it.</param>
/// <param name="StartBand">The part of <paramref name="Start"/> that is measured, 0 when it is only estimated: it lifts the cap of
/// the margin or the gap there, since measured content must never be cut.</param>
/// <param name="EndBand">The part of <paramref name="End"/> that is measured; it lifts the cap of the margin or the gap there.</param>
/// <param name="PastEnd">What the axes draws past its end spine, without the pad (<see cref="RightBand.Past"/>): the figure's colour
/// bar in the library's margin stands past it when the axes stands at the end of the grid. 0 down the grid.</param>
internal readonly record struct AxisNeeds(double Start, double End, double StartBand, double EndBand, double PastEnd = 0);
