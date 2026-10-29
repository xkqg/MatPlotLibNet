// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Specifies which price component to use as input for an indicator.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum PriceSource
{
    /// <summary>Closing price.</summary>
    Close = 0,
    /// <summary>Opening price.</summary>
    Open = 1,
    /// <summary>Highest price.</summary>
    High = 2,
    /// <summary>Lowest price.</summary>
    Low = 3,
    /// <summary>Average of high and low: (H + L) / 2.</summary>
    HL2 = 4,
    /// <summary>Average of high, low, and close: (H + L + C) / 3.</summary>
    HLC3 = 5,
    /// <summary>Average of open, high, low, and close: (O + H + L + C) / 4.</summary>
    OHLC4 = 6,
}

/// <summary>Resolves a <see cref="PriceSource"/> enum to a computed price array from OHLC data.</summary>
public static class PriceSources
{
    /// <summary>The typical price of one bar, (high + low + close) / 3: the one place that formula is written, read by the HLC3
    /// source, the indicators that take a typical price and the DataFrame bridge.</summary>
    /// <param name="high">The bar's high.</param>
    /// <param name="low">The bar's low.</param>
    /// <param name="close">The bar's close.</param>
    internal static double TypicalPrice(double high, double low, double close) => (high + low + close) / 3.0;

    /// <summary>Computes the price array for the given source from OHLC data.</summary>
    public static double[] Resolve(PriceSource source, double[] open, double[] high, double[] low, double[] close)
    {
        int n = close.Length;
        return source switch
        {
            PriceSource.Close => close,
            PriceSource.Open => open,
            PriceSource.High => high,
            PriceSource.Low => low,
            PriceSource.HL2 => Compute(n, i => (high[i] + low[i]) / 2),
            PriceSource.HLC3 => Compute(n, i => TypicalPrice(high[i], low[i], close[i])),
            PriceSource.OHLC4 => Compute(n, i => (open[i] + high[i] + low[i] + close[i]) / 4),
            _ => close
        };
    }

    private static double[] Compute(int length, Func<int, double> formula)
    {
        var result = new double[length];
        for (int i = 0; i < length; i++) result[i] = formula(i);
        return result;
    }
}
