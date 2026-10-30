// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>One step of the Ehlers instantaneous trendline: the recursion that the batch <see cref="EhlersITrend"/> folds over its
/// prices and <see cref="Streaming.StreamingEhlersITrend"/> takes one price at a time. The first six bars (which the
/// <see cref="HilbertStep"/> only feeds) carry the price; from the seventh the trend is the mean of the last
/// <c>period</c> prices (the dominant cycle, rounded) with linearly falling weights, and keeps its last value while the cycle is
/// still settling (under 2 bars) or longer than the history. Before the seventh bar the step says NaN.</summary>
internal struct EhlersITrendStep
{
    private const int History = 50;

    private HilbertStep _hilbert = new();
    private readonly double[] _prices = new double[History];
    private double _trend;
    private int _count;

    /// <summary>A step before any price.</summary>
    public EhlersITrendStep()
    {
    }

    /// <summary>Takes one price and returns the trend, or NaN before the seventh price.</summary>
    /// <param name="price">The price.</param>
    internal double Next(double price)
    {
        int i = _count++;
        var row = _hilbert.Next(price);
        _prices[i % History] = price;
        if (i < 6)
        {
            _trend = price;
            return double.NaN;
        }

        int windowLen = (int)Math.Round(row.Period);
        if (windowLen < 2 || windowLen > i + 1)
            return _trend;

        double num = 0;
        double den = 0;
        for (int k = 0; k < windowLen; k++)
        {
            double w = windowLen - k;
            num += w * _prices[(i - k) % History];
            den += w;
        }

        _trend = den > 0 ? num / den : _trend;
        return _trend;
    }
}
