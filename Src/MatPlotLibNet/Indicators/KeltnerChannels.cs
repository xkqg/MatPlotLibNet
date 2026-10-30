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

    /// <summary>The opacity of the shaded channel between the upper and lower lines, from 0 (invisible) to 1 (solid). Default 0.15.</summary>
    public double Alpha { get; set; } = 0.15;

    /// <summary>Creates a new Keltner Channels indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">EMA and ATR period (default 20).</param>
    /// <param name="atrMultiplier">ATR band-width multiplier (default 1.5).</param>
    public KeltnerChannels(OhlcvBars bars, int period = 20, double atrMultiplier = 1.5)
        : base(bars)
    {
        _period = period; _atrMultiplier = atrMultiplier;
        Label = $"KC({period},{atrMultiplier})";
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] ema = new Ema(Close, _period).Compute();
        double[] atr = new Atr(OhlcvBars.Hlc(High, Low, Close), _period).Compute();

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
