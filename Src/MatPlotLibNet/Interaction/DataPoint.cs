// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>A point in data-space (axis-unit) coordinates. Returned by
/// <see cref="ChartLayout.PixelToData(double, double, int)"/> when the given pixel lies within a
/// plot area, and consumed by the nearest-point hit-test pipeline in the
/// <c>DataCursorModifier</c>. Deconstructs as <c>var (dx, dy) = point;</c>.</summary>
/// <param name="DataX">X coordinate in the axes' data range.</param>
/// <param name="DataY">Y coordinate in the axes' data range.</param>
public readonly record struct DataPoint(double DataX, double DataY);
