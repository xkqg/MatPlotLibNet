// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series from which price/Y data can be extracted for indicator computation.</summary>
public interface IPriceSeries : ISeries
{
    double[] PriceData { get; }
}
