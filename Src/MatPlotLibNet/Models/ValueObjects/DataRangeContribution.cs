// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series' contribution to the overall axes data range. Null values indicate no contribution to that axis.</summary>
/// <param name="XMin">Minimum X value contributed, or null.</param>
/// <param name="XMax">Maximum X value contributed, or null.</param>
/// <param name="YMin">Minimum Y value contributed, or null.</param>
/// <param name="YMax">Maximum Y value contributed, or null.</param>
/// <param name="StickyXMin">Sticky-edge X minimum — if set, `axes.xmargin` expansion will not push the computed xMin below this value. Mirrors matplotlib's <c>Artist.sticky_edges.x</c>.</param>
/// <param name="StickyXMax">Sticky-edge X maximum. Analogous to <paramref name="StickyXMin"/>.</param>
/// <param name="StickyYMin">Sticky-edge Y minimum — matplotlib's <c>BarContainer</c> sets this to <c>0</c> so the y-axis never pads below the bar baseline.</param>
/// <param name="StickyYMax">Sticky-edge Y maximum.</param>
public readonly record struct DataRangeContribution(
    double? XMin, double? XMax, double? YMin, double? YMax,
    double? StickyXMin = null, double? StickyXMax = null,
    double? StickyYMin = null, double? StickyYMax = null);
