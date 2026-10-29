// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>A straight mark between two points, in pixels: a whisker, a tick, an interval.</summary>
/// <param name="From">One end.</param>
/// <param name="To">The other end.</param>
internal readonly record struct PixelSegment(Point From, Point To);
