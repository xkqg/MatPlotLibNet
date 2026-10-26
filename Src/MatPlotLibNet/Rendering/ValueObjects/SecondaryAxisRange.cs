// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>One outward-stacked secondary Y axis' data range, tagged with which side and slot it belongs to
/// (<see cref="Axes.RightYAxes"/>/<see cref="Axes.LeftYAxes"/>) — the per-axis counterpart of
/// <see cref="Interaction.IChartLayout.GetDataRange"/> for everything beyond the primary (ROADMAP.md row 100:
/// "zoom and pan change the main X and Y axes; the right-hand and top axes keep their range"). Used both to
/// REPORT a slot's current range (<see cref="Interaction.IChartLayout.GetSecondaryDataRanges"/>) and to CARRY the
/// range a gesture computed for it (<see cref="Interaction.AxisRangeEvent.SecondaryRanges"/>,
/// <see cref="Interaction.PanEvent.SecondaryRanges"/>) — the same shape either way.</summary>
/// <param name="Side">Which side of the plot the slot stacks outward from.</param>
/// <param name="SlotIndex">The slot's index within its side's list (<see cref="Axes.RightYAxes"/> or
/// <see cref="Axes.LeftYAxes"/>), not a running index across both.</param>
/// <param name="Range">The slot's data range. Only <see cref="DataRange.YMin"/>/<see cref="DataRange.YMax"/> are
/// meaningful (a secondary Y axis shares the primary X); <see cref="DataRange.XMin"/>/<see cref="DataRange.XMax"/>
/// carry the primary X range it was computed against, for a reader that wants it without a second lookup.</param>
public readonly record struct SecondaryAxisRange(AxisSide Side, int SlotIndex, DataRange Range);
