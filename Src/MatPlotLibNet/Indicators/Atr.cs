// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Average True Range indicator. Measures market volatility by averaging the true range over N periods.</summary>
/// <remarks>True range = max(H-L, |H-prevC|, |L-prevC|). Best placed in a separate subplot below the price chart.</remarks>
public sealed class Atr : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 14;

    private readonly int _period;

    /// <summary>Creates a new ATR indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Averaging period (default <see cref="DefaultPeriod"/>).</param>
    public Atr(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("ATR", period, period);

    /// <inheritdoc />
    public override SignalResult Compute() => FoldHlc(new AtrStep(_period));

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
