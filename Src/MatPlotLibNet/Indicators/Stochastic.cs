// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Stochastic Oscillator indicator. Compares closing price to the high-low range over a lookback period.</summary>
/// <remarks>Produces two lines, as pandas-ta's <c>stoch</c> and TA-Lib's <c>STOCH</c> do: %K, the raw position of the close in the range of the
/// last <c>k</c> bars averaged over <c>smoothK</c> bars (<see cref="DefaultSmoothK"/> unless given; 1 draws the raw, fast %K), and %D, the simple mean of the last <c>d</c> values
/// of %K. Values range 0-100. Above 80 is typically overbought; below 20 is oversold. Best placed in a separate subplot.</remarks>
public sealed class Stochastic : CandleIndicator<StochasticResult>
{
    /// <summary>The <c>kPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultKPeriod = 14;

    /// <summary>The <c>dPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultDPeriod = 3;

    /// <summary>How many raw %K values the %K line is the mean of unless the caller says: the slow stochastic of pandas-ta and TA-Lib.</summary>
    public const int DefaultSmoothK = 3;

    private readonly int _kPeriod;
    private readonly int _smoothK;
    private readonly int _dPeriod;

    /// <summary>The colour of the %D line. <see langword="null"/> (the default) draws it orange under the library's own themes and in
    /// the next colour of the cycle under the matplotlib themes, as matplotlib draws a line that names no colour.</summary>
    public Color? DColor { get; set; }

    /// <summary>Creates a new Stochastic Oscillator indicator: the slow stochastic, %K averaged over 3 bars.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="kPeriod">The %K lookback period (default <see cref="DefaultKPeriod"/>).</param>
    /// <param name="dPeriod">The %D smoothing period (default <see cref="DefaultDPeriod"/>).</param>
    public Stochastic(OhlcvBars bars, int kPeriod = DefaultKPeriod, int dPeriod = DefaultDPeriod)
        : this(bars, kPeriod, dPeriod, DefaultSmoothK)
    {
    }

    /// <summary>Creates a new Stochastic Oscillator indicator with the smoothing of %K named.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="kPeriod">The %K lookback period.</param>
    /// <param name="dPeriod">The %D smoothing period.</param>
    /// <param name="smoothK">How many raw %K values the %K line is the mean of; 1 draws the raw (fast) %K.</param>
    public Stochastic(OhlcvBars bars, int kPeriod, int dPeriod, int smoothK)
        : base(bars, Spec(kPeriod, dPeriod, smoothK))
    {
        _kPeriod = kPeriod;
        _dPeriod = dPeriod;
        _smoothK = smoothK;
    }

    internal static IndicatorSpec Spec(int kPeriod, int dPeriod, int smoothK)
    {
        string label = IndicatorSpec.Labelled("%K", kPeriod);
        return IndicatorSpec.Of(kPeriod + Math.Max(0, smoothK - 1) + dPeriod - 2, label, label, IndicatorSpec.Labelled("%D", dPeriod));
    }

    /// <inheritdoc />
    public override StochasticResult Compute()
    {
        int n = Close.Length;
        if (n < _kPeriod) return new StochasticResult([], []);

        // The extremes of every window in one pass each (RollingExtremeStep), then the raw %K of every bar by the step's one formula,
        // then the means (RollingMeanStep): the passes are separate because that is three times as cheap as the step run bar by bar,
        // and StochasticStep says the same bar by bar for the streaming form.
        var highest = new double[n];
        var lowest = new double[n];
        High.RollingMax(_kPeriod, highest);
        Low.RollingMin(_kPeriod, lowest);

        var k = new double[n - _kPeriod + 1];
        for (int i = 0; i < k.Length; i++)
            k[i] = StochasticStep.RawK(Close[i + _kPeriod - 1], lowest[i + _kPeriod - 1], highest[i + _kPeriod - 1]);

        if (_smoothK > 1) k = k.RollingMean(_smoothK);
        return new StochasticResult(k, k.RollingMean(_dPeriod));
    }

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingStochastic"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Percent;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.K);
        PlotLine(axes, result.D, LineLabel(1), DColor, IndicatorLook.Library.Lines.Second);
        Pane.Pin(axes);
    }
}
