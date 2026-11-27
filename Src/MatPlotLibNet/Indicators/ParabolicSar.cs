// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Result of a <see cref="ParabolicSar"/> computation.</summary>
/// <param name="Sar">The SAR values at each bar; NaN at the first bar, which only seeds the trend with the second.</param>
/// <param name="IsLong">True when the SAR is below price (long/bullish position); false at the first bar.</param>
public sealed record ParabolicSarResult(double[] Sar, bool[] IsLong) : IIndicatorResult;

/// <summary>Parabolic SAR trend-following indicator. Plotted as dots above or below price bars.</summary>
/// <remarks>Dots below price indicate an uptrend (long). Dots above indicate a downtrend (short).
/// When price crosses the SAR, the trend reverses and the SAR jumps to the extreme point of the trend that ended. The values are
/// Wilder's as TA-Lib computes them; the first bar has none. Overlay this on the price axes.</remarks>
public sealed class ParabolicSar : Indicator<ParabolicSarResult>
{
    /// <summary>The <c>step</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultStep = 0.02;

    /// <summary>The <c>max</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultMax = 0.2;

    private readonly double[] _high, _low;
    private readonly double _step;
    private readonly double _max;

    /// <inheritdoc />
    private protected override int BarsIn => Math.Min(_high.Length, _low.Length);

    /// <summary>The colour of the dots drawn below the price, while the trend is up. Green by default: a SAR given no colour draws
    /// these dots green under the library's own themes and in the next colour of the cycle under the matplotlib themes, as
    /// matplotlib draws a scatter that names none; a colour that is given, green included, is drawn as given.</summary>
    public Color LongColor { get => GivenLongColor ?? IndicatorLook.Library.Moves.Up; set => GivenLongColor = value; }

    /// <summary>The value of <see cref="LongColor"/> its author gave, <see langword="null"/> while none was.</summary>
    internal Color? GivenLongColor { get; private set; }

    /// <summary>The colour of the dots drawn above the price, while the trend is down. Red by default: a SAR given no colour draws
    /// these dots red under the library's own themes and in the next colour of the cycle under the matplotlib themes; a colour
    /// that is given, red included, is drawn as given.</summary>
    public Color ShortColor { get => GivenShortColor ?? IndicatorLook.Library.Moves.Down; set => GivenShortColor = value; }

    /// <summary>The value of <see cref="ShortColor"/> its author gave, <see langword="null"/> while none was.</summary>
    internal Color? GivenShortColor { get; private set; }

    /// <summary>Creates a new Parabolic SAR indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="step">Acceleration factor step (default <see cref="DefaultStep"/>).</param>
    /// <param name="max">Maximum acceleration factor (default <see cref="DefaultMax"/>).</param>
    public ParabolicSar(double[] high, double[] low, double step = DefaultStep, double max = DefaultMax) : base(Spec())
    {
        _high = high; _low = low;
        _step = step; _max = max;
    }

    internal static IndicatorSpec Spec()
    {
        string label = "SAR";
        return IndicatorSpec.Of(1, label, label, "SAR Long");
    }

    /// <inheritdoc />
    public override ParabolicSarResult Compute()
    {
        int n = ValuedBars;
        var sar = new double[n];
        var isLong = new bool[n];
        var step = new ParabolicSarStep(_step, _max);
        for (int i = 0; i < n; i++)
        {
            var point = step.Next(_high[i], _low[i]);
            sar[i] = point.Sar;
            isLong[i] = point.IsLong;
        }

        return new ParabolicSarResult(sar, isLong);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        if (result.Sar.Length == 0) return;

        var x = Positions(result.Sar.Length);

        // Split into long and short positions
        var longX = new List<double>();
        var longY = new List<double>();
        var shortX = new List<double>();
        var shortY = new List<double>();

        for (int i = 1; i < result.Sar.Length; i++)   // the first bar has no SAR
        {
            if (result.IsLong[i]) { longX.Add(x[i]); longY.Add(result.Sar[i]); }
            else { shortX.Add(x[i]); shortY.Add(result.Sar[i]); }
        }

        var longSeries = axes.Scatter(longX.ToArray(), longY.ToArray());
        WearColor(longSeries, GivenLongColor, LongColor);
        longSeries.Label = Label + " (Long)";
        longSeries.MarkerSize = 4;

        var shortSeries = axes.Scatter(shortX.ToArray(), shortY.ToArray());
        WearColor(shortSeries, GivenShortColor, ShortColor);
        shortSeries.Label = Label + " (Short)";
        shortSeries.MarkerSize = 4;
    }
}
