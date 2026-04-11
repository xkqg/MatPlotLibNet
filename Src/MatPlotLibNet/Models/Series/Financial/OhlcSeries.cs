// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Abstract base for OHLC financial series (candlestick and OHLC bar).
/// Stores Open/High/Low/Close arrays, up/down colors, date labels, and provides PriceData.</summary>
public abstract class OhlcSeries : ChartSeries, IPriceSeries
{
    /// <summary>Gets the opening prices.</summary>
    public double[] Open { get; }

    /// <summary>Gets the highest prices.</summary>
    public double[] High { get; }

    /// <summary>Gets the lowest prices.</summary>
    public double[] Low { get; }

    /// <summary>Gets the closing prices.</summary>
    public double[] Close { get; }

    /// <summary>Gets or sets the optional date/category labels for the X axis.</summary>
    public string[]? DateLabels { get; set; }

    /// <summary>Gets or sets the color for up (close >= open) bars.</summary>
    public Color UpColor { get; set; } = Colors.Green;

    /// <summary>Gets or sets the color for down (close &lt; open) bars.</summary>
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
