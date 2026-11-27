// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models;

/// <summary>Where a series and a break are seated on an axes: the coordinate system a series puts the axes in, the colour group a set
/// of series wears, the last primary series taken back to be seated elsewhere, and the break added to either axis.</summary>
internal static class AxesSeatExtensions
{
    extension(Axes axes)
    {
        /// <summary>Puts <paramref name="members"/> in one fresh colour group (<see cref="ChartSeries.ColorGroup"/>), so they wear one
        /// colour and hold one colour slot: a verb that draws several series as one unit (a curve, its band and its marks) names its
        /// members once, before or after it adds them.</summary>
        /// <param name="members">The series of the group.</param>
        internal void InColorGroup(params ReadOnlySpan<ChartSeries> members)
        {
            // One more than the highest group a series of these axes holds (0 when none holds one), derived every call: a stored
            // counter would count past a series removed or an axes read back from JSON while a lower number sits free.
            int group = axes.AllSeries.OfType<ChartSeries>().Select(s => s.ColorGroup).Where(g => g.HasValue).Select(g => g!.Value)
                .DefaultIfEmpty(-1).Max() + 1;
            foreach (var member in members) member.ColorGroup = group;
        }

        /// <summary>Puts this axes in the coordinate system <paramref name="series"/> is drawn in, when its kind is drawn in one of its
        /// own (<c>ChartSeries.DrawnIn</c>: polar, ternary, 3-D); a Cartesian kind leaves the axes where it is, so a ternary axes holding
        /// a line stays ternary. Every seat a series is added at asks it: a Y axis slot and the top X axis.</summary>
        /// <param name="series">The series being added.</param>
        internal void TakeTheSystemOf(ISeries series)
        {
            if (series is ChartSeries { DrawnIn: { } system }) axes.CoordinateSystem = system;
        }

        /// <summary>Removes and returns the LAST primary series, or <see langword="null"/> when there is none. The seat-aware JSON
        /// restore path (<c>AxesSerializationExtensions.CreateOnSecondarySeat</c>) needs this: every registered <c>SeriesRegistry</c>
        /// factory lands its series here through its own primary verb (<c>axes.Plot</c>, <c>axes.Bar</c>, …), so restoring one onto a
        /// secondary slot or the top axis means building it normally, then moving it — not rewriting every factory to take a seat
        /// parameter.</summary>
        /// <returns>The series that was removed; <see langword="null"/> when the axes has no primary series.</returns>
        internal ISeries? TakeLastSeries()
        {
            var series = axes.PrimarySeriesList;
            if (series.Count == 0) return null;
            var last = series[^1];
            series.RemoveAt(series.Count - 1);
            return last;
        }

        /// <summary>Adds a broken region to the X axis (<paramref name="isX"/>) or the Y axis: the one implementation
        /// <see cref="Axes.AddXBreak"/>, <see cref="Axes.AddYBreak"/> and the reader of the wire form forward to.</summary>
        /// <param name="isX">The X axis when <see langword="true"/>, the Y axis otherwise.</param>
        /// <param name="from">Start of the hidden range.</param>
        /// <param name="to">End of the hidden range.</param>
        /// <param name="style">How the break is drawn.</param>
        /// <returns>This axes.</returns>
        internal Axes AddBreak(bool isX, double from, double to, BreakStyle style)
        {
            axes.Breaks.Along(isX).Add(new AxisBreak(from, to, style));
            return axes;
        }
    }
}
