// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Result of a nearest-point search: the closest data point to a hover position,
/// together with the series it belongs to and the pixel distance from the cursor.</summary>
public sealed record NearestPointResult(
    string SeriesLabel, int SeriesIndex,
    double DataX, double DataY,
    double PixelDistance)
{
    /// <summary>Position of the chosen point in the series' own arrays, or <c>-1</c> when the result was
    /// constructed without one.
    /// <para>The search walks <c>XData</c>/<c>YData</c> by index, so it knows exactly which sample it picked.
    /// Without that number a caller can only compare two doubles against its source data, and two samples at
    /// the same instant — a value published twice, a retry, two flows sharing one label — are indistinguishable
    /// that way. The index is the one thing that keys back to the caller's own row.</para>
    /// <para>An <c>init</c> member rather than a positional one: a seventh positional parameter would break
    /// every existing construction site, the same reason <c>StateSegment.Hatch</c> is an <c>init</c>
    /// property.</para></summary>
    public int PointIndex { get; init; } = -1;
}
