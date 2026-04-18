// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Indicators.Streaming;

/// <summary>Shared OHLC(V) synthetic-data helpers for the streaming indicator test suite.
/// Centralised here so each <c>Streaming&lt;X&gt;Tests</c> file uses the same deterministic inputs
/// (avoids per-file copy-paste of bar arrays). All factories return small, hand-derivable bars
/// so the test files can compute expected values inline.</summary>
internal static class StreamingTestData
{
    /// <summary>Six bars with monotonically rising O/H/L/C — useful for trend assertions where
    /// signed-direction indicators (OBV, MACD) must produce strictly positive output.</summary>
    public static OhlcBar[] RisingBars(int count = 6, double startClose = 100, double step = 1)
    {
        var bars = new OhlcBar[count];
        for (int i = 0; i < count; i++)
        {
            double close = startClose + i * step;
            bars[i] = new OhlcBar(Open: close - step / 2, High: close + step, Low: close - step, Close: close);
        }
        return bars;
    }

    /// <summary>All bars share the same constant OHLC. Drives the high==low / zero-range branches
    /// in CCI, Williams %R and Stochastic.</summary>
    public static OhlcBar[] FlatBars(int count, double price = 100)
    {
        var bars = new OhlcBar[count];
        for (int i = 0; i < count; i++) bars[i] = new OhlcBar(price, price, price, price);
        return bars;
    }

    /// <summary>Five bars with the close oscillating between the period high and low — produces
    /// alternating high-extreme and low-extreme values, exercises ring-buffer wraparound.</summary>
    public static OhlcBar[] ZigZagBars()
    {
        return
        [
            new OhlcBar(10, 12, 8, 10),
            new OhlcBar(10, 14, 9, 13),
            new OhlcBar(13, 13, 7, 8),
            new OhlcBar(8,  15, 8, 14),
            new OhlcBar(14, 14, 6, 7),
        ];
    }
}
