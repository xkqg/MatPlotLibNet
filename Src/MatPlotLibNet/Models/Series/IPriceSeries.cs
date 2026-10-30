// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series from which price/Y data can be extracted for indicator computation.</summary>
public interface IPriceSeries : ISeries
{
    /// <summary>The price values an indicator is computed over, in series order: the Y values of a line, or the
    /// closing prices of a candlestick series.</summary>
    double[] PriceData { get; }
}
