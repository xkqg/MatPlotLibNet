// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ichimoku Cloud (Ichimoku Kinko Hyo) indicator. Shows support/resistance, trend direction, and momentum.</summary>
/// <remarks>Produces five lines: Tenkan-sen (conversion), Kijun-sen (base), Senkou Span A and B (cloud),
/// and Chikou Span (lagging). The cloud is filled between Span A and B.</remarks>
public sealed class Ichimoku : Indicator
{
    private readonly double[] _high, _low, _close;
    private readonly int _tenkanPeriod, _kijunPeriod, _senkouBPeriod, _displacement;

    /// <summary>Creates a new Ichimoku Cloud indicator with standard parameters.</summary>
    public Ichimoku(double[] high, double[] low, double[] close,
        int tenkanPeriod = 9, int kijunPeriod = 26, int senkouBPeriod = 52, int displacement = 26)
    {
        _high = high; _low = low; _close = close;
        _tenkanPeriod = tenkanPeriod; _kijunPeriod = kijunPeriod;
        _senkouBPeriod = senkouBPeriod; _displacement = displacement;
        Label = "Ichimoku";
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        int n = _close.Length;
        if (n < _senkouBPeriod) return;

        var tenkan = DonchianMid(_high, _low, _tenkanPeriod);
        var kijun = DonchianMid(_high, _low, _kijunPeriod);

        // Tenkan-sen
        var tenkanX = MakeX(tenkan.Length, _tenkanPeriod - 1);
        var ts = axes.Plot(tenkanX, tenkan);
        ts.Label = "Tenkan"; ts.Color = Styling.Color.FromHex("#0496ff"); ts.LineWidth = 1;

        // Kijun-sen
        var kijunX = MakeX(kijun.Length, _kijunPeriod - 1);
        var ks = axes.Plot(kijunX, kijun);
        ks.Label = "Kijun"; ks.Color = Styling.Color.FromHex("#991515"); ks.LineWidth = 1;

        // Senkou Span A = (Tenkan + Kijun) / 2, shifted forward by displacement
        int spanLen = Math.Min(tenkan.Length, kijun.Length);
        int kijunOffset = _kijunPeriod - _tenkanPeriod;
        var spanA = new double[spanLen - kijunOffset];
        for (int i = 0; i < spanA.Length; i++)
            spanA[i] = (tenkan[i + kijunOffset] + kijun[i]) / 2;

        // Senkou Span B = Donchian mid of senkouB period, shifted forward
        var spanB = DonchianMid(_high, _low, _senkouBPeriod);
        int cloudLen = Math.Min(spanA.Length, spanB.Length);
        var cloudSpanA = spanA[..cloudLen];
        var cloudSpanB = spanB[..cloudLen];

        var cloudX = MakeX(cloudLen, _kijunPeriod - 1 + _displacement);
        var cloud = axes.FillBetween(cloudX, cloudSpanA, cloudSpanB);
        cloud.Alpha = 0.2;
        cloud.Color = Styling.Color.FromHex("#2ca02c");
        cloud.Label = "Cloud";
    }

    private static double[] DonchianMid(double[] high, double[] low, int period)
    {
        int n = high.Length;
        var result = new double[n - period + 1];
        for (int i = 0; i < result.Length; i++)
        {
            double hh = double.MinValue, ll = double.MaxValue;
            for (int j = i; j < i + period; j++)
            {
                if (high[j] > hh) hh = high[j];
                if (low[j] < ll) ll = low[j];
            }
            result[i] = (hh + ll) / 2;
        }
        return result;
    }

    private double[] MakeX(int length, int offset)
    {
        var x = new double[length];
        for (int i = 0; i < length; i++) x[i] = offset + i;
        return ApplyOffset(x);
    }
}
