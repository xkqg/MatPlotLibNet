// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Associates a series index with its legend entry's pixel-space bounding rectangle.
/// Used by <see cref="LayoutResult"/> to expose legend hit-test data to interaction modifiers.</summary>
/// <param name="SeriesIndex">The zero-based index of the series in the parent <see cref="Models.Axes.Series"/> list.</param>
/// <param name="Bounds">The pixel-space rectangle enclosing the legend entry (swatch + label).</param>
public readonly record struct LegendItemBounds(int SeriesIndex, Rect Bounds);
