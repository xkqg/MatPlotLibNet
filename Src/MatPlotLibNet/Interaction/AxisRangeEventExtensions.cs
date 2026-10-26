// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>The one place a <see cref="SecondaryAxisRange"/> list is applied to an <see cref="Axes"/> — shared by
/// <see cref="AxisRangeEvent.ApplyTo"/> (zoom/reset) and <see cref="PanEvent.ApplyTo"/> (pan), so the two event
/// families never diverge on how a secondary axis' range is written (CLAUDE.md §5).</summary>
internal static class AxisRangeEventExtensions
{
    /// <summary>Writes each change's range onto its own slot, unless that slot's own series fix their Y range (a
    /// state timeline on a secondary axis — independent of the primary's <see cref="Axes.HasFixedYRange"/>).</summary>
    /// <param name="axes">The axes the changes target.</param>
    /// <param name="ranges">The changes to apply; <see langword="null"/> or empty does nothing.</param>
    internal static void ApplySecondaryRanges(Axes axes, IReadOnlyList<SecondaryAxisRange>? ranges)
    {
        foreach (var change in ranges ?? [])
        {
            var slot = change.Side == AxisSide.Right ? axes.RightYAxes[change.SlotIndex] : axes.LeftYAxes[change.SlotIndex];
            if (slot.Series.HasFixedYRange()) continue;
            slot.Axis.Min = change.Range.YMin;
            slot.Axis.Max = change.Range.YMax;
        }
    }
}
