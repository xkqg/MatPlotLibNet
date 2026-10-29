// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>The pixel/data transform of one Y axis stacked past the primary, tagged with which side and slot it
/// belongs to — an interaction modifier's seam onto a specific axis, the same way the primary's own
/// <see cref="DataTransform"/> (<c>IChartLayout.ViewOf</c>) is. Internal: a host of <see cref="Interaction.IChartLayout"/>
/// other than the library's own <c>ChartLayout</c> states only a range (<see cref="SecondaryAxisRange"/>), never a
/// scale-aware transform — <c>ChartLayoutViewExtensions.SecondaryViewsOf</c> builds a plain linear one for it.</summary>
/// <param name="Side">Which side of the plot the slot stands on.</param>
/// <param name="SlotIndex">The slot's OUTWARD index within its side (see <see cref="SecondaryAxisRange.SlotIndex"/>), not a
/// running index across both: the primary Y axis has none.</param>
/// <param name="Transform">The transform mapping this slot's data range onto the shared plot area.</param>
internal readonly record struct SecondaryAxisTransform(AxisSide Side, int SlotIndex, DataTransform Transform);
