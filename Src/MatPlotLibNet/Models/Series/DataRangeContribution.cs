// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>A series' contribution to the overall axes data range. Null values indicate no contribution to that axis.</summary>
public readonly record struct DataRangeContribution(double? XMin, double? XMax, double? YMin, double? YMax);
