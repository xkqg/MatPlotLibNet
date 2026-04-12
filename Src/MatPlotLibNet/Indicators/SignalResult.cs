// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result for single-line indicators (SMA, EMA, RSI, ATR, etc.). Wraps a double array with implicit conversion for interchangeability.</summary>
public sealed record SignalResult(double[] Values) : IIndicatorResult
{
    /// <summary>Gets the number of computed values.</summary>
    public int Length => Values.Length;

    /// <summary>Gets the computed value at the specified index.</summary>
    public double this[int index] => Values[index];

    /// <summary>Implicit conversion to double[] for backward compatibility.</summary>
    public static implicit operator double[](SignalResult result) => result.Values;

    /// <summary>Implicit conversion from double[] for convenience.</summary>
    public static implicit operator SignalResult(double[] values) => new(values);
}
