// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Average Directional Index indicator. Measures trend strength on a 0-100 scale regardless of direction.</summary>
/// <remarks>ADX above 25 indicates a strong trend, below 20 indicates ranging. Produces three lines:
/// ADX (trend strength), +DI (bullish direction), and -DI (bearish direction), all three smoothed as Wilder smooths them.</remarks>
public sealed class Adx : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 14;

    private readonly int _period;

    /// <summary>The colour of the +DI line. <see langword="null"/> (the default) draws it green under the library's own themes and in the
    /// next colour of the cycle under the matplotlib themes, as matplotlib draws a line that names no colour.</summary>
    public Color? PlusDiColor { get; set; }

    /// <summary>The colour of the -DI line. <see langword="null"/> (the default) draws it red under the library's own themes and in the
    /// next colour of the cycle under the matplotlib themes.</summary>
    public Color? MinusDiColor { get; set; }

    /// <summary>Creates a new ADX indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Smoothing period (default <see cref="DefaultPeriod"/>).</param>
    public Adx(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period)
    {
        string label = IndicatorSpec.Labelled("ADX", period);
        return IndicatorSpec.Of(2 * period, label, label, "+DI", "-DI");
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var r = ComputeFull();
        return r.Adx;
    }

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingAdx"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Percent;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var r = ComputeFull();
        double[] adx = r.Adx, plusDi = r.PlusDi, minusDi = r.MinusDi;
        PlotLine(axes, adx);
        if (plusDi.Length == adx.Length)
        {
            PlotLine(axes, plusDi, LineLabel(1), PlusDiColor, IndicatorLook.Library.Moves.Up);
            PlotLine(axes, minusDi, LineLabel(2), MinusDiColor, IndicatorLook.Library.Moves.Down);
        }
        Pane.Pin(axes);
    }

    /// <summary>Computes ADX, +DI, and −DI in a single pass.</summary>
    /// <remarks>Returns all three ADX signals (<c>Adx</c>, <c>PlusDi</c>, <c>MinusDi</c>) in one pass.
    /// Use <see cref="Compute"/> when only the scalar ADX line is needed.</remarks>
    public AdxResult ComputeFull() => FoldHlc<AdxStep, AdxPoint, Lines>(new(_period), new(ValueCount)).Result;

    /// <summary>The three lines of the indicator, one entry per bar from its first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<AdxPoint>
    {
        private readonly double[] _adx = new double[count], _plusDi = new double[count], _minusDi = new double[count];

        internal AdxResult Result => new(_adx, _plusDi, _minusDi);

        public void Set(int index, AdxPoint output)
        {
            _adx[index] = output.Adx;
            _plusDi[index] = output.PlusDi;
            _minusDi[index] = output.MinusDi;
        }
    }
}
