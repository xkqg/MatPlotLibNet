// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>An oriented line segment from <paramref name="From"/> to <paramref name="To"/>.
/// Used by contour extractors (e.g.
/// <see cref="SeriesRenderers.TricontourSeriesRenderer"/>) to yield iso-line edges
/// before polyline chaining.</summary>
/// <param name="From">Starting endpoint.</param>
/// <param name="To">Ending endpoint.</param>
public readonly record struct LineSegment(Point From, Point To);
