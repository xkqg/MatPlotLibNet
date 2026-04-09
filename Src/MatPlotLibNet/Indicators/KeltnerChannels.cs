// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Keltner Channels indicator. EMA-based channel with ATR-based bands.</summary>
/// <remarks>Similar to Bollinger Bands but uses ATR instead of standard deviation for band width.
/// Upper = EMA + multiplier * ATR, Lower = EMA - multiplier * ATR.</remarks>
public sealed class KeltnerChannels : Indicator<BandsResult>
{
    private readonly double[] _high, _low, _close;
    private readonly int _period;
    private readonly double _atrMultiplier;

    /// <summary>Gets or sets the fill opacity for the channel region.</summary>
    public double Alpha { get; set; } = 0.15;

    /// <summary>Creates a new Keltner Channels indicator.</summary>
    public KeltnerChannels(double[] high, double[] low, double[] close, int period = 20, double atrMultiplier = 1.5)
    {
        _high = high; _low = low; _close = close;
        _period = period; _atrMultiplier = atrMultiplier;
        Label = $"KC({period},{atrMultiplier})";
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] ema = new Ema(_close, _period).Compute();
        double[] atr = new Atr(_high, _low, _close, _period).Compute();

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
    public override void Apply(Axes axes)
    {
        var result = Compute();
        var middle = result.Middle;
        var upper = result.Upper;
        var lower = result.Lower;
        if (middle.Length == 0) return;
        var x = ApplyOffset(VectorMath.Linspace(middle.Length, _period));

        var bandColor = Color ?? Colors.Tab10Orange;
        var fill = axes.FillBetween(x, upper, lower);
        fill.Color = bandColor;
        fill.Alpha = Alpha;
        fill.LineWidth = 0;

        var mid = axes.Plot(x, middle);
        mid.Label = Label;
        mid.Color = bandColor;
        mid.LineWidth = LineWidth;
        mid.LineStyle = LineStyle;
    }
}
