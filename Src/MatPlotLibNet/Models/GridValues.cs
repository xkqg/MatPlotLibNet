// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>A grid of values and the header it is tabulated under (<see cref="ChartDataTable.OverGrid"/>): a contour's or a surface's
/// heights, a field's components.</summary>
/// <param name="Header">The header of the column the values stand in.</param>
/// <param name="Values">The values, indexed [row, column].</param>
internal readonly record struct GridValues(string Header, double[,] Values);
