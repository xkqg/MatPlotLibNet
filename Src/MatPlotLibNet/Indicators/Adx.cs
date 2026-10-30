// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Average Directional Index indicator. Measures trend strength on a 0-100 scale regardless of direction.</summary>
/// <remarks>ADX above 25 indicates a strong trend, below 20 indicates ranging. Produces three lines:
/// ADX (trend strength), +DI (bullish direction), and -DI (bearish direction).</remarks>
public sealed class Adx : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>The colour of the +DI line. <see langword="null"/> (the default) draws it green.</summary>
    public Color? PlusDiColor { get; set; }

    /// <summary>The colour of the -DI line. <see langword="null"/> (the default) draws it red.</summary>
    public Color? MinusDiColor { get; set; }

    /// <summary>Creates a new ADX indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Smoothing period (default 14).</param>
    public Adx(OhlcvBars bars, int period = 14)
        : base(bars)
    {
        _period = period;
        Label = $"ADX({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var r = ComputeFull();
        return r.Adx;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var r = ComputeFull();
        double[] adx = r.Adx, plusDi = r.PlusDi, minusDi = r.MinusDi;
        int offset = _period * 2;
        PlotSignal(axes, adx, offset);
        if (plusDi.Length == adx.Length)
        {
            PlotSignal(axes, plusDi, offset, "+DI", PlusDiColor ?? Colors.Green).LineWidth = 1;
            PlotSignal(axes, minusDi, offset, "-DI", MinusDiColor ?? Colors.Red).LineWidth = 1;
        }
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }

    /// <summary>Computes ADX, +DI, and −DI in a single pass.</summary>
    /// <remarks>Returns all three ADX signals (<c>Adx</c>, <c>PlusDi</c>, <c>MinusDi</c>) in one pass.
    /// Use <see cref="Compute"/> when only the scalar ADX line is needed.</remarks>
    public AdxResult ComputeFull()
    {
        int n = Close.Length;
        if (n <= _period * 2) return new([], [], []);

        int length = n - _period * 2;
        var adx = new double[length];
        var plusDi = new double[length];
        var minusDi = new double[length];
        var step = new AdxStep(_period);
        for (int i = 0; i < n; i++)
        {
            var point = step.Next(High[i], Low[i], Close[i]);
            if (i < _period * 2) continue;
            adx[i - _period * 2] = point.Adx;
            plusDi[i - _period * 2] = point.PlusDi;
            minusDi[i - _period * 2] = point.MinusDi;
        }

        return new(adx, plusDi, minusDi);
    }
}
