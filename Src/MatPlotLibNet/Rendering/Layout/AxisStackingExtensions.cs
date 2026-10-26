// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The one rule for stacking outward-stacked secondary axes past each other — matplotlib's own repeated
/// <c>ax.twinx()</c> plus <c>spines.set_position(('outward', N))</c>, in pixels: the first slot sits flush against the
/// plot edge, and each slot after it stands past the SUM of the bands every slot before it takes. Read by the layout
/// engine (to reserve the total) and the render part (to offset each slot) alike, so the two can never disagree about
/// where a slot's edge is — the same bug class as <c>ConstrainedLayoutEngine.cs:427</c> vs
/// <c>CartesianAxesRenderer.cs:40</c> deciding "is this axis drawn" two different ways.</summary>
internal static class AxisStackingExtensions
{
    /// <summary>The outward pixel offset of each band in <paramref name="bands"/>, in the order given: 0 for the
    /// first, then the running sum of every band before it.</summary>
    /// <param name="bands">One band per stacked slot, nearest-to-the-plot first.</param>
    /// <returns>One offset per band, the same length and order as <paramref name="bands"/>.</returns>
    internal static double[] StackedOffsets(this IReadOnlyList<double> bands)
    {
        var offsets = new double[bands.Count];
        double running = 0;
        for (int i = 0; i < bands.Count; i++)
        {
            offsets[i] = running;
            running += bands[i];
        }
        return offsets;
    }

    /// <summary>The total room every band in <paramref name="bands"/> takes together: the sum, the same total the
    /// layout reserves and the last slot's <see cref="StackedOffsets"/> entry plus its own band reach.</summary>
    internal static double TotalStackedBand(this IReadOnlyList<double> bands)
    {
        double total = 0;
        foreach (var band in bands) total += band;
        return total;
    }
}
