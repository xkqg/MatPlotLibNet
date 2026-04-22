// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>One segment of a <see cref="BrokenBarSeries"/> row: a horizontal bar
/// occupying <c>[Start, Start + Width]</c> on the X axis. Consumed by the
/// <c>BrokenBarH(BarRange[][], …)</c> convenience methods on <c>AxesBuilder</c>,
/// <c>FigureBuilder</c>, and <see cref="Axes"/>.</summary>
/// <param name="Start">Starting X coordinate of the segment.</param>
/// <param name="Width">Horizontal span of the segment. Must be non-negative.</param>
public readonly record struct BarRange(double Start, double Width);
