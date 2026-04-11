// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for OHLC financial series (candlestick and OHLC bar).
/// Stores Open/High/Low/Close arrays, up/down colors, date labels, and provides PriceData.</summary>
public abstract class OhlcSeries : ChartSeries, IPriceSeries
{
    public double[] Open { get; }

    public double[] High { get; }

    public double[] Low { get; }

    public double[] Close { get; }

    public string[]? DateLabels { get; set; }

    public Color UpColor { get; set; } = Colors.Green;

    public Color DownColor { get; set; } = Colors.Red;

    /// <inheritdoc />
    public double[] PriceData => Close;

    /// <summary>Creates a new OHLC series from the given data.</summary>
    protected OhlcSeries(double[] open, double[] high, double[] low, double[] close)
    {
        Open = open;
        High = high;
        Low = low;
        Close = close;
    }
}
