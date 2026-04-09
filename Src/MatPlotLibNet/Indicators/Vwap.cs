// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Volume-Weighted Average Price indicator. Computes the cumulative average price weighted by volume.</summary>
/// <remarks>VWAP is an intraday benchmark used by institutional traders. The value at each point
/// represents the average price paid per share up to that point, weighted by volume.</remarks>
public sealed class Vwap : Indicator<SignalResult>
{
    private readonly double[] _prices;
    private readonly double[] _volumes;

    /// <summary>Creates a new VWAP indicator.</summary>
    /// <param name="prices">The price data (typically the typical price: (H+L+C)/3).</param>
    /// <param name="volumes">The volume data for each period.</param>
    public Vwap(double[] prices, double[] volumes)
    {
        _prices = prices;
        _volumes = volumes;
        Label = "VWAP";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Math.Min(_prices.Length, _volumes.Length);
        var result = new double[n];
        double cumPriceVol = 0, cumVol = 0;
        for (int i = 0; i < n; i++)
        {
            cumPriceVol += _prices[i] * _volumes[i];
            cumVol += _volumes[i];
            result[i] = cumVol > 0 ? cumPriceVol / cumVol : _prices[i];
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] vwap = Compute();
        var x = VectorMath.Linspace(vwap.Length, 0.0);
        var series = axes.Plot(x, vwap);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
    }
}
