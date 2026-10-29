// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Series;

/// <summary>Series that can be stacked (cumulative baseline) when multiple instances share an axes.</summary>
public interface IStackable
{
    /// <summary>The values a stacked render sums: each series starts on the sum of the series added before it that point the same
    /// way, one running sum for the values at or above zero and one for the values below zero.</summary>
    double[] Values { get; }

    /// <summary>Where each value starts, as the user sets it; <see langword="null"/> starts every value at zero. A
    /// stacked render works out its stacking afresh each time it draws and leaves this as it was set.</summary>
    double[]? StackBaseline { get; set; }
}
