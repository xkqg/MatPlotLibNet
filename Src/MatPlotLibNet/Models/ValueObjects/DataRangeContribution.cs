// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series' contribution to the overall axes data range. Null values indicate no contribution to that axis.</summary>
/// <param name="XMin">Minimum X value contributed, or null.</param>
/// <param name="XMax">Maximum X value contributed, or null.</param>
/// <param name="YMin">Minimum Y value contributed, or null.</param>
/// <param name="YMax">Maximum Y value contributed, or null.</param>
public readonly record struct DataRangeContribution(double? XMin, double? XMax, double? YMin, double? YMax);
