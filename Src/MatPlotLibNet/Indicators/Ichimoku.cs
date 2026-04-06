// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ichimoku Cloud (Ichimoku Kinko Hyo) indicator. Shows support/resistance, trend direction, and momentum.</summary>
/// <remarks>Produces five lines: Tenkan-sen (conversion), Kijun-sen (base), Senkou Span A and B (cloud),
/// and Chikou Span (lagging). The cloud is filled between Span A and B.</remarks>
public sealed class Ichimoku : Indicator<IchimokuResult>
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
    public override IchimokuResult Compute()
    {
        var tenkan = DonchianMid(_high, _low, _tenkanPeriod);
        var kijun = DonchianMid(_high, _low, _kijunPeriod);

        // Senkou Span A = (Tenkan + Kijun) / 2
        int spanLen = Math.Min(tenkan.Length, kijun.Length);
        int kijunOffset = _kijunPeriod - _tenkanPeriod;
        var spanA = new double[spanLen - kijunOffset];
        for (int i = 0; i < spanA.Length; i++)
            spanA[i] = (tenkan[i + kijunOffset] + kijun[i]) / 2;

        // Senkou Span B = Donchian mid of senkouB period
        var spanB = DonchianMid(_high, _low, _senkouBPeriod);

        // Chikou Span = close (lagging line, plotted shifted back by displacement)
        var chikou = _close;

        return new IchimokuResult(tenkan, kijun, spanA, spanB, chikou);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        if (_close.Length < _senkouBPeriod) return;

        var result = Compute();

        // Tenkan-sen
        var tenkanX = MakeX(result.TenkanSen.Length, _tenkanPeriod - 1);
        var ts = axes.Plot(tenkanX, result.TenkanSen);
        ts.Label = "Tenkan"; ts.Color = Styling.Color.FromHex("#0496ff"); ts.LineWidth = 1;

        // Kijun-sen
        var kijunX = MakeX(result.KijunSen.Length, _kijunPeriod - 1);
        var ks = axes.Plot(kijunX, result.KijunSen);
        ks.Label = "Kijun"; ks.Color = Styling.Color.FromHex("#991515"); ks.LineWidth = 1;

        // Cloud (Senkou Span A & B)
        int cloudLen = Math.Min(result.SenkouSpanA.Length, result.SenkouSpanB.Length);
        var cloudSpanA = result.SenkouSpanA[..cloudLen];
        var cloudSpanB = result.SenkouSpanB[..cloudLen];

        var cloudX = MakeX(cloudLen, _kijunPeriod - 1 + _displacement);
        var cloud = axes.FillBetween(cloudX, cloudSpanA, cloudSpanB);
        cloud.Alpha = 0.2;
        cloud.Color = Styling.Color.Tab10Green;
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
