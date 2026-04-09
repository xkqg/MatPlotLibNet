// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Computed histogram bin data.</summary>
/// <param name="Min">Minimum data value.</param>
/// <param name="BinWidth">Width of each bin.</param>
/// <param name="Counts">Count of values in each bin.</param>
public readonly record struct HistogramBins(double Min, double BinWidth, int[] Counts);
