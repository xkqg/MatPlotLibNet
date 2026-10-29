// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>The data range of one Y axis past the primary, tagged with which side and slot it belongs to — the per-axis
/// counterpart of <see cref="Interaction.IChartLayout.GetDataRange"/> for everything beyond the primary, so that a zoom
/// or a pan can move every Y axis by its own range. Used both to
/// REPORT a slot's current range (<see cref="Interaction.IChartLayout.GetSecondaryDataRanges"/>) and to CARRY the
/// range a gesture computed for it (<see cref="Interaction.AxisRangeEvent.SecondaryRanges"/>,
/// <see cref="Interaction.PanEvent.SecondaryRanges"/>) — the same shape either way.</summary>
/// <param name="Side">Which side of the plot the slot stands on.</param>
/// <param name="SlotIndex">The slot's OUTWARD index within its side, not a running index across both: 0 is the first axis
/// past the primary Y axis on the left and the first right axis on the right, so the primary Y axis has no index here.
/// The same number is in the <c>data-sec-{side}-{index}-*</c> attributes of a drawing and in the JSON lists of a saved
/// figure, and an older drawing in an open browser tab sends it back, so it keeps its meaning across releases.
/// <see cref="YAxisStack.SlotAtOutward"/> turns it into a slot of the container.</param>
/// <param name="Range">The slot's data range. Only <see cref="DataRange.YMin"/>/<see cref="DataRange.YMax"/> are
/// meaningful (a Y axis past the primary shares the primary X); <see cref="DataRange.XMin"/>/<see cref="DataRange.XMax"/>
/// carry the primary X range it was computed against, for a reader that wants it without a second lookup.</param>
public readonly record struct SecondaryAxisRange(AxisSide Side, int SlotIndex, DataRange Range);
