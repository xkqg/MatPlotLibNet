// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for the CUSUM filter — three aligned arrays:
/// <c>Signal</c> carries the {-1, 0, +1} regime-break events;
/// <c>SPos</c> and <c>SNeg</c> expose the accumulating positive/negative CUSUMs
/// for diagnostic overlay.</summary>
public sealed record CusumResult(double[] Signal, double[] SPos, double[] SNeg) : IIndicatorResult;
