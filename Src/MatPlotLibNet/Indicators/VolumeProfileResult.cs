// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="VolumeProfile"/>: the volume traded in each price bin. Empty when no bar could be counted.</summary>
/// <param name="BinCentres">The price at the middle of each bin, lowest first.</param>
/// <param name="BinEdges">The price at the edges of the bins, lowest first: one more than the bins. A bin takes the closes from
/// its lower edge up to, but not including, its upper edge; the last bin includes its upper edge.</param>
/// <param name="Volumes">The volume of the bars whose close falls in each bin, one for each bin.</param>
public readonly record struct VolumeProfileResult(double[] BinCentres, double[] BinEdges, double[] Volumes) : IIndicatorResult;
