// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Keltner Channels indicator. EMA-based channel with ATR-based bands.</summary>
/// <remarks>Similar to Bollinger Bands but uses ATR instead of standard deviation for band width.
/// Upper = EMA + multiplier * ATR, Lower = EMA - multiplier * ATR.</remarks>
public sealed class KeltnerChannels : CandleIndicator<BandsResult>
{
    private readonly int _period;
    private readonly double _atrMultiplier;

    public double Alpha { get; set; } = 0.15;

    /// <summary>Creates a new Keltner Channels indicator.</summary>
    public KeltnerChannels(double[] high, double[] low, double[] close, int period = 20, double atrMultiplier = 1.5)
        : base(high, low, close)
    {
        _period = period; _atrMultiplier = atrMultiplier;
        Label = $"KC({period},{atrMultiplier})";
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] ema = new Ema(Close, _period).Compute();
        double[] atr = new Atr(High, Low, Close, _period).Compute();

        int len = Math.Min(atr.Length, ema.Length - _period);
        if (len <= 0) return new BandsResult([], [], []);

        var middle = new double[len];
        ema.AsSpan(_period, len).CopyTo(middle);
        var scaledAtr = new double[len];
        VectorMath.Multiply(((ReadOnlySpan<double>)atr).Slice(0, len), _atrMultiplier, scaledAtr);
        var upper = new double[len];
        var lower = new double[len];
        VectorMath.Add(middle, scaledAtr, upper);
        VectorMath.Subtract(middle, scaledAtr, lower);
        return new BandsResult(middle, upper, lower);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotBands(axes, Compute(), _period, Alpha);
}
