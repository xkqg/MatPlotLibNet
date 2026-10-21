// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>What the right edge of one axes draws past its right spine, in drawing order: the axis side first (the
/// secondary Y axis or the mirrored primary Y ticks), then the end-of-line labels.
/// <see cref="ConstrainedLayoutEngine.MeasureRightBand"/> measures it.</summary>
/// <param name="SecondaryAxis">From the right spine to the far side of the secondary Y axis: its tick marks, padding
/// and widest tick label, then its title when it has one. 0 without a secondary Y axis.</param>
/// <param name="MirroredYTicks">From the right spine to the far side of the mirrored primary Y tick labels
/// (<see cref="EdgeBands.Right"/>). 0 when none are drawn.</param>
/// <param name="EndLabels">The gap before the end labels plus the widest of them. 0 when none is drawn.</param>
internal readonly record struct RightBand(double SecondaryAxis, double MirroredYTicks, double EndLabels)
{
    /// <summary>The axis side of the edge, where the end labels start: the secondary axis or the mirrored ticks,
    /// whichever reaches further. A drawn secondary axis owns the edge, so at most one of them is drawn; the wider one
    /// is charged because the secondary axis is reserved even when it has nothing to draw.</summary>
    public double Axis => Math.Max(SecondaryAxis, MirroredYTicks);

    /// <summary>The room the right edge needs: the axis side and the end labels stacked, plus the engine's outer
    /// padding when either is present. 0 when nothing is drawn past the spine.</summary>
    public double Width => Axis + EndLabels > 0
        ? Axis + EndLabels + ConstrainedLayoutEngine.PadRight
        : 0;
}
