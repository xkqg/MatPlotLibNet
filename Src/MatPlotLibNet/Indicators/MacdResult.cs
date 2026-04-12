// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for the MACD indicator containing the MACD line, signal line, and histogram.</summary>
public sealed record MacdResult(double[] MacdLine, double[] SignalLine, double[] Histogram) : IIndicatorResult;
