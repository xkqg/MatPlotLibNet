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
    extension(Axes axes)
    {
        /// <summary>Writes each change's range onto its own slot, unless that slot's own series fix their Y range (a
        /// state timeline on a secondary axis — independent of the primary's <see cref="Axes.HasFixedYRange"/>).
        /// <para>The index of a change is the interaction wire's, OUTWARD only: <see cref="SecondaryAxisRange.SlotIndex"/> 0
        /// on a side is the first slot PAST the primary Y axis, which is never addressable here.
        /// <see cref="YAxisStack.SlotAtOutward"/> is the one place that turns it into a slot of the container.</para>
        /// A change for a slot the axes does not have is skipped, as <see cref="LegendToggleEvent"/> skips a series index
        /// past the end: the index comes from a client over the wire, and one stale or crafted value must not throw out
        /// of the session's drain loop.</summary>
        /// <param name="ranges">The changes to apply; <see langword="null"/> or empty does nothing.</param>
        internal void ApplySecondaryRanges(IReadOnlyList<SecondaryAxisRange>? ranges)
        {
            foreach (var change in ranges ?? [])
            {
                if (axes.YAxes.StackOn(change.Side).SlotAtOutward(change.SlotIndex) is not { } slot) continue;
                if (slot.Series.HasFixedYRange()) continue;
                slot.Axis.Min = change.Range.YMin;
                slot.Axis.Max = change.Range.YMax;
            }
        }
    }
}
