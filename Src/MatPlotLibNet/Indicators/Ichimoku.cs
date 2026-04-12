// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Ichimoku Cloud (Ichimoku Kinko Hyo) indicator. Shows support/resistance, trend direction, and momentum.</summary>
/// <remarks>Produces five lines: Tenkan-sen (conversion), Kijun-sen (base), Senkou Span A and B (cloud),
/// and Chikou Span (lagging). The cloud is filled between Span A and B.</remarks>
public sealed class Ichimoku : CandleIndicator<IchimokuResult>
{
    private readonly int _tenkanPeriod, _kijunPeriod, _senkouBPeriod, _displacement;

    /// <summary>Creates a new Ichimoku Cloud indicator with standard parameters.</summary>
    public Ichimoku(double[] high, double[] low, double[] close,
        int tenkanPeriod = 9, int kijunPeriod = 26, int senkouBPeriod = 52, int displacement = 26)
        : base(high, low, close)
    {
        _tenkanPeriod = tenkanPeriod; _kijunPeriod = kijunPeriod;
        _senkouBPeriod = senkouBPeriod; _displacement = displacement;
        Label = "Ichimoku";
    }

    /// <inheritdoc />
    public override IchimokuResult Compute()
    {
        var tenkan = ComputeDonchianMid(_tenkanPeriod);
        var kijun = ComputeDonchianMid(_kijunPeriod);

        // Senkou Span A = (Tenkan + Kijun) / 2
        int spanLen = Math.Min(tenkan.Length, kijun.Length);
        int kijunOffset = _kijunPeriod - _tenkanPeriod;
        var spanA = new double[spanLen - kijunOffset];
        for (int i = 0; i < spanA.Length; i++)
            spanA[i] = (tenkan[i + kijunOffset] + kijun[i]) / 2;

        // Senkou Span B = Donchian mid of senkouB period
        var spanB = ComputeDonchianMid(_senkouBPeriod);

        // Chikou Span = close (lagging line, plotted shifted back by displacement)
        var chikou = Close;

        return new IchimokuResult(tenkan, kijun, spanA, spanB, chikou);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        if (Close.Length < _senkouBPeriod) return;

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
        cloud.Color = Colors.Tab10Green;
        cloud.Label = "Cloud";
    }

}
