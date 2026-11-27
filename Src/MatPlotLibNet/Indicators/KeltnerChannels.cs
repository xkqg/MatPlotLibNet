// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics.Tensors;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Keltner Channels indicator. EMA-based channel with ATR-based bands.</summary>
/// <remarks>Similar to Bollinger Bands but uses ATR instead of standard deviation for band width.
/// Upper = EMA + multiplier * ATR, Lower = EMA - multiplier * ATR.</remarks>
public sealed class KeltnerChannels : CandleIndicator<BandsResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    /// <summary>The <c>atrMultiplier</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultAtrMultiplier = 1.5;

    private readonly int _period;
    private readonly double _atrMultiplier;

    /// <summary>The opacity of the shaded channel between the upper and lower lines, from 0 (invisible) to 1 (solid). Default <see cref="IndicatorLook.BandOpacity"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => field = value.ThrowIfNotOpacity(nameof(Alpha)); } = IndicatorLook.BandOpacity;

    /// <summary>Creates a new Keltner Channels indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">EMA and ATR period (default <see cref="DefaultPeriod"/>).</param>
    /// <param name="atrMultiplier">ATR band-width multiplier (default <see cref="DefaultAtrMultiplier"/>).</param>
    public KeltnerChannels(OhlcvBars bars, int period = DefaultPeriod, double atrMultiplier = DefaultAtrMultiplier)
        : base(bars, Spec(period, atrMultiplier))
    {
        _period = period; _atrMultiplier = atrMultiplier;
    }

    internal static IndicatorSpec Spec(int period, double atrMultiplier)
    {
        string label = string.Invariant($"KC({period},{atrMultiplier.InvariantText()})");
        return IndicatorSpec.Of(period, label, label, $"{label} Upper", $"{label} Lower");
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] ema = new Ema(Close, _period).Compute();
        double[] atr = new Atr(OhlcvBars.Hlc(High, Low, Close), _period).Compute();

        // The middle line is the EMA at the bars the channel has a value at: its last len values.
        int len = ValueCount;
        var middle = new double[len];
        ema.AsSpan(ema.Length - len).CopyTo(middle);
        var scaledAtr = new double[len];
        TensorPrimitives.Multiply(((ReadOnlySpan<double>)atr).Slice(0, len), _atrMultiplier, scaledAtr);
        var upper = new double[len];
        var lower = new double[len];
        TensorPrimitives.Add(middle, scaledAtr, upper);
        TensorPrimitives.Subtract(middle, scaledAtr, lower);
        return new BandsResult(middle, upper, lower);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotBand(axes, Compute(), Alpha);
}
