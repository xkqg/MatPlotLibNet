// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The one rule for stacking the Y axes of one side past each other — matplotlib's own repeated
/// <c>ax.twinx()</c> plus <c>spines.set_position(('outward', N))</c>, in pixels: the first slot stands at the plot edge, and
/// each slot after it stands the stacking gap past the far side of the one before it. Read by the layout engine (to
/// reserve the total) and the renderer (to offset each slot) alike, so the two can never disagree about where a slot's
/// edge is.</summary>
internal static class AxisStackingExtensions
{
    extension(IReadOnlyList<double> bands)
    {
        /// <summary>The outward pixel offset of each band, in the order given: 0 for the first, then the running sum of
        /// every band before it and one <paramref name="gap"/> after each of them.</summary>
        /// <param name="gap">The blank gap between the far side of a slot and the tick marks of the slot past it.</param>
        /// <returns>One offset per band, the same length and order as the bands.</returns>
        internal double[] StackedOffsets(double gap)
        {
            var offsets = new double[bands.Count];
            double running = 0;
            for (int i = 0; i < bands.Count; i++)
            {
                offsets[i] = running;
                running += bands[i] + gap;
            }
            return offsets;
        }

        /// <summary>The room every band takes together: every band and one <paramref name="gap"/> between each pair, so
        /// the last slot's <see cref="StackedOffsets"/> entry plus its own band; 0 for no band.</summary>
        /// <param name="gap">The blank gap between the far side of a slot and the tick marks of the slot past it.</param>
        /// <returns>The total band in pixels.</returns>
        internal double TotalStackedBand(double gap)
        {
            double total = 0;
            foreach (var band in bands) total += band;
            return bands.Count > 1 ? total + (bands.Count - 1) * gap : total;
        }
    }
}
